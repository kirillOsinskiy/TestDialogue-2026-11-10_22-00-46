using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialogue
{

    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Game/Dialogue/Dialogue")]
    public class DialogueAsset : ScriptableObject
    {
        [Tooltip("Стабильный идентификатор диалога.")]
        public string Id = Guid.NewGuid().ToString("N");

        public string DisplayName;

        [Tooltip("Имя говорящего персонажа.")]
        public string SpeakerName;

        [Tooltip("Идентификатор стартового узла.")]
        public string EntryNodeId;

        public List<DialogueNode> Nodes = new List<DialogueNode>();
    }

    [Serializable]
    public sealed class DialogueNode
    {
        [Tooltip("Стабильный идентификатор узла. Не зависит от текста.")]
        public string Id = Guid.NewGuid().ToString("N");

        [TextArea(3, 8)]
        public string Text;

        [Tooltip("Если ответов нет, реплика завершает диалог.")]
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
    }

    [Serializable]
    public sealed class DialogueChoice
    {
        [Tooltip("Стабильный ID для записи в историю выборов.")]
        public string Id = Guid.NewGuid().ToString("N");

        [TextArea(2, 5)]
        public string Text;

        public DialogueConditionSet Availability = new DialogueConditionSet();

        [Tooltip("Последствия выполняются в порядке списка.")]
        public List<DialogueEffect> Effects = new List<DialogueEffect>();

        public DialogueDestination Destination = new DialogueDestination();
    }

    // OR между группами.
    [Serializable]
    public sealed class DialogueConditionSet
    {
        [Tooltip("Достаточно выполнения одной группы. Пустой список означает отсутствие ограничений.")]
        public List<DialogueConditionGroup> AnyOf = new List<DialogueConditionGroup>();
    }

    // AND внутри группы.
    [Serializable]
    public sealed class DialogueConditionGroup
    {
        [Tooltip("Должны выполниться все условия этой группы.")]
        public List<DialogueCondition> AllOf = new List<DialogueCondition>();
    }

    [Serializable]
    public sealed class DialogueCondition
    {
        public DialogueConditionType Type;

        [Tooltip("ID выбора, общины, информации или сюжетного флага в зависимости от Type.")]
        public string Key;

        [Tooltip("Для ChoiceSelected, Knowledge и StoryFlag. False означает проверку отсутствия / ложного значения.")]
        public bool ExpectedValue = true;

        [Tooltip("Используется только для CommunityReputation.")]
        public DialogueComparison Comparison = DialogueComparison.GreaterOrEqual;

        [Tooltip("Используется только для CommunityReputation.")]
        public int IntValue;
    }

    public enum DialogueConditionType
    {
        ChoiceSelected = 0,
        CommunityReputation = 1,
        Knowledge = 2,
        StoryFlag = 3
    }

    public enum DialogueComparison
    {
        Equal = 0,
        NotEqual = 1,
        Greater = 2,
        GreaterOrEqual = 3,
        Less = 4,
        LessOrEqual = 5
    }

    [Serializable]
    public sealed class DialogueEffect
    {
        public DialogueEffectType Type;

        [Tooltip("ID общины, флага, информации, задачи или экрана в зависимости от Type.")]
        public string Key;

        [Tooltip("Изменение отношения для ChangeCommunityReputation. Допускаются отрицательные значения.")]
        public int IntValue;

        [Tooltip("Значение флага для SetStoryFlag.")]
        public bool BoolValue = true;
    }

    public enum DialogueEffectType
    {
        ChangeCommunityReputation = 0,
        SetStoryFlag = 1,
        AddKnowledge = 2,
        GiveQuest = 3,
        OpenScreen = 4
    }

    [Serializable]
    public sealed class DialogueDestination
    {
        public DialogueDestinationType Type = DialogueDestinationType.NextNode;

        [Tooltip("Используется только для перехода NextNode.")]
        public string NodeId;
    }

    public enum DialogueDestinationType
    {
        EndDialogue = 0,
        NextNode = 1
    }
}