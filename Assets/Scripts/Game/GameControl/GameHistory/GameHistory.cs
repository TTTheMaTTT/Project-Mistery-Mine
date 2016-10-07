using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Компонент, ответственный за работу игровых событий, квестов и обучения
/// </summary>
public class GameHistory : MonoBehaviour
{
    #region fields

    public History history = new History();

    #endregion //fields


    public void Awake()
    {
        history.Initialize();
        history.FormJournalBase();
    }

}

/// <summary>
/// Особый список журнальных сриптов, в котором добавлены ф-ции при добавлении новых событий
/// </summary>
[System.Serializable]
public class History
{

    #region delegates

    private delegate bool storyConditionDelegate(StoryCondition _condition, StoryEventArgs e);

    public delegate void storyActionDelegate(StoryAction _action);

    private delegate void storyInitDelegate(Story _story, GameObject obj);

    #endregion //delegates

    #region dictionaries

    private Dictionary<string, storyActionDelegate> storyActionBase = new Dictionary<string, storyActionDelegate>(); //Словарь сюжетных действий

    //Словарь функций проверки сюжетных условий
    private static Dictionary<string, storyConditionDelegate> storyConditionBase = new Dictionary<string, storyConditionDelegate> { { "compare", Compare } };

    private Dictionary<string, storyInitDelegate> storyInitBase = new Dictionary<string, storyInitDelegate>();//подписка
    private Dictionary<string, storyInitDelegate> storyDeInitBase = new Dictionary<string, storyInitDelegate>();//отписка

    #endregion //dictionaries

    #region fields

    public List<StoryInitializer> initList = new List<StoryInitializer>();
    public List<Story> storyList = new List<Story>();

    [Space(10)]
    [SerializeField]
    protected List<Quest> activeQuests = new List<Quest>();

    #endregion fields

    #region interface

    //то, что вызывается в самом начале
    public void Initialize()
    {
        foreach (Story _story in storyList)
        {
            InitializeScript(_story);
        }
    }

    /// <summary>
    /// Сформировать базы данных
    /// </summary>
    public void FormJournalBase()
    {

        storyActionBase.Add("changeQuestsData", ChangeQuestData);

        storyConditionBase.Add("compare", Compare);
        storyConditionBase.Add("compareSpeech", CompareSpeech);

        storyInitBase.Add("startGame", (x,y)=> { if(y.GetComponent<GameStatistics>()!=null) y.GetComponent<GameStatistics>().StartGameEvent += x.HandleStoryEvent; });
        storyInitBase.Add("characterDeath", (x, y) => { if (y.GetComponent<CharacterController>()!=null) y.GetComponent<CharacterController>().CharacterDeathEvent += x.HandleStoryEvent; });
        storyInitBase.Add("triggerEvent", (x, y) => { if (y.GetComponent<StoryTrigger>() != null) y.GetComponent<StoryTrigger>().TriggerEvent += x.HandleStoryEvent; });
        storyInitBase.Add("speech", (x, y) => { if (y.GetComponent<NPCController>() != null) y.GetComponent<NPCController>().SpeechSaidEvent += x.HandleStoryEvent; });

        storyDeInitBase.Add("startGame", (x, y) => { if (y.GetComponent<GameStatistics>() != null) y.GetComponent<GameStatistics>().StartGameEvent -= x.HandleStoryEvent; });
        storyDeInitBase.Add("characterDeath", (x, y) => { if (y.GetComponent<CharacterController>() != null) y.GetComponent<CharacterController>().CharacterDeathEvent -= x.HandleStoryEvent; });
        storyDeInitBase.Add("triggerEvent", (x, y) => { if (y.GetComponent<StoryTrigger>() != null) y.GetComponent<StoryTrigger>().TriggerEvent -= x.HandleStoryEvent; });
        storyDeInitBase.Add("speech", (x, y) => { if (y.GetComponent<NPCController>() != null) y.GetComponent<NPCController>().SpeechSaidEvent -= x.HandleStoryEvent; });

    }

    /// <summary>
    /// Подписка всех событий происходит здесь. Сделав правильную подписку, мы реализуем историю игры
    /// </summary>
    public void InitializeScript(Story _story)
    {
        StoryInitializer storyInit = null;
        GameObject storyTarget = null;

        storyInit = initList.Find(x => (x.story == _story));

        if (storyInit == null)
        {
            return;
        }

        storyTarget = storyInit.eventReason;
        //В первую очередь, подпишемся на журнальные объекты
        if (storyInitBase.ContainsKey(_story.storyCondition.conditionName))
        {
            string s = _story.storyCondition.conditionName;
            storyInitBase[s].Invoke(_story, storyTarget);
        }

        for (int i=0;i<_story.storyActions.Count; i++)
        {
            StoryAction _action = _story.storyActions[i];
            _action= null;
            if (i > storyInit.eventObj.Count)
            {
                break;
            }
            GameObject obj = storyInit.eventObj[i];
            if (obj.GetComponent<GameHistory>() != null)
            {
                if (storyActionBase.ContainsKey(_action.storyActionName))
                {
                    string s = _action.storyActionName;
                    _action.storyAction = storyActionBase[s].Invoke;
                }
            }
            else if (obj.GetComponent<NPCController>() != null)
            {
                NPCController npc = obj.GetComponent<NPCController>();
                if (npc.StoryActionBase.ContainsKey(_action.storyActionName))
                {
                    string s = _action.storyActionName;
                    _action.storyAction = npc.StoryActionBase[s].Invoke;
                }      
            }
        }

        if (storyConditionBase.ContainsKey(_story.storyCondition.conditionName))
        {
            string s = _story.storyCondition.conditionName;
            _story.storyCondition.storyCondition = storyConditionBase[s].Invoke;
        }
        else
        {
            _story.storyCondition.storyCondition = Empty;
        }
    }

    /// <summary>
    /// Убрать игровой объект из участия в событии, вызываемом классом Story 
    /// </summary>
    public void DeInitializeScript(Story _story)
    {
        StoryInitializer storyInit = initList.Find(x => (x.story == _story));

        if (storyInit == null)
            return;

        GameObject obj = storyInit.eventReason;

        //В первую очередь, отпишимся от журнальных объектов
        if (storyDeInitBase.ContainsKey(_story.storyCondition.conditionName))
        {
            string s = _story.storyCondition.conditionName;
            storyDeInitBase[s].Invoke(_story, obj);
        }

        foreach (StoryAction _action in _story.storyActions)
        {
            _action.storyAction = null;
        }

        _story.storyCondition.storyCondition = null;

    }

    /// <summary>
    /// Добавить в список новую историю
    /// </summary>
    public void AddStory(Story _story)
    {
        for (int i = 0; i < _story.presequences.Count; i++)
        {
            if (!FindStoryInitializer(_story.presequences[i].storyName).completed)
                return;
        }

        storyList.Add(_story);
        InitializeScript(_story);
    }

    /// <summary>
    /// Убрать из списка произошедшую историю
    /// </summary>
    public void RemoveCompletedStory(Story _story)
    {
        if (storyList.Contains(_story))
        {
            FindStoryInitializer(_story.storyName).completed = true;
            storyList.Remove(_story);
            DeInitializeScript(_story);
        }
    }

    /// <summary>
    /// Убрать из списка историю
    /// </summary>
    public void RemoveStory(Story _story)
    {
        if (storyList.Contains(_story))
        {
            storyList.Remove(_story);
            DeInitializeScript(_story);
        }
    }

    /// <summary>
    /// Найти инициализатор данной истории
    /// </summary>
    protected StoryInitializer FindStoryInitializer(string _storyName)
    {
        return initList.Find(x => x.story.storyName == _storyName);
    }

    /// <summary>
    /// Определить, есть ли данный квест в списке активных квестов
    /// </summary>
    protected bool ContainsQuest(Quest _quest)
    {
        Quest quest1 = null;
        quest1 = activeQuests.Find(x => (x.questName == _quest.questName));
        return (quest1 != null);
    }

    #endregion //interface

    #region storyActions

    /// <summary>
    /// Изменить данные о квестах
    /// </summary>
    public void ChangeQuestData(StoryAction _action)
    {
        Quest _quest = null;
        if ((_quest = SpecialFunctions.statistics.GetQuest(_action.id1)) != null)
        {
            switch (_action.id2)
            {
                case "add":
                    {
                        if (!ContainsQuest(_quest))
                        {
                            activeQuests.Add(new Quest(_quest));
                        }
                        break;
                    }
                case "continue":
                    {
                        if (ContainsQuest(_quest))
                        {
                            Quest quest1 = activeQuests.Find(x => (x.questName == _quest.questName));
                            int questStage=quest1.stage++;
                            if (questStage >= quest1.questLine.Count)
                            {
                                activeQuests.Remove(quest1);
                            }
                        }
                        break;
                    }
                case "complete":
                    {
                        if (ContainsQuest(_quest))
                        {
                            activeQuests.Remove(activeQuests.Find(x => (x.questName == _quest.questName)));
                        }
                        break;
                    }
                default:
                    {
                        if (ContainsQuest(_quest))
                        {
                            Quest quest1 = activeQuests.Find(x => (x.questName == _quest.questName));
                            if (quest1.questLine.Contains(_action.id2))
                            {
                                quest1.stage = quest1.questLine.IndexOf(_action.id2);
                            }
                        }
                        break;
                    }
            }
        }
    }

    #endregion //storyActions

    #region conditionFunctions

    /// <summary>
    /// Пустая функция на тот случай, если не окажется подходящих функций проверки условия
    /// </summary>
    static bool Empty(StoryCondition _condition, StoryEventArgs e)
    {
        return true;
    }

    /// <summary>
    /// Сравнить 2 числа
    /// </summary>
    static bool Compare(StoryCondition _condition, StoryEventArgs e)
    {
        return SpecialFunctions.ComprFunctionality(e.Argument, _condition.id, _condition.argument);
    }

    /// <summary>
    /// Проверить, была ли сказана персонажем нужна нам реплика
    /// </summary>
    static bool CompareSpeech(StoryCondition _condition, StoryEventArgs e)
    {
        return (e.ID == _condition.id);
    }


    #endregion //conditionFunctions

}


/// <summary>
/// Класс, необходимый для инициализации сюжетных скриптов и учёта их выполнения
/// </summary>
[System.Serializable]
public class StoryInitializer
{
    public Story story;
    public GameObject eventReason;//Какой объект вызовет событие?
    public List<GameObject> eventObj;//Какие объекты подключатся к событию?

    public bool completed = false;//Был ли выполнен данный сюжетный скрипт?
}


