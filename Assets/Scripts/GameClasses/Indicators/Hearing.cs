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

    #region fields

    protected Transform hero;

    #endregion //fields

    #region parametres

    public float radius = 1f;
    public float Radius { set { radius = value;/*GetComponent<CircleCollider2D>().radius = value;*/ } }

    protected bool activated=false;

    #endregion //parametres

    void Awake()
    {
        hero = SpecialFunctions.Player.transform;
    }

    /*void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "player")
            OnHearingEvent(new EventArgs());
    }*/

    void FixedUpdate()
    {
        if (!activated)
        {
            if (Vector2.SqrMagnitude(hero.position - transform.position) < radius * radius)
            {
                activated = true;
                OnHearingEvent(new EventArgs());
            }
        }
        else
        {
            if (Vector2.SqrMagnitude(hero.position - transform.position) > radius * radius)
            {
                activated = false;
            }
        }
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

    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.activeObject == gameObject)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif //UNITY_EDITOR
    }

}
