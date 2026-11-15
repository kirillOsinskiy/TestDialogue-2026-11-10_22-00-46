using System;
using System.Collections.Generic;

namespace Game.Dialogue
{
    public sealed class GameState
    {
        private readonly HashSet<string> _selectedChoices =
            new HashSet<string>(StringComparer.Ordinal);

        private readonly HashSet<string> _knowledge =
            new HashSet<string>(StringComparer.Ordinal);

        private readonly HashSet<string> _quests =
            new HashSet<string>(StringComparer.Ordinal);

        private readonly Dictionary<string, int> _communityReputation =
            new Dictionary<string, int>(StringComparer.Ordinal);

        private readonly Dictionary<string, bool> _storyFlags =
            new Dictionary<string, bool>(StringComparer.Ordinal);

        // История выборов
        public bool HasSelectedChoice(string choiceId)
        {
            ValidateId(choiceId, nameof(choiceId));
            return _selectedChoices.Contains(choiceId);
        }

        public bool RecordChoice(string choiceId)
        {
            ValidateId(choiceId, nameof(choiceId));
            return _selectedChoices.Add(choiceId);
        }

        // Полученная информация
        public bool HasKnowledge(string knowledgeId)
        {
            ValidateId(knowledgeId, nameof(knowledgeId));
            return _knowledge.Contains(knowledgeId);
        }

        public bool AddKnowledge(string knowledgeId)
        {
            ValidateId(knowledgeId, nameof(knowledgeId));
            return _knowledge.Add(knowledgeId);
        }

        // Задачи
        public bool HasQuest(string questId)
        {
            ValidateId(questId, nameof(questId));
            return _quests.Contains(questId);
        }

        public bool GiveQuest(string questId)
        {
            ValidateId(questId, nameof(questId));
            return _quests.Add(questId);
        }

        // Отношение общин
        public int GetCommunityReputation(string communityId)
        {
            ValidateId(communityId, nameof(communityId));

            return _communityReputation.TryGetValue(
                communityId, out int value)
                ? value
                : 0;
        }

        public void SetCommunityReputation(
            string communityId,
            int value)
        {
            ValidateId(communityId, nameof(communityId));
            _communityReputation[communityId] = value;
        }

        public void ChangeCommunityReputation(
            string communityId,
            int delta)
        {
            int currentValue = GetCommunityReputation(communityId);

            _communityReputation[communityId] = currentValue + delta;
        }

        // Сюжетные флаги
        public bool GetStoryFlag(string flagId)
        {
            ValidateId(flagId, nameof(flagId));

            return _storyFlags.TryGetValue(flagId, out bool value)
                && value;
        }

        public void SetStoryFlag(string flagId, bool value)
        {
            ValidateId(flagId, nameof(flagId));
            _storyFlags[flagId] = value;
        }

        // Для нового прохождения / отладки.
        public void Reset()
        {
            _selectedChoices.Clear();
            _knowledge.Clear();
            _quests.Clear();
            _communityReputation.Clear();
            _storyFlags.Clear();
        }

        private static void ValidateId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Идентификатор не может быть пустым.",
                    parameterName);
            }
        }
    }
}