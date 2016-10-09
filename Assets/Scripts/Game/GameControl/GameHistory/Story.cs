using UnityEngine;
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
    Story story;

    public override void OnInspectorGUI()
    {
        if (story == null)
            story = (Story)target;

        story.storyName = EditorGUILayout.TextField("story name", story.storyName);

        if (story.storyActions == null)
        {
            story.storyActions = new List<StoryAction>();
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

        EditorGUILayout.Space();

        #region storyActions

        EditorGUILayout.HelpBox("story actions",MessageType.Info);

        actionSize = story.storyActions.Count;
        actionSize = EditorGUILayout.IntField("action size", actionSize);
        if (actionSize !=story.storyActions.Count)
        {
            int m = story.storyActions.Count;
            for (int i = m; i < actionSize; i++)
                story.storyActions.Add(new StoryAction());
            for (int i = m - 1; i >= actionSize; i--)
                story.storyActions.RemoveAt(i);
        }

        foreach (StoryAction _action in story.storyActions)
        {

            _action.storyActionName = EditorGUILayout.TextField("story action name", _action.storyActionName);
            _action.actionName= EditorGUILayout.TextField("action name", _action.actionName);
            _action.id1 = EditorGUILayout.TextField("id1", _action.id1);
            _action.id2 = EditorGUILayout.TextField("id2", _action.id2);
            _action.argument=EditorGUILayout.IntField("argument",_action.argument);

            _action.gameObj=(GameObject)EditorGUILayout.ObjectField("action object",_action.gameObj,typeof(GameObject), true);//с каким игровым объектом произвести действие

            if (GUILayout.Button("Delete"))
            {
                story.storyActions.Remove(_action);
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
        if (_condition == null)
            _condition = story.storyCondition = new StoryCondition();

        _condition.storyConditionName=EditorGUILayout.TextField("story condition name", _condition.storyConditionName);

        _condition.conditionName= EditorGUILayout.TextField("condition name", _condition.conditionName);
        _condition.id = EditorGUILayout.TextField("id", _condition.id);
        _condition.argument=EditorGUILayout.IntField("argument",_condition.argument);
        _condition.obj=(GameObject)EditorGUILayout.ObjectField("condition object",_condition.obj, typeof(GameObject),true);

        #endregion//storyCondition

        story.SetDirty();

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

}
#endif
