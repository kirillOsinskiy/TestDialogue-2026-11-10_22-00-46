using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Dialogue
{
    public sealed class DialogueEffectExecutor
    {
        private readonly GameState _state;
        private readonly IDialogueScreenService _screenService;

        public DialogueEffectExecutor(
            GameState state,
            IDialogueScreenService screenService)
        {
            _state = state
                ?? throw new ArgumentNullException(nameof(state));

            _screenService = screenService
                ?? throw new ArgumentNullException(nameof(screenService));
        }

        /// <summary>
        /// Последовательно выполняет последствия.
        /// Не записывает выбор и не выполняет переход между узлами.
        /// </summary>
        public async Task ExecuteAsync(
            IReadOnlyList<DialogueEffect> effects,
            CancellationToken cancellationToken = default)
        {
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Проверяем весь список до первых изменений состояния.
            ValidateEffects(effects);

            for (int i = 0; i < effects.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DialogueEffect effect = effects[i];

                switch (effect.Type)
                {
                    case DialogueEffectType.ChangeCommunityReputation:
                        _state.ChangeCommunityReputation(
                            effect.Key,
                            effect.IntValue);
                        break;

                    case DialogueEffectType.SetStoryFlag:
                        _state.SetStoryFlag(
                            effect.Key,
                            effect.BoolValue);
                        break;

                    case DialogueEffectType.AddKnowledge:
                        _state.AddKnowledge(effect.Key);
                        break;

                    case DialogueEffectType.GiveQuest:
                        _state.GiveQuest(effect.Key);
                        break;

                    case DialogueEffectType.OpenScreen:
                        await _screenService.OpenAsync(
                            effect.Key,
                            cancellationToken);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(effect.Type),
                            effect.Type,
                            "Неизвестный тип последствия.");
                }
            }

            // Учитываем отмену во время последнего открытия экрана,
            // даже если сервис завершил ожидание обычным образом.
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static void ValidateEffects(
            IReadOnlyList<DialogueEffect> effects)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                DialogueEffect effect = effects[i];

                if (effect == null)
                {
                    throw new ArgumentException(
                        $"Последствие с индексом {i} равно null.",
                        nameof(effects));
                }

                if (string.IsNullOrWhiteSpace(effect.Key))
                {
                    throw new ArgumentException(
                        $"У последствия с индексом {i} отсутствует Key.",
                        nameof(effects));
                }

                switch (effect.Type)
                {
                    case DialogueEffectType.ChangeCommunityReputation:
                    case DialogueEffectType.SetStoryFlag:
                    case DialogueEffectType.AddKnowledge:
                    case DialogueEffectType.GiveQuest:
                    case DialogueEffectType.OpenScreen:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(effect.Type),
                            effect.Type,
                            $"Неизвестный тип последствия " +
                            $"с индексом {i}.");
                }
            }
        }
    }
}