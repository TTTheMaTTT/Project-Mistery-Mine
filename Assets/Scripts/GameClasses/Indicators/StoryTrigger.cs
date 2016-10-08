using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Скрипт, управляющий коллайдером, чувствительным к столкновению с ГГ и вызывающий продвижение сюжета
/// </summary>
public class StoryTrigger : MonoBehaviour
{

    #region events

    public EventHandler<StoryEventArgs> TriggerEvent;

    #endregion //events

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "player")
        {
            SpecialFunctions.StartStoryEvent(this, TriggerEvent, new StoryEventArgs());
        }
    }

}
