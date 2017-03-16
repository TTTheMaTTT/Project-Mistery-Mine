using UnityEngine;
using System;
using System.Collections;

/// <summary>
///  Индикатор, ответственный за зрение персонажа. Это его поле зрения. Считаем, что все монстры пытаются отследить ГГ
/// </summary>
public class SightFrustum : MonoBehaviour
{

    #region consts

    private const float angleEps = 3f;//Точность ориентации поля зрения
    private const float minDistance = .45f;//Дистанция, в пределах которой невозможно потерять цель из виду

    #endregion //consts

    #region eventHandlers

    public EventHandler<EventArgs> sightInEventHandler;//Вызывается, когда что-то важное попало в поле зрение
    public EventHandler<EventArgs> sightOutEventHandler;//Вызывается, когда что-то было упущено из виду

    #endregion //eventHandlers

    #region fields

    [SerializeField]private LayerMask whatToSight;//Какие объекты нужно замечать?
    public LayerMask WhatToSight { get { return whatToSight; } set { whatToSight = value; } }

    private GameObject target;//За кем следим?
    private Transform sight;//Родительский объект по отношению к данному. Именно его мы и будем вращать
    private Transform Sight { get { if (sight == null) { sight = transform.parent; } return sight; } }

    #endregion //fields

    #region parametres

    private float sightRadius;

    #endregion //parametres

    void Start()
    {
        sight = transform.parent;
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            Vector2 targetDirection = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            Rotate(targetDirection);
            if (Vector2.SqrMagnitude((Vector2)sight.transform.position - (Vector2)target.transform.position) > minDistance * minDistance)
            {
                sightRadius = ((Vector2)(transform.position - target.transform.position)).magnitude;
                if (Physics2D.Raycast(transform.position, targetDirection, sightRadius, whatToSight))
                    OnSightOutEvent(new EventArgs());
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (target == null)
        {
            if (other.gameObject.CompareTag("player"))
            {
                RaycastHit2D hit;
                sightRadius = 5f;
                if (hit = Physics2D.Raycast(transform.position, ((Vector2)other.transform.position - (Vector2)transform.position).normalized, sightRadius, whatToSight))
                {
                    if (hit.collider != null ? hit.collider == other : true)
                    {
                        OnSightInEvent(new EventArgs());
                    }
                }
            }
        }

    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (target != null && other.gameObject==target)
        {

            if (RotationIsCorrect() && (Vector2.SqrMagnitude((Vector2)sight.transform.position - (Vector2)target.transform.position) > minDistance * minDistance))
            {
                OnSightOutEvent(new EventArgs());
            }
        }
    }

    /// <summary>
    /// Сориентировать поле зрение на заданный угол
    /// </summary>
    public void Rotate(Vector2 targetDirection)
    {
        float angle = GetAngle(targetDirection);
        Sight.eulerAngles = Vector3.forward*angle; 
    }

    /// <summary>
    /// Сориентировать зрение относительно владельца зрения
    /// </summary>
    /// <param name="_angle">угол поворота</param>
    public void RotateLocal(float _angle)
    {
        Sight.localEulerAngles = Vector3.forward * _angle;
    }

    /// <summary>
    /// Проверка на то, что поле зрения повёрнуто по отношению к цели верно. 
    /// </summary>
    /// <returns></returns>
    bool RotationIsCorrect()
    {
        if (target == null)
            return false;
        float angle = Mathf.Repeat(GetAngle(((Vector2)target.transform.position - (Vector2)transform.position).normalized),360f);
        return (Mathf.Abs(angle - Sight.eulerAngles.z) < angleEps);
    }

    /// <summary>
    /// Угол, на который должно быть повёрнуто поле зрения, чтобы следить за целью
    /// </summary>
    /// <param name="targetDirection">Направление, в котором нужно смотреть</param>
    /// <returns>угол нужного поворота</returns>
    float GetAngle(Vector2 targetDirection)
    {
        Vector2 beginDirection = Vector2.right * Mathf.Sign(transform.lossyScale.x);
        return Mathf.Sign(targetDirection.y*transform.lossyScale.x)*Vector2.Angle(beginDirection, targetDirection);
    }

    /// <summary>
    /// Изменить режим обозревания
    /// </summary>
    public void ChangeSightMod()
    {
        if (target != null)
            OnSightOutEvent(new EventArgs());
        else
            OnSightInEvent(new EventArgs());
    }

    public void SetSightMod(bool activate)
    {
        if (activate && target==null)
            OnSightInEvent(new EventArgs());
        else if (target!=null && !activate)
            OnSightOutEvent(new EventArgs());
    }

    /// <summary>
    /// Определить режим обозревания местности
    /// </summary>
    /// <returns>Следит ли система зрения за целью?</returns>
    public bool ObserveTarget()
    {
        return target != null;
    }

    #region events

    /// <summary>
    /// Событие, которое наступит, когда персонаж заметит героя
    /// </summary>
    protected void OnSightInEvent(EventArgs e)
    {
        target = SpecialFunctions.Player;
        EventHandler<EventArgs> handler = sightInEventHandler;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    /// <summary>
    /// Событие, которое наступит, когда персонаж упустит из виду героя
    /// </summary>
    protected void OnSightOutEvent(EventArgs e)
    {
        EventHandler<EventArgs> handler = sightOutEventHandler;
        target = null;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

}
