using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Индикатор, представляющий слух персонажа
/// </summary>
public class Hearing : MonoBehaviour
{

    #region eventHandlers

    public EventHandler<HearingEventArgs> hearingEventHandler;//Обработчик события "Был услышан враг"

    #endregion //eventHandlers

    #region fields

    protected List<CharacterController> allies;//Список союзных игроку персонажей, что находятся на поле боя в данный момент
    protected Collider2D col;
    protected Collider2D Col { get { if (!col) col = GetComponent<Collider2D>(); return col; } }

    #endregion //fields

    #region parametres

    public float radius = 1f;
    public float Radius { set { radius = value; if (col != null) ((CircleCollider2D)col).radius = value;} }

    protected bool allyHearing = false;//Находится ли слух в режиме слуха союзника (использует коллайдер) или слуха врага (оценивает расстояние)
    public bool AllyHearing { set { allyHearing = value; Col.enabled = value; gameObject.layer = LayerMask.NameToLayer(value ? "heroHitBox": "hitBox"); } }

    #endregion //parametres

    void Start()
    {
        allies = SpecialFunctions.battleField.Allies;
        col = GetComponent<Collider2D>();
        if (col is CircleCollider2D)
        {
            ((CircleCollider2D)col).radius = radius;
        }
        col.enabled = allyHearing;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (allyHearing)
        {
            AIController ai = other.GetComponent<AIController>();
            if (ai!= null? ai.Loyalty==LoyaltyEnum.enemy:false)
                OnHearingEvent(new HearingEventArgs(other.gameObject));
        }
    }

    void FixedUpdate()
    {
        if (!allyHearing)
        {
            Vector3 pos = transform.position;
            foreach (CharacterController ally in allies)
            {
                if (ally == null)
                    continue;
                if (Vector2.SqrMagnitude(ally.transform.position - pos) < radius * radius)
                {
                    OnHearingEvent(new HearingEventArgs(ally.gameObject));
                    break;
                }
            }
        }
    }

    #region events

    /// <summary>
    /// Событие, которое наступит, когда персонаж услышит героя
    /// </summary>
    protected void OnHearingEvent(HearingEventArgs e)
    {
        EventHandler<HearingEventArgs> handler = hearingEventHandler;
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
