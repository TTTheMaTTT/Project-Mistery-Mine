using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Контроллер, управляющий призраком
/// </summary>
public class GhostController : AIController
{
    #region consts

    protected const float attackDistance = .15f;//На каком расстоянии должен стоять паук, чтобы решить атаковать

    protected const float attackTime = .6f, preAttackTime = .4f;

    #endregion //consts

    #region fields

    protected SightFrustum sight;//Зрение персонажа

    #endregion //fields

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        Animate(new AnimationEventArgs("groundMove"));

    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        Transform indicators = transform.FindChild("Indicators");
        sight = indicators.GetComponentInChildren<SightFrustum>();
        sight.sightInEventHandler += HandleSightInEvent;
        sight.sightOutEventHandler += HandleSightOutEvent;

        base.Initialize();

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

    /// <summary>
    /// Успокоится
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        sight.Rotate(Vector2.right * (int)orientation);
    }

    /// <summary>
    /// Выдвинуться к целевой позиции
    /// </summary>
    /// <param name="targetPosition">Целевая позиция</param>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        BecomePatrolling();
        if (currentTarget == null)
        {
            currentTarget = new GameObject("GhostTarget");
            currentTarget.transform.position = targetPosition;
        }
    }

    #region eventHandlers

    /// <summary>
    /// Обработка события "Увидел врага"
    /// </summary>
    protected virtual void HandleSightInEvent(object sender, EventArgs e)
    {
        if (behaviour != BehaviourEnum.agressive)
            BecomeAgressive();
    }

    /// <summary>
    /// Обработка события "Упустил врага из виду"
    /// </summary>
    protected virtual void HandleSightOutEvent(object sender, EventArgs e)
    {
        if (behaviour == BehaviourEnum.agressive)
            GoToThePoint(mainTarget.transform.position);//Выдвинуться туда, где в последний раз видел врага
    }

    #endregion //eventHandlers

    #region behaviourActions

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehaviour()
    {
        base.AgressiveBehaviour();
        if (mainTarget != null && employment > 7)
        {
            Vector2 targetPosition = mainTarget.transform.position;
            if ((Mathf.Abs(targetPosition.x - transform.position.x) > attackDistance) ||
                (Mathf.Abs(targetPosition.y - transform.position.y) > (attackDistance / 2f)))
            {
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
            }
            else
            {
                Attack();
            }
        }
    }

    /// <summary>
    /// Поведение преследования какой-либо цели
    /// </summary>
    protected override void PatrolBehaviour()
    {
        base.PatrolBehaviour();

        Vector3 targetPosition = currentTarget.transform.position;
        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
        sight.Rotate(((Vector2)rigid.velocity).normalized);//В режиме патрулирования призрак смотрит в ту сторону, в которую движется
        if (Vector3.SqrMagnitude(currentTarget.transform.position - transform.position) < minDistance * minDistance)
        {
            if (Vector2.SqrMagnitude((Vector2)targetPosition - beginPosition) > minDistance * minDistance)
            {
                DestroyImmediate(currentTarget);
                GoHome();
            }
            else
            {
                BecomeCalm();
            }

        }
    }

    #endregion //behaviourActions

}
