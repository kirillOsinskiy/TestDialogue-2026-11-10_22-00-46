using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialogue
{
    public sealed class DialogueView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Text")]
        [SerializeField] private TMP_Text _speakerText;
        [SerializeField] private TMP_Text _lineText;
        [SerializeField] private TMP_Text _statusText;

        [Header("Choices")]
        [SerializeField] private Transform _choicesContent;
        [SerializeField]
        private DialogueChoiceButtonView _choiceButtonPrefab;

        [Header("Controls")]
        [SerializeField] private Button _finishButton;
        [SerializeField] private ScrollRect _lineScroll;
        [SerializeField] private ScrollRect _choicesScroll;

        private readonly List<DialogueChoiceButtonView> _buttons =
            new List<DialogueChoiceButtonView>();

        private DialogueRunner _runner;
        private DialogueNode _displayedNode;
        private bool _handlingChoice;

        private void Awake()
        {
            _finishButton.onClick.AddListener(HandleFinishClicked);
            _panel.SetActive(false);
        }

        public void Initialize(DialogueRunner runner)
        {
            if (runner == null)
            {
                throw new ArgumentNullException(nameof(runner));
            }

            if (_runner != null)
            {
                throw new InvalidOperationException("DialogueView уже инициализирован.");
            }

            _runner = runner;
            _runner.StateChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            bool active = _runner != null && _runner.IsActive;
            _panel.SetActive(active);

            if (!active)
            {
                ClearButtons();
                _displayedNode = null;
                return;
            }

            DialogueNode node = _runner.CurrentNode;
            DialogueAsset dialogue = _runner.CurrentDialogue;
            bool nodeChanged = !ReferenceEquals(_displayedNode, node);
            _displayedNode = node;

            _speakerText.text = dialogue.SpeakerName;
            _lineText.text = node.Text;

            _statusText.text = _runner.IsBusy
                ? "Обработка..."
                : _runner.CanFinish
                    ? "Разговор завершён."
                    : "Выберите ответ";

            _finishButton.gameObject.SetActive(_runner.CanFinish);
            _finishButton.interactable = !_runner.IsBusy;

            // При блокировке существующие кнопки не пересоздаём.
            if (_runner.IsBusy)
            {
                foreach (DialogueChoiceButtonView button in _buttons)
                {
                    button.SetInteractable(false);
                }

                return;
            }

            RebuildButtons();

            // Layout должен пересчитаться до сброса прокрутки.
            Canvas.ForceUpdateCanvases();

            if (nodeChanged)
            {
                _lineScroll.StopMovement();
                _lineScroll.verticalNormalizedPosition = 1f;
            }

            _choicesScroll.StopMovement();
            _choicesScroll.verticalNormalizedPosition = 1f;
        }

        private void RebuildButtons()
        {
            ClearButtons();

            int number = 1;

            foreach (DialogueChoice choice in _runner.AvailableChoices)
            {
                DialogueChoiceButtonView button = Instantiate(
                    _choiceButtonPrefab,
                    _choicesContent);

                button.Bind(choice, number, HandleChoiceClicked);
                button.SetInteractable(true);

                _buttons.Add(button);
                number++;
            }
        }

        private void ClearButtons()
        {
            foreach (DialogueChoiceButtonView button in _buttons)
            {
                if (button == null)
                {
                    continue;
                }

                // Destroy отложен до конца кадра.
                // Сразу исключаем старую кнопку из layout и ввода.
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }

            _buttons.Clear();
        }

        // async void используется только как обработчик UI-события.
        private async void HandleChoiceClicked(DialogueChoice choice)
        {
            if (_runner == null ||
                !_runner.IsActive ||
                _runner.IsBusy ||
                _handlingChoice)
            {
                return;
            }

            _handlingChoice = true;

            try
            {
                await _runner.ChooseAsync(choice.Id);
            }
            catch (OperationCanceledException)
            {
                // Штатная отмена: runner сам очищает состояние.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (this != null && _runner.IsActive)
                {
                    _statusText.text = "Не удалось выбрать ответ.";
                }
            }
            finally
            {
                _handlingChoice = false;
            }
        }

        private void HandleFinishClicked()
        {
            if (_runner != null && _runner.CanFinish)
            {
                _runner.FinishDialogue();
            }
        }

        private void OnDestroy()
        {
            if (_runner != null)
            {
                _runner.StateChanged -= Refresh;
            }

            if (_finishButton != null)
            {
                _finishButton.onClick.RemoveListener(HandleFinishClicked);
            }
        }
    }
}