using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, представляющий собой некий сюжет - что-то произошло и вызвало череду событий. Вообще Story - базовый элемент для построений цепочки игровых событий
/// </summary>
[System.Serializable]
public class Story : ScriptableObject
{
    #region fields

    public string storyName;
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
    public GameObject gameObj;//с каким префабом произвести действие
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
    //public GameObject obj;//с каким префабом произвести действие
    public StoryConditionDelegate storyCondition;//ссылка на функцию, которая соответствует названию выше

}