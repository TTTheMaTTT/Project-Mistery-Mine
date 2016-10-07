using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Контроллер, управляющий призраком
/// </summary>
public class GhostController : BatController
{
    #region consts

    protected const float attackDistance = .15f;//На каком расстоянии должен стоять паук, чтобы решить атаковать

    protected const float attackTime = .6f, preAttackTime = .4f;

    #endregion //consts

    protected override void FixedUpdate()
    {
        if (agressive && target != null && employment > 7)
        {
            Vector2 targetPosition = target.transform.position;
            if ((Mathf.Abs(targetPosition.x-transform.position.x) > attackDistance) || 
                (Mathf.Abs(targetPosition.y-transform.position.y)> (attackDistance/2f)))
            {
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
            }
            else
            {
                Attack();
            }
        }

        Animate(new AnimationEventArgs("groundMove"));

    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {

        rigid = GetComponent<Rigidbody2D>();

        hitBox = GetComponentInChildren<HitBox>();
        hitBox.SetEnemies(enemies);

        orientation = (OrientationEnum)Mathf.RoundToInt(Mathf.Sign(transform.localScale.x));

        anim = GetComponentInChildren<CharacterVisual>();
        if (anim != null)
        {
            AnimationEventHandler += anim.AnimateIt;
        }

        employment = maxEmployment;

        agressive = false;

        Transform indicators = transform.FindChild("Indicators");
        hearing = indicators.GetComponentInChildren<Hearing>();
        hearing.hearingEventHandler += HandleHearingEvent;

        rigid.gravityScale = 0f;

    }

    protected override void StopMoving()
    {
        rigid.velocity = Vector2.zero;
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        Animate(new AnimationEventArgs("attack"));
        StopMoving();
        StartCoroutine(AttackProcess());
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(preAttackTime);
        hitBox.SetHitBox(new HitClass(damage, attackTime, attackSize, attackPosition, hitForce));
        yield return new WaitForSeconds(attackTime);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    #region eventHandlers

    protected override void BecomeAgressive()
    {
        agressive = true;
        target = SpecialFunctions.player;
    }

    #endregion //eventHandlers


}
