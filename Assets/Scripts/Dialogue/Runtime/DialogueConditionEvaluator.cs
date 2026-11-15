using System;

namespace Game.Dialogue
{
    public sealed class DialogueConditionEvaluator
    {
        private readonly GameState _state;

        public DialogueConditionEvaluator(GameState state)
        {
            _state = state
                ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// OR между группами.
        /// Пустой список групп означает отсутствие ограничений.
        /// </summary>
        public bool Evaluate(DialogueConditionSet conditionSet, string dialogueId)
        {
            // Повреждённые данные не открывают ответ.
            if (conditionSet == null || conditionSet.AnyOf == null)
            {
                return false;
            }

            if (conditionSet.AnyOf.Count == 0)
            {
                return true;
            }

            foreach (DialogueConditionGroup group in conditionSet.AnyOf)
            {
                if (EvaluateGroup(group, dialogueId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Проверка одного условия без изменения состояния.
        /// </summary>
        public bool EvaluateCondition(DialogueCondition condition, string dialogueId)
        {
            if (condition == null ||
                string.IsNullOrWhiteSpace(condition.Key))
            {
                return false;
            }

            switch (condition.Type)
            {
                case DialogueConditionType.ChoiceSelected:
                    return _state.HasSelectedChoice(condition.Key)
                        == condition.ExpectedValue;

                case DialogueConditionType.CommunityReputation:
                    return Compare(
                        _state.GetCommunityReputation(condition.Key),
                        condition.IntValue,
                        condition.Comparison);

                case DialogueConditionType.Knowledge:
                    return _state.HasKnowledge(condition.Key)
                        == condition.ExpectedValue;

                case DialogueConditionType.StoryFlag:
                    return _state.GetStoryFlag(condition.Key)
                        == condition.ExpectedValue;

                default:
                    return false;
            }
        }

        /// <summary>
        /// AND внутри группы.
        /// Пустая группа считается выполненной.
        /// </summary>
        private bool EvaluateGroup(DialogueConditionGroup group, string dialogueId)
        {
            if (group == null || group.AllOf == null)
            {
                return false;
            }

            foreach (DialogueCondition condition in group.AllOf)
            {
                if (!EvaluateCondition(condition, dialogueId))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Compare(
            int actual,
            int expected,
            DialogueComparison comparison)
        {
            switch (comparison)
            {
                case DialogueComparison.Equal:
                    return actual == expected;

                case DialogueComparison.NotEqual:
                    return actual != expected;

                case DialogueComparison.Greater:
                    return actual > expected;

                case DialogueComparison.GreaterOrEqual:
                    return actual >= expected;

                case DialogueComparison.Less:
                    return actual < expected;

                case DialogueComparison.LessOrEqual:
                    return actual <= expected;

                default:
                    return false;
            }
        }
    }
}