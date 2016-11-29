using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// Класс, представляющий собой некий сюжет - что-то произошло и вызвало череду событий. Вообще Story - базовый элемент для построений цепочки игровых событий
/// </summary>
[System.Serializable]
public class Story : ScriptableObject
{
    #region fields

    public string storyName;
    public string sceneName;//для какой сцены предназначен эта история?

    public List<StoryAction> storyActions = new List<StoryAction>();//Сюжетные действия

    public StoryCondition storyCondition = new StoryCondition();//Сюжетная причина для действия

    public List<Story> presequences = new List<Story>();//какие сюжетные события должны произойти, чтобы было возможно свершение данной истории

    public List<Story> consequences = new List<Story>();//К каким последствиям приведёт данный сюжетный поворот 
                                                                                  //(как будет развиваться история, какие новые сюжетные скрипты будут использованы в дальнейшем)

    public List<Story> nonConsequences = new List<Story>();//Какие события становятся невозможными, если приведётся в исполнение данный журнальный скрипт?

    #endregion //fields

    /// <summary>
    /// Обработать событие "Причинно-следственная связь"
    /// </summary>
    public void HandleStoryEvent(object sender, StoryEventArgs e)
    {

        History history = SpecialFunctions.history;

        if (!storyCondition.storyCondition(storyCondition,e))
        {
            return;
        }

        foreach (StoryAction _action in storyActions)
        {
            _action.storyAction.Invoke(_action);
        }

        history.RemoveCompletedStory(this);
        foreach (Story _script in consequences)
        {
            history.AddStory(_script);
        }
        foreach (Story _script in nonConsequences)
        {
            history.RemoveStory(_script);
        }
    }
}

/// <summary>
/// Класс, поясняющий что должно произойти в игре, когда было вызвано действие класса Story
/// </summary>
[System.Serializable]
public class StoryAction
{
    public delegate void StoryActionDelegate(StoryAction _action);

    public string storyActionName;

    public string actionName;//имя действия производимого историей
    public string id1, id2; //параметры, что использует
    public int argument;//данное действие
    public GameObject gameObj;//с каким игровым объектом произвести действие
    public StoryActionDelegate storyAction;//ссылка на функцию, которая соответствует названию выше

}

/// <summary>
/// Класс, поясняющий что должно произойти в игре, чтобы вызвать действие класса Story
/// </summary>
[System.Serializable]
public class StoryCondition
{
    public delegate bool StoryConditionDelegate(StoryCondition _condition,  StoryEventArgs e);

    public string storyConditionName;

    public string conditionName;//имя функции проверки сюжетного события
    public string id;    //параметры, что использует
    public int argument;//данное действие
    public GameObject obj;//с каким объектом произвести действие
    public StoryConditionDelegate storyCondition;//ссылка на функцию, которая соответствует названию выше

}


#if UNITY_EDITOR
[CustomEditor(typeof(Story))]
public class CustomStoryEditor : Editor
{

    int actionSize, presequencesSize, consequencesSize, nonsequencesSize;
    string sceneName;

    Story story;
    History history = null;
    StoryInitializer init = null;

    List<int> actionNamesIndexes = new List<int>(), actionID1Indexes = new List<int>(), actionID2Indexes=new List<int>();

    int initNameIndex = -1, conditionNameIndex = -1, conditionIDIndex=-1;
    List<string> conditionNames = new List<string>(), initNames = new List<string>(), conditionIDs=new List<string>();
    IHaveStory currentConditionObject = null;//Объект, который является инициатором сюжетного события

    public void OnEnable()
    {
        if (story == null)
            story = (Story)target;

        List<StoryInitializer> initList = null;

        if (GameObject.FindGameObjectWithTag("gameController") != null)
        {
            history = SpecialFunctions.history;
            if (history != null)
                initList = history.InitList;
        }

        if (story.storyActions == null)
        {
            story.storyActions = new List<StoryAction>();
            actionNamesIndexes = new List<int>();
            actionID1Indexes = new List<int>();
            actionID2Indexes = new List<int>();
        }
        if (story.presequences == null)
        {
            story.presequences = new List<Story>();
        }
        if (story.consequences == null)
        {
            story.consequences = new List<Story>();
        }
        if (story.nonConsequences == null)
        {
            story.nonConsequences = new List<Story>(); ;
        }
        if (story.storyCondition == null)
        {
            story.storyCondition = new StoryCondition();
        }

        sceneName = SceneManager.GetActiveScene().name;

        if (story.sceneName == SceneManager.GetActiveScene().name)
        {
            if (initList != null)
            {
                init = history.FindInitializer(story);
                if (init == null)
                {
                    init = new StoryInitializer();
                    init.story = story;
                    init.eventObjects = new List<GameObject>();
                    initList.Add(init);
                }
                if (init.eventObjects.Count != story.storyActions.Count)
                {
                    int m = init.eventObjects.Count;
                    for (int i = m; i < story.storyActions.Count; i++)
                    {
                        init.eventObjects.Add(null);
                    }
                    for (int i = m - 1; i >= story.storyActions.Count; i--)
                    {
                        init.eventObjects.RemoveAt(i);
                    }
                }
            }
        }

        #region reinitialize

        if (actionNamesIndexes.Count != story.storyActions.Count)
        {
            actionNamesIndexes = new List<int>();
            actionID1Indexes = new List<int>();
            actionID2Indexes = new List<int>();

            for (int i = 0; i < story.storyActions.Count; i++)
                CheckGameObjectActions(story.storyActions[i], i);

        }

        CheckGameObjectCondition(story.storyCondition);

        #endregion //reinitialize
    }

    public void OnDestroy()
    {
        actionNamesIndexes = new List<int>();
        actionID1Indexes = new List<int>();
        actionID2Indexes = new List<int>();
        story = null;
        init = null;
    }

    public override void OnInspectorGUI()
    {

        story.storyName = EditorGUILayout.TextField("story name", story.storyName);

        story.sceneName = EditorGUILayout.TextField("scene name", story.sceneName);

        EditorGUILayout.Space();

        #region storyActions

        EditorGUILayout.HelpBox("story actions",MessageType.Info);

        actionSize = story.storyActions.Count;
        actionSize = EditorGUILayout.IntField("action size", actionSize);
        if (actionSize !=story.storyActions.Count)
        {
            int m = story.storyActions.Count;
            for (int i = m; i < actionSize; i++)
            {
                story.storyActions.Add(new StoryAction());
                CheckGameObjectActions(story.storyActions[i], i);
            }
            for (int i = m - 1; i >= actionSize; i--)
            {
                story.storyActions.RemoveAt(i);
                ChangeActionIndexesCount(-1);
            }
        }

        if (init!=null? init.eventObjects.Count != story.storyActions.Count:false)
        {
            int m = init.eventObjects.Count;
            for (int i = m; i < story.storyActions.Count; i++)
            {
                init.eventObjects.Add(null);
            }
            for (int i = m - 1; i >= story.storyActions.Count; i--)
            {
                init.eventObjects.RemoveAt(i);
            }
        }

        for (int i = 0; i <story.storyActions.Count;i++)
        {

            StoryAction _action = story.storyActions[i];
            _action.storyActionName = EditorGUILayout.TextField("story action name", _action.storyActionName);

            GameObject actionObject = _action.gameObj;
            IHaveStory storyObject = null;

            if (actionObject != null)
                storyObject = actionObject.GetComponent<IHaveStory>();

            if (storyObject != null)
            {
                int newActionIndex = EditorGUILayout.Popup(actionNamesIndexes[i], storyObject.actionNames().ToArray());
                if (newActionIndex != actionNamesIndexes[i])
                {
                    actionNamesIndexes[i] = newActionIndex;
                    _action.actionName = storyObject.actionNames()[newActionIndex];

                    actionID1Indexes[i] = -1;
                    actionID2Indexes[i] = -1;
                    _action.id1 = string.Empty;
                    _action.id2 = string.Empty;

                }
            }
            _action.actionName= EditorGUILayout.TextField("action name", _action.actionName);

            if (storyObject!=null? storyObject.actionIDs1().ContainsKey(_action.actionName):false)
            {
                List<string> IDs = storyObject.actionIDs1()[_action.actionName];
                if (IDs.Count > 0)
                {
                    int newIDIndex = EditorGUILayout.Popup(actionID1Indexes[i], IDs.ToArray());
                    if (newIDIndex != actionID1Indexes[i])
                    {
                        actionID1Indexes[i] = newIDIndex;
                        _action.id1 = IDs[newIDIndex];
                    }
                }
            }
            _action.id1 = EditorGUILayout.TextField("id1", _action.id1);

            if (storyObject != null ? storyObject.actionIDs2().ContainsKey(_action.actionName) : false)
            {
                List<string> IDs = storyObject.actionIDs2()[_action.actionName];
                if (IDs.Count > 0)
                {
                    int newIDIndex = EditorGUILayout.Popup(actionID2Indexes[i], IDs.ToArray());
                    if (newIDIndex != actionID2Indexes[i])
                    {
                        actionID2Indexes[i] = newIDIndex;
                        _action.id2 = IDs[newIDIndex];
                    }
                }
            }
            _action.id2 = EditorGUILayout.TextField("id2", _action.id2);

            _action.argument=EditorGUILayout.IntField("argument",_action.argument);

            GameObject newObj=(GameObject)EditorGUILayout.ObjectField("action object",_action.gameObj,typeof(GameObject), true);//с каким игровым объектом произвести действие
            if (newObj != null ? newObj != _action.gameObj : _action.gameObj != null)
            {
                _action.gameObj = newObj;
                CheckGameObjectActions(_action, i);

                if ((sceneName == story.sceneName) && (_action.gameObj != null))
                {
                    if (init != null)
                    {
                        init.eventObjects[story.storyActions.IndexOf(_action)] = _action.gameObj;
                    }
                }
            }

            if (GUILayout.Button("Delete"))
            {
                if (sceneName==story.sceneName)
                {
                    if (init != null)
                    {
                        init.eventObjects.RemoveAt(story.storyActions.IndexOf(_action));
                    }
                }
                story.storyActions.Remove(_action);
                actionNamesIndexes.RemoveAt(i);
                actionID1Indexes.RemoveAt(i);
                actionID2Indexes.RemoveAt(i);
            }

            EditorGUILayout.Space();

        }

        #endregion //storyActions

        EditorGUILayout.Space();

        #region presequences

        EditorGUILayout.HelpBox("presequences", MessageType.Info);
        EditorGUILayout.Space();

        presequencesSize = story.presequences.Count;
        presequencesSize = EditorGUILayout.IntField("presequences size", presequencesSize);
        if (presequencesSize != story.presequences.Count)
        {
            int m = story.presequences.Count;
            for (int i = m; i < presequencesSize; i++)
                story.presequences.Add(null);
            for (int i = m - 1; i >= presequencesSize; i--)
                story.storyActions.RemoveAt(i);
        }

        for (int i=0; i<story.presequences.Count;i++)
        {
            EditorGUILayout.BeginHorizontal();
            {
                story.presequences[i] = (Story)EditorGUILayout.ObjectField(story.presequences[i], typeof(Story));
                Story _presequence = story.presequences[i];
                if (_presequence != null)
                {
                    if (!_presequence.consequences.Contains(story))
                        _presequence.consequences.Add(story);
                    if (story.presequences.FindAll(x => (story.presequences.IndexOf(x) != i)).Contains(_presequence))
                    {
                        _presequence = null;
                        story.presequences[i] = null;
                    }
                }
                if (GUILayout.Button("Delete"))
                {
                    if (_presequence != null)
                        DeletePresequence(story, _presequence);
                    else
                        story.presequences.RemoveAt(i);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        #endregion //presequences

        #region consequences

        EditorGUILayout.HelpBox("consequences", MessageType.Info);
        EditorGUILayout.Space();

        consequencesSize = story.consequences.Count;
        consequencesSize = EditorGUILayout.IntField("consequences size", consequencesSize);
        if (consequencesSize != story.consequences.Count)
        {
            int m = story.consequences.Count;
            for (int i = m; i < consequencesSize; i++)
                story.consequences.Add(null);
            for (int i = m - 1; i >= consequencesSize; i--)
                story.consequences.RemoveAt(i);
        }

        for (int i = 0; i < story.consequences.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            {
                story.consequences[i] = (Story)EditorGUILayout.ObjectField(story.consequences[i], typeof(Story));
                Story _consequence = story.consequences[i];
                if (_consequence != null)
                {
                    if (story.nonConsequences.Contains(_consequence))
                    {
                        story.nonConsequences.Remove(_consequence);
                    }
                    if (!_consequence.presequences.Contains(story))
                        _consequence.presequences.Add(story);
                    if (story.consequences.FindAll(x => (story.consequences.IndexOf(x) != i)).Contains(_consequence))
                    {
                        story.consequences[i] = null;
                        _consequence = null;
                    }
                }
                if (GUILayout.Button("Delete"))
                {
                    if (_consequence != null)
                        DeleteConsequence(story, _consequence);
                    else
                        story.consequences.RemoveAt(i);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Space();

        #endregion //consequences

        #region nonsequences

        EditorGUILayout.HelpBox("nonsequences", MessageType.Info);
        EditorGUILayout.Space();

        nonsequencesSize = story.nonConsequences.Count;
        nonsequencesSize = EditorGUILayout.IntField("nonConsequences size", nonsequencesSize);
        if (nonsequencesSize != story.nonConsequences.Count)
        {
            int m = story.nonConsequences.Count;
            for (int i = m; i < nonsequencesSize; i++)
                story.nonConsequences.Add(null);
            for (int i = m - 1; i >= nonsequencesSize; i--)
                story.nonConsequences.RemoveAt(i);
        }

        for (int i = 0; i < story.nonConsequences.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            {
                story.nonConsequences[i] = (Story)EditorGUILayout.ObjectField(story.nonConsequences[i], typeof(Story));
                Story _nonsequence = story.nonConsequences[i];
                if (_nonsequence != null)
                {
                    if (story.consequences.Contains(_nonsequence))
                    {
                        DeleteConsequence(story, _nonsequence);
                    }
                    if (story.nonConsequences.FindAll(x => (story.nonConsequences.IndexOf(x) != i)).Contains(_nonsequence))
                    {
                        story.nonConsequences[i] = null;
                        _nonsequence = null;
                    }
                }
                if (GUILayout.Button("Delete"))
                {
                    story.nonConsequences.RemoveAt(i);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Space();

        #endregion //nonsequences

        #region storyCondition

        EditorGUILayout.HelpBox("story condition", MessageType.Info);
        EditorGUILayout.Space();

        StoryCondition _condition = story.storyCondition;


        if (_condition.obj != null)
        {
            int newIndex = EditorGUILayout.Popup(initNameIndex, initNames.ToArray());
            if (newIndex != initNameIndex)
            {
                initNameIndex = newIndex;
                _condition.storyConditionName = initNames[newIndex];

                _condition.conditionName = string.Empty;
                _condition.id = string.Empty;
                conditionIDIndex = -1;
                conditionNameIndex = -1;
            }
        }
        _condition.storyConditionName = EditorGUILayout.TextField("story condition name", _condition.storyConditionName);

        if (_condition.obj != null)
        {
            int newIndex = EditorGUILayout.Popup(conditionNameIndex, conditionNames.ToArray());
            if (newIndex != conditionNameIndex)
            {
                conditionNameIndex = newIndex;
                _condition.conditionName = conditionNames[newIndex];

                _condition.id = string.Empty;
                conditionIDIndex = -1;

                List<string> conditionTypes = SpecialFunctions.history.storyTypes;
                for (int i = 0; i < conditionTypes.Count; i++)
                {
                    Type conditionType = Type.GetType(conditionTypes[i]);
                    var component = _condition.obj.GetComponent(conditionType);
                    if (component != null && (component is IHaveStory))
                    {
                        IHaveStory storyObject = (IHaveStory)component;
                        if (storyObject.conditionIDs().ContainsKey(_condition.conditionName))
                        {
                            conditionIDs = storyObject.conditionIDs()[_condition.conditionName];
                            currentConditionObject = storyObject;
                            break;
                        }
                    }
                }

            }
        }
        _condition.conditionName= EditorGUILayout.TextField("condition name", _condition.conditionName);

        if (_condition.obj != null && currentConditionObject!=null)
        {
            int newIndex = EditorGUILayout.Popup(conditionIDIndex, conditionIDs.ToArray());
            if (newIndex != conditionIDIndex && conditionIDs.Count>0)
            {
                conditionIDIndex = newIndex;
                _condition.id = conditionIDs[newIndex];
            }
        }
        _condition.id = EditorGUILayout.TextField("id", _condition.id);

        _condition.argument=EditorGUILayout.IntField("argument",_condition.argument);

        GameObject newObj1 = (GameObject)EditorGUILayout.ObjectField("condition object",_condition.obj, typeof(GameObject),true);
        if (newObj1 != null ? newObj1 != _condition.obj : _condition.obj != null)
        {
            _condition.obj = newObj1;
            CheckGameObjectCondition(_condition);

            if ((sceneName == story.sceneName) && (_condition.obj != null))
            {
                if (init != null)
                {
                    init.eventReason = _condition.obj;
                }
            }
        }
        #endregion//storyCondition

        story.SetDirty();
        if (history != null)
            EditorUtility.SetDirty(SpecialFunctions.gameController.GetComponent<GameHistory>());

    }

    /// <summary>
    /// Удалить причину события
    /// </summary>
    public void DeletePresequence(Story _story, Story _presequence)
    {
        if (_presequence != null)
        {
            _presequence.consequences.Remove(story);
        }
        story.presequences.Remove(_presequence);
    }

    /// <summary>
    /// Удалить последствие
    /// </summary>
    public void DeleteConsequence(Story _story, Story _consequence)
    {
        if (_consequence != null)
        {
            _consequence.presequences.Remove(story);
        }
        story.consequences.Remove(_consequence);
    }

    /// <summary>
    /// Проверить, соответствует ли текущее сюжетное действие объекту, над которым это действие совершается
    /// </summary>
    public void CheckGameObjectActions(StoryAction sAction, int actionIndex)
    {
        if (actionNamesIndexes.Count < actionIndex)
            return;

        else if (actionNamesIndexes.Count==actionIndex)
            ChangeActionIndexesCount(1);

        GameObject actionObject = sAction.gameObj;
        if (actionObject == null)
        {
            actionNamesIndexes[actionIndex]=-1;
            actionID1Indexes[actionIndex]=-1;
            actionID2Indexes[actionIndex] = -1;
        }
        else
        {
            IHaveStory storyObject = actionObject.GetComponent<IHaveStory>();
            if (storyObject == null)
            {
                actionNamesIndexes[actionIndex] = -1;
                actionID1Indexes[actionIndex] = -1;
                actionID2Indexes[actionIndex] = -1;
            }
            else
            {
                if (storyObject.actionNames().Contains(sAction.actionName))
                {
                    actionNamesIndexes.Add(storyObject.actionNames().IndexOf(sAction.actionName));
                    List<string> id1List = storyObject.actionIDs1()[sAction.actionName], id2List = storyObject.actionIDs2()[sAction.actionName];
                    if (id1List.Contains(sAction.id1))
                        actionID1Indexes[actionIndex] = id1List.IndexOf(sAction.id1);
                    else if (id1List.Count > 0)
                    {
                        actionID1Indexes[actionIndex] = -1;
                        sAction.id1 = string.Empty;
                    }

                    if (id2List.Contains(sAction.id2))
                        actionID2Indexes[actionIndex] = id2List.IndexOf(sAction.id2);
                    else if (id2List.Count>0)
                    {
                        actionID2Indexes[actionIndex] = -1;
                        sAction.id2 = string.Empty;
                    }
                }
                else
                {
                    actionNamesIndexes[actionIndex] = -1;
                    actionID1Indexes[actionIndex] = -1;
                    actionID2Indexes[actionIndex] = -1;
                    sAction.actionName = string.Empty;
                    sAction.id1 = string.Empty;
                    sAction.id2 = string.Empty;
                }

            }
        }
    }

    /// <summary>
    /// Проверить, соответствуте ли текущее сюжетная причина объекту, который вызывает рассматриваемую историю
    /// </summary>
    public void CheckGameObjectCondition(StoryCondition sCondition)
    {

        conditionNames = new List<string>();
        initNames = new List<string>();
        conditionIDs = new List<string>();

        currentConditionObject = null;

        GameObject conditionObject = sCondition.obj;

        if (conditionObject == null)
        {
            initNameIndex = -1;
            conditionNameIndex = -1;
            conditionIDIndex = -1;
        }
        else
        {
            List<string> conditionTypes = SpecialFunctions.history.storyTypes;
            for (int i = 0; i < conditionTypes.Count; i++)
            {
                Type conditionType = Type.GetType(conditionTypes[i]);
                if (conditionObject.GetComponent(conditionType) != null)
                {
                    List<string> newInitList = SpecialFunctions.history.initNames[conditionType];
                    for (int j = 0; j < newInitList.Count; j++)
                        if (!initNames.Contains(newInitList[j]))
                            initNames.Add(newInitList[j]);

                    List<string> newConditionList = SpecialFunctions.history.compareNames[conditionType];
                    for (int j = 0; j < newConditionList.Count; j++)
                        if (!conditionNames.Contains(newConditionList[j]))
                            conditionNames.Add(newConditionList[j]);

                }
            }

            if (conditionNames.Count == 0)
            {
                conditionNameIndex = -1;
                initNameIndex = -1;
            }
            else
            {
                if (initNames.Contains(sCondition.storyConditionName))
                    initNameIndex = initNames.IndexOf(sCondition.storyConditionName); 
                else
                {
                    initNameIndex = -1;
                    sCondition.storyConditionName = string.Empty;
                }

                if (conditionNames.Contains(sCondition.conditionName))
                {
                    conditionNameIndex = conditionNames.IndexOf(sCondition.conditionName);
                    for (int i = 0; i < conditionTypes.Count; i++)
                    {
                        Type conditionType = Type.GetType(conditionTypes[i]);
                        var component = conditionObject.GetComponent(conditionType);
                        if (component != null && (component is IHaveStory))
                        {
                            IHaveStory storyObject = (IHaveStory)component;
                            if (storyObject.conditionIDs().ContainsKey(sCondition.conditionName))
                            {
                                conditionIDs = storyObject.conditionIDs()[sCondition.conditionName];
                                currentConditionObject = storyObject;
                                break;
                            }
                        }
                    }

                    if (conditionIDs.Count > 0)
                    {
                        if (conditionIDs.Contains(sCondition.id))
                            conditionIDIndex = conditionIDs.IndexOf(sCondition.id);
                        else
                        {
                            conditionIDIndex = -1;
                            sCondition.id = string.Empty;
                        }
                    }
                    else
                    {
                        conditionIDIndex = -1;
                    }
                }
                else
                {
                    conditionNameIndex = -1;
                    sCondition.conditionName = string.Empty;

                    conditionIDIndex = -1;
                    sCondition.id = string.Empty;
                }

            }
        }
    }

    /// <summary>
    /// Функция, меняющая размер списков индексов
    /// </summary>
    void ChangeActionIndexesCount(int delta)
    {
        if (delta > 0)
            for (int i = 0; i < delta; i++)
            {
                actionNamesIndexes.Add(-1);
                actionID1Indexes.Add(-1);
                actionID2Indexes.Add(-1);
            }
        if (delta < 0)
            for (int i = 0; (i < -1 * delta && actionNamesIndexes.Count>0); i++)
            {
                actionNamesIndexes.RemoveAt(actionNamesIndexes.Count-1);
                actionID1Indexes.RemoveAt(actionNamesIndexes.Count - 1);
                actionID2Indexes.RemoveAt(actionNamesIndexes.Count - 1);
            }
    }

}
#endif
