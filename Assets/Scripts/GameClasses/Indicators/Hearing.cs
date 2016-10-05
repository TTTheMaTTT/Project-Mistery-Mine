using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Индикатор, представляющий слух персонажа
/// </summary>
public class Hearing : MonoBehaviour
{

    #region eventHandlers

    public EventHandler<EventArgs> hearingEventHandler;

    #endregion //eventHandlers

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "player")
            OnHearingEvent(new EventArgs());
    }

    #region events

    /// <summary>
    /// Событие, которое наступит, когда персонаж услышит героя
    /// </summary>
    protected void OnHearingEvent(EventArgs e)
    {
        EventHandler<EventArgs> handler = hearingEventHandler;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

}
