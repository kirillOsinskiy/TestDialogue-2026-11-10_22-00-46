using System;

namespace Game.Dialogue
{
    public static class DialogueChoiceHistoryKey
    {
        public static string Build(
            string dialogueId,
            string choiceId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                throw new ArgumentException(
                    "Id диалога не может быть пустым.",
                    nameof(dialogueId));
            }

            if (string.IsNullOrWhiteSpace(choiceId))
            {
                throw new ArgumentException(
                    "Id ответа не может быть пустым.",
                    nameof(choiceId));
            }

            // choiceId уже содержит node_id.choice_id.
            return $"{dialogueId}.{choiceId}";
        }
    }
}