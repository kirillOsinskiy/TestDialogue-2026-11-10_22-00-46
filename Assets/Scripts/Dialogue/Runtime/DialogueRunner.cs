using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Dialogue
{
    public sealed class DialogueRunner
    {
        private readonly GameState _state;
        private readonly DialogueConditionEvaluator _evaluator;
        private readonly DialogueEffectExecutor _executor;

        private readonly Dictionary<string, DialogueNode> _nodes =
            new Dictionary<string, DialogueNode>(StringComparer.Ordinal);

        private readonly List<DialogueChoice> _availableChoices = new List<DialogueChoice>();

        private CancellationTokenSource _executionCancellation;

        public DialogueAsset CurrentDialogue { get; private set; }
        public DialogueNode CurrentNode { get; private set; }

        public IReadOnlyList<DialogueChoice> AvailableChoices { get; }

        public bool IsActive => CurrentDialogue != null;
        public bool IsBusy { get; private set; }

        public bool CanFinish => IsActive && !IsBusy && _availableChoices.Count == 0;

        // UI перечитывает свойства runner после этого события.
        public event Action StateChanged;

        public DialogueRunner(
            GameState state,
            DialogueConditionEvaluator evaluator,
            DialogueEffectExecutor executor)
        {
            _state = state
                ?? throw new ArgumentNullException(nameof(state));

            _evaluator = evaluator
                ?? throw new ArgumentNullException(nameof(evaluator));

            _executor = executor
                ?? throw new ArgumentNullException(nameof(executor));

            AvailableChoices = _availableChoices.AsReadOnly();
        }

        public void StartDialogue(DialogueAsset dialogue)
        {
            if (IsActive || IsBusy)
            {
                throw new InvalidOperationException("Предыдущий диалог ещё не завершён.");
            }

            if (dialogue == null)
            {
                throw new ArgumentNullException(nameof(dialogue));
            }

            if (string.IsNullOrWhiteSpace(dialogue.Id))
            {
                throw new InvalidOperationException("У диалога отсутствует Id.");
            }

            _nodes.Clear();

            if (dialogue.Nodes == null)
            {
                throw new InvalidOperationException("Список узлов не инициализирован.");
            }

            foreach (DialogueNode node in dialogue.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                {
                    throw new InvalidOperationException(
                        "Узел отсутствует или содержит пустой Id.");
                }

                if (_nodes.ContainsKey(node.Id))
                {
                    throw new InvalidOperationException($"Повторяющийся Id узла: {node.Id}.");
                }

                _nodes.Add(node.Id, node);
            }

            DialogueNode entry = FindNode(dialogue.EntryNodeId);

            // Проверяем стартовую реплику до начала разговора.
            List<DialogueChoice> choices = CollectAvailableChoices(entry, dialogue.Id);

            CurrentDialogue = dialogue;
            CurrentNode = entry;
            SetAvailableChoices(choices);

            StateChanged?.Invoke();
        }

        public async Task ChooseAsync(string choiceId)
        {
            if (!IsActive || IsBusy)
            {
                throw new InvalidOperationException("Сейчас нельзя выбрать ответ.");
            }

            DialogueChoice choice = FindChoice(choiceId);

            // Состояние мира могло измениться после показа кнопок.
            if (!_evaluator.Evaluate(choice.Availability, CurrentDialogue.Id))
            {
                RefreshChoices();

                throw new InvalidOperationException(
                    "Этот ответ больше недоступен.");
            }

            CancellationTokenSource cancellation = new CancellationTokenSource();

            _executionCancellation = cancellation;
            CancellationToken token = cancellation.Token;

            IsBusy = true;

            try
            {
                // Проверяем переход до записи выбора и последствий.
                DialogueNode nextNode = ResolveDestination(choice);

                if (nextNode != null)
                {
                    ValidateChoices(nextNode);
                }

                StateChanged?.Invoke();
                token.ThrowIfCancellationRequested();

                _state.RecordChoice(DialogueChoiceHistoryKey.Build(CurrentDialogue.Id, choice.Id));

                await _executor.ExecuteAsync(choice.Effects, token);

                token.ThrowIfCancellationRequested();

                if (nextNode == null)
                {
                    ClearDialogue();
                }
                else
                {
                    // Учитываем изменения состояния от последствий.
                    List<DialogueChoice> choices = CollectAvailableChoices(nextNode, CurrentDialogue.Id);

                    CurrentNode = nextNode;
                    SetAvailableChoices(choices);
                }
            }
            catch
            {
                // Ошибка или отмена: не продолжаем и не повторяем эффекты.
                ClearDialogue();
                throw;
            }
            finally
            {
                _executionCancellation = null;
                cancellation.Dispose();

                IsBusy = false;
                StateChanged?.Invoke();
            }
        }

        // Можно вызвать после внешнего изменения GameState.
        public void RefreshChoices()
        {
            if (!IsActive || IsBusy)
            {
                return;
            }

            SetAvailableChoices(
                CollectAvailableChoices(CurrentNode, CurrentDialogue.Id)
            );

            StateChanged?.Invoke();
        }

        // Для последней реплики или узла без доступных ответов.
        public void FinishDialogue()
        {
            if (!CanFinish)
            {
                throw new InvalidOperationException(
                    "Сейчас нельзя завершить диалог этой командой.");
            }

            ClearDialogue();
            StateChanged?.Invoke();
        }

        // Например, при выходе из диалога или выгрузке сцены.
        public void CancelDialogue()
        {
            if (IsBusy)
            {
                // Очистка произойдёт после завершения ожидания отмены.
                _executionCancellation?.Cancel();
                return;
            }

            if (!IsActive)
            {
                return;
            }

            ClearDialogue();
            StateChanged?.Invoke();
        }

        private DialogueNode ResolveDestination(DialogueChoice choice)
        {
            if (choice.Destination == null)
            {
                throw new InvalidOperationException(
                    $"У ответа {choice.Id} отсутствует Destination.");
            }

            switch (choice.Destination.Type)
            {
                case DialogueDestinationType.EndDialogue:
                    return null;

                case DialogueDestinationType.NextNode:
                    return FindNode(choice.Destination.NodeId);

                default:
                    throw new InvalidOperationException(
                        $"Неизвестный тип перехода у ответа {choice.Id}.");
            }
        }

        private DialogueNode FindNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) ||
                !_nodes.TryGetValue(nodeId, out DialogueNode node))
            {
                throw new InvalidOperationException(
                    $"Не найден узел: '{nodeId}'.");
            }

            return node;
        }

        private DialogueChoice FindChoice(string choiceId)
        {
            if (string.IsNullOrWhiteSpace(choiceId))
            {
                throw new ArgumentException(
                    "Id ответа не может быть пустым.",
                    nameof(choiceId));
            }

            foreach (DialogueChoice choice in CurrentNode.Choices)
            {
                if (string.Equals(
                    choice.Id, choiceId, StringComparison.Ordinal))
                {
                    return choice;
                }
            }

            throw new ArgumentException(
                "Ответ не принадлежит текущему узлу.",
                nameof(choiceId));
        }

        private List<DialogueChoice> CollectAvailableChoices(DialogueNode node, string dialogueId)
        {
            ValidateChoices(node);

            var result = new List<DialogueChoice>();

            foreach (DialogueChoice choice in node.Choices)
            {
                if (_evaluator.Evaluate(choice.Availability, dialogueId))
                {
                    result.Add(choice);
                }
            }

            return result;
        }

        private static void ValidateChoices(DialogueNode node)
        {
            if (node.Choices == null)
            {
                throw new InvalidOperationException($"У узла {node.Id} отсутствует список ответов.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (DialogueChoice choice in node.Choices)
            {
                if (choice == null || string.IsNullOrWhiteSpace(choice.Id))
                {
                    throw new InvalidOperationException($"Некорректный ответ в узле {node.Id}.");
                }

                if (!ids.Add(choice.Id))
                {
                    throw new InvalidOperationException($"Повторяющийся Id ответа в узле {node.Id}: {choice.Id}");
                }
            }
        }

        private void SetAvailableChoices(List<DialogueChoice> choices)
        {
            _availableChoices.Clear();
            _availableChoices.AddRange(choices);
        }

        private void ClearDialogue()
        {
            CurrentDialogue = null;
            CurrentNode = null;

            _availableChoices.Clear();
            _nodes.Clear();
        }
    }
}