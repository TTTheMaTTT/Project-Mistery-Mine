using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, управляющий коллайдером, чувствительным к столкновению с ГГ и вызывающий продвижение сюжета
/// </summary>
public class StoryTrigger : MonoBehaviour, IHaveStory
{

    #region events

    public EventHandler<StoryEventArgs> TriggerEvent;

    protected bool triggered=false;

    #endregion //events

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "player")
        {
            SpecialFunctions.StartStoryEvent(this, TriggerEvent, new StoryEventArgs());
            if (!triggered)
            {
                triggered = true;
                SpecialFunctions.statistics.ConsiderStatistics(this);
            }
        }
    }

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить скрипт
    /// </summary>
    /// <returns></returns>
    public List<string> actionNames()
    {
        return new List<string>() { };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() { };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() { };
    }

    /// <summary>
    /// Вернуть словарь id-шников, связанных с конкретной функцией проверки условия сюжетного события
    /// </summary>
    public Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>() { { "", new List<string>() },
                                                        { "compareHistoryProgress",SpecialFunctions.statistics.HistoryBase.stories.ConvertAll(x=>x.storyName)} };
    }

    /// <summary>
    /// Возвращает ссылку на сюжетное действие, соответствующее данному имени
    /// </summary>
    public StoryAction.StoryActionDelegate GetStoryAction(string s)
    {
        return null;
    }

    /// <summary>
    /// Функция-пустышка
    /// </summary>
    public void NullFunction(StoryAction _action)
    { }

    #endregion //IHaveStory


}
