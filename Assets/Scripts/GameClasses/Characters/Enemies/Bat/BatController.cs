using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Скрипт, реализующий поведение летучей мыши
/// </summary>
public class BatController : AIController
{

    #region consts

    protected const float minSpeed = .1f;

    protected const float pushBackForce = 100f;

    #endregion //consts

    #region fields

    protected Hearing hearing;//Слух персонажа

    #endregion //fields


    protected virtual void FixedUpdate()
    {
        if (agressive && target!=null)
        {
            Vector3 targetPosition = target.transform.position;
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));

        }

        if (!agressive && rigid.velocity.magnitude < minSpeed)
        {
            Animate(new AnimationEventArgs("idle"));
        }
        else
        {
            Animate(new AnimationEventArgs("fly"));
        }

    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
        rigid.gravityScale = 0f;
        rigid.isKinematic = true;

        hitBox.AttackEventHandler += HandleAttackProcess;
        Transform indicators = transform.FindChild("Indicators");
        hearing = indicators.GetComponentInChildren<Hearing>();
        hearing.hearingEventHandler += HandleHearingEvent;

    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = (target.transform.position - transform.position).normalized * speed;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity,Time.fixedDeltaTime*acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        hitBox.SetHitBox(new HitClass(damage,-1f,attackSize,attackPosition,0f));
        rigid.isKinematic = false;
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        hitBox.ResetHitBox();
        rigid.isKinematic = true;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        BecomeAgressive();
    }

    #region events

    /// <summary>
    /// Обработка события "Услышал врага"
    /// </summary>
    protected virtual void HandleHearingEvent(object sender, EventArgs e)
    {
        BecomeAgressive();
    }

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected void HandleAttackProcess(object sender, EventArgs e)
    {
        rigid.AddForce((transform.position - target.transform.position).normalized * pushBackForce);//При столкновении с врагом летучая мышь отталкивается назад
    }

    #endregion //events

}
