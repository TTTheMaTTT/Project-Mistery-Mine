using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Компонент, ответственный за работу игровых событий, квестов и обучения
/// </summary>
public class GameHistory : MonoBehaviour, IHaveStory
{
    #region fields

    public History history = new History();

    #endregion //fields


    public void Awake()
    {
        history.PreInitialize();
    }

    public void Start()
    {
    }

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public virtual List<string> actionNames()
    {
        return new List<string>() { "changeQuestData", "removeObject" };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() {
                                                    { "changeQuestData", new List<string>() {"add","continue","complete" } },
                                                    { "removeObject", new List<string>(){ } } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() {
                                                    { "changeQuestData", (SpecialFunctions.statistics.database != null?
                                                                                        SpecialFunctions.statistics.database.quests.ConvertAll<string>(x=>x.questName):
                                                                                        new List<string>())},
                                                    {"removeOject", new List<string>()} };
    }

    public virtual Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>();
    }

    #endregion //IHaveStory

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
    
    //Возвращает список типов, которые могут быть причиной события
    public virtual List<string> storyTypes
    {
        get
        {
            return new List<string>() { "GameController", "GameStatistics", "CharacterController", "StoryTrigger", "NPCController" };
        }
    }

    //Возвращает имена инциализирующих функций для создания причины сюжетного события
    public virtual Dictionary<Type, List<string>> initNames { get { return new Dictionary<Type, List<string>>() {
                                                                                    { typeof(GameController), new List<string> {"startGame" } },
                                                                                    { typeof(GameStatistics), new List<string> {"statisticCount"} },
                                                                                    { typeof(CharacterController),new List<string> { "characterDeath"} },
                                                                                    { typeof(StoryTrigger),new List<string> {"triggerEvent"} },
                                                                                    { typeof(NPCController),new List<string> {"speech" } } }; } }
    //Возвращает имена сравнивающих функций для настройки причины сюжетного события
    public virtual Dictionary<Type, List<string>> compareNames
    {
        get
        {
            return new Dictionary<Type, List<string>> {
                                                                                    { typeof(GameController), new List<string> {"","compare"} },
                                                                                    { typeof(GameStatistics), new List<string> {"", "compare", "compareStatistics", } },
                                                                                    { typeof(CharacterController),new List<string> { "", "compare" } },
                                                                                    { typeof(StoryTrigger),new List<string> { "", "compare" } },
                                                                                    { typeof(NPCController),new List<string> { "", "compare", "compareSpeech"} } };
        }
    }

    //Словарь функций проверки сюжетных условий
    private static Dictionary<string, storyConditionDelegate> storyConditionBase = new Dictionary<string, storyConditionDelegate> ();

    private Dictionary<string, storyInitDelegate> storyInitBase = new Dictionary<string, storyInitDelegate>();//подписка
    private Dictionary<string, storyInitDelegate> storyDeInitBase = new Dictionary<string, storyInitDelegate>();//отписка

    #endregion //dictionaries

    #region fields

    public List<Story> storyList = new List<Story>();

    public List<StoryInitializer> initList = new List<StoryInitializer>();
    public List<StoryInitializer> InitList { get { return initList; } }

    [Space(10)]
    [SerializeField]
    protected List<Quest> activeQuests = new List<Quest>();
    public List<Quest> ActiveQuests { get { return activeQuests; } }
      
    #endregion fields

    #region interface

    public void PreInitialize()
    {
        FormStoryBase();
        SpecialFunctions.statistics.StatisticCountEvent += HandleStatisticCountEvent;
    }

    //то, что вызывается в самом начале
    public void Initialize()
    {

        foreach (Story _story in storyList)
        {
            InitializeScript(_story);
        }
    }

    /// <summary>
    /// Найти инициализатор, соответствующий данной истории
    /// </summary>
    public StoryInitializer FindInitializer(Story _story)
    {
        return initList.Find(x => (x.story.storyName == _story.storyName));
    }

    /// <summary>
    /// Найти инициализатор, соответствующий данному названию истории
    /// </summary>
    public StoryInitializer FindInitializer(string storyName)
    {
        return initList.Find(x => (x.story.storyName == storyName));
    }

    /// <summary>
    /// Сформировать базы данных
    /// </summary>
    public void FormStoryBase()
    {

        storyActionBase.Add("changeQuestData", ChangeQuestData);
        storyActionBase.Add("removeObject", RemoveHistoryObject);

        storyConditionBase.Clear();
        storyConditionBase.Add("compare", Compare);
        storyConditionBase.Add("compareSpeech", CompareSpeech);
        storyConditionBase.Add("compareStatistics", CompareStatistics);

        storyInitBase.Add("startGame", (x,y)=> { if(y.GetComponent<GameStatistics>()!=null) y.GetComponent<GameController>().StartGameEvent += x.HandleStoryEvent; });
        storyInitBase.Add("statisticCount", (x, y) => { if (y.GetComponent<GameStatistics>() != null) y.GetComponent<GameStatistics>().StatisticCountEvent += x.HandleStoryEvent; });
        storyInitBase.Add("characterDeath", (x, y) => { if (y.GetComponent<CharacterController>()!=null) y.GetComponent<CharacterController>().CharacterDeathEvent += x.HandleStoryEvent; });
        storyInitBase.Add("triggerEvent", (x, y) => { if (y.GetComponent<StoryTrigger>() != null) y.GetComponent<StoryTrigger>().TriggerEvent += x.HandleStoryEvent; });
        storyInitBase.Add("speech", (x, y) => { if (y.GetComponent<NPCController>() != null) y.GetComponent<NPCController>().SpeechSaidEvent += x.HandleStoryEvent; });

        storyDeInitBase.Add("startGame", (x, y) => { if (y.GetComponent<GameStatistics>() != null) y.GetComponent<GameController>().StartGameEvent -= x.HandleStoryEvent; });
        storyDeInitBase.Add("statisticCount", (x, y) => { if (y.GetComponent<GameStatistics>() != null) y.GetComponent<GameStatistics>().StatisticCountEvent -= x.HandleStoryEvent; });
        storyDeInitBase.Add("characterDeath", (x, y) => { if (y.GetComponent<CharacterController>() != null) y.GetComponent<CharacterController>().CharacterDeathEvent -= x.HandleStoryEvent; });
        storyDeInitBase.Add("triggerEvent", (x, y) => { if (y.GetComponent<StoryTrigger>() != null) y.GetComponent<StoryTrigger>().TriggerEvent -= x.HandleStoryEvent; });
        storyDeInitBase.Add("speech", (x, y) => { if (y.GetComponent<NPCController>() != null) y.GetComponent<NPCController>().SpeechSaidEvent -= x.HandleStoryEvent; });

    }

    /// <summary>
    /// Подписка всех событий происходит здесь. Сделав правильную подписку, мы реализуем историю игры
    /// </summary>
    public void InitializeScript(Story _story)
    {
        GameObject storyTarget = null;

        StoryInitializer init = FindInitializer(_story);
        if (init == null)
            return;

        storyTarget = init.eventReason;

        if (storyTarget == null)
            return;

        //В первую очередь, подпишемся на сюжетные объекты объекты
        if (storyInitBase.ContainsKey(_story.storyCondition.storyConditionName))
        {
            string s = _story.storyCondition.storyConditionName;
            storyInitBase[s].Invoke(_story, storyTarget);
        }

        for (int i=0;i<_story.storyActions.Count; i++)
        {
            if (i >= init.eventObjects.Count)
                break;
            StoryAction _action = _story.storyActions[i];

            GameObject obj = init.eventObjects[i];
            if (obj == null)
                continue;
            if (obj.GetComponent<GameHistory>() != null)
            {
                if (storyActionBase.ContainsKey(_action.actionName))
                {
                    string s = _action.actionName;
                    _action.storyAction = storyActionBase[s].Invoke;
                }
            }
            else if (obj.GetComponent<NPCController>() != null)
            {
                NPCController npc = obj.GetComponent<NPCController>();
                if (npc.StoryActionBase.ContainsKey(_action.actionName))
                {
                    string s = _action.actionName;
                    _action.storyAction = npc.StoryActionBase[s].Invoke;
                }
            }
            else if (obj.GetComponent<CharacterController>() != null)
            {
                CharacterController character = obj.GetComponent<CharacterController>();
                if (character.StoryActionBase.ContainsKey(_action.actionName))
                {
                    string s = _action.actionName;
                    _action.storyAction = character.StoryActionBase[s].Invoke;
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

        SpecialFunctions.statistics.InitializeAllStatistics();//Сразу же проверить выполнение условия, связанного со статистикой, для нового события
    }

    /// <summary>
    /// Убрать игровой объект из участия в событии, вызываемом классом Story 
    /// </summary>
    public void DeInitializeScript(Story _story)
    {
        StoryInitializer init = FindInitializer(_story);
        GameObject obj = init.eventReason;
        if (obj == null)
            return;

        //В первую очередь, отпишимся от журнальных объектов
        if (storyDeInitBase.ContainsKey(_story.storyCondition.storyConditionName))
        {
            string s = _story.storyCondition.storyConditionName;
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
            if (FindInitializer(_story.presequences[i])!=null? !FindInitializer(_story.presequences[i]).completed:false)
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
            FindInitializer(_story).completed = true;
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
        if ((_quest = SpecialFunctions.statistics.GetQuest(_action.id2)) != null)
        {
            switch (_action.id1)
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
                            if (quest1.questLine.Contains(_action.id1))
                            {
                                quest1.stage = quest1.questLine.IndexOf(_action.id1);
                            }
                        }
                        break;
                    }
            }
            SpecialFunctions.gameUI.ConsiderQuests(activeQuests.ConvertAll<string>(x => x.questLine[x.stage]));
            HandleStatisticCountEvent(this, new StoryEventArgs("", 0));
        }
    }

    /// <summary>
    /// Убрать объект
    /// </summary>
    public void RemoveHistoryObject(StoryAction _action)
    {
        GameObject dObj = GameObject.Find(_action.id1);
        if (dObj!=null)
            dObj.SetActive(false);
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
        return SpecialFunctions.ComprFunctionality(e.Argument, _condition.id2, _condition.argument);
    }

    /// <summary>
    /// Проверить, была ли сказана персонажем нужна нам реплика
    /// </summary>
    static bool CompareSpeech(StoryCondition _condition, StoryEventArgs e)
    {
        return (e.ID == _condition.id1);
    }

    /// <summary>
    /// Проверить учёт выбранной статистики
    /// </summary>
    static bool CompareStatistics(StoryCondition _condition, StoryEventArgs e)
    {
        if ((e.ID == _condition.id1))
            return SpecialFunctions.ComprFunctionality(e.Argument, _condition.id2, _condition.argument);
        else
            return false;
    }

    #endregion //conditionFunctions

    #region events

    protected void HandleStatisticCountEvent(object other, StoryEventArgs e)
    {
        List<string> questLines = new List<string>();
        foreach (Quest _quest in activeQuests)
        {
            string s = _quest.questLine[_quest.stage];
            if (_quest.hasStatistic && s.Contains("/"))
            {
                string s1 = s.Substring(0, s.LastIndexOf("/"));
                string s2 = s.Substring(s.LastIndexOf("/") + 1);
                if (_quest.statisticName == e.ID && _quest.questLine[_quest.stage].Contains("/"))
                {
                    _quest.statisticCount = e.Argument;
                }
                questLines.Add(s1 + _quest.statisticCount.ToString() + "/" + s2);
            }
            else
                questLines.Add(s);
        }
        SpecialFunctions.gameUI.ConsiderQuests(questLines);
    }

    #endregion //events

    #region saveSystem

    public void LoadHistory(StoryInfo sInfo, QuestInfo qInfo)
    {
        if (sInfo!=null)
        {
            storyList = new List<Story>();
            foreach (string _storyName in sInfo.stories)
            {
                StoryInitializer init = FindInitializer(_storyName);
                if (init != null)
                    storyList.Add(init.story);
            }

            foreach (string _storyName in sInfo.completedStories)
            {
                StoryInitializer init = FindInitializer(_storyName);
                if (init != null)
                    init.completed = true;
            }
        }

        if (qInfo != null)
        {
            activeQuests = new List<Quest>();
            foreach (string questName in qInfo.quests)
            {
                Quest _quest = null;
                if ((_quest = SpecialFunctions.statistics.GetQuest(questName)) != null)
                    activeQuests.Add(new Quest(_quest));
                SpecialFunctions.gameUI.ConsiderQuests(activeQuests.ConvertAll<string>(x => x.questLine[x.stage]));
            }
        }

    }

    #endregion //saveSystem

}

/// <summary>
/// Класс, инициализирующий истории
/// </summary>
[System.Serializable]
public class StoryInitializer
{
    public Story story;
    public GameObject eventReason;
    public List<GameObject> eventObjects;

    [NonSerialized][HideInInspector]
    public bool completed=false;
}



