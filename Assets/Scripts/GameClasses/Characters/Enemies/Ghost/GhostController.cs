using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Контроллер, управляющий призраком
/// </summary>
public class GhostController : AIController
{

    #region fields

    //protected SightFrustum sight;//Зрение персонажа

    #endregion //fields

    #region parametres

    protected override float attackTime { get { return .6f; } }
    protected override float preAttackTime { get { return .4f; } }
    protected override float attackDistance { get { return .15f; } }//На каком расстоянии должен стоять паук, чтобы решить атаковать

    #endregion //parametres

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        Animate(new AnimationEventArgs("groundMove"));

    }

    protected virtual void Update()
    {
        Analyse();
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        Transform indicators = transform.FindChild("Indicators");
        //sight = indicators.GetComponentInChildren<SightFrustum>();
        //sight.sightInEventHandler += HandleSightInEvent;
        //sight.sightOutEventHandler += HandleSightOutEvent;

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

    protected override void Analyse()
    {
        base.Analyse();

        switch (behaviour)
        {
            case BehaviourEnum.agressive:
                {
                    Vector2 direction = mainTarget.transform.position - transform.position;
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, direction.magnitude, LayerMask.GetMask(gLName));
                    if (hit)
                        GoToThePoint(mainTarget.transform.position);

                    break;
                }
            case BehaviourEnum.patrol:
                {
                    Vector2 direction = rigid.velocity.normalized;
                    RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + sightOffset * direction, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (hit.collider.gameObject.CompareTag("player"))
                        {
                            BecomeAgressive();
                        }
                    }

                    break;
                }

            case BehaviourEnum.calm:
                {
                    Vector2 direction = Vector2.right * (int)orientation;
                    RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + sightOffset * direction, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (hit.collider.gameObject.CompareTag("player"))
                        {
                            BecomeAgressive();
                        }
                    }
                    break;
                }

            default:
                {
                    break;
                }
        }

        prevPosition = new EVector3(transform.position, true);
    }

    /// <summary>
    /// Успокоится
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        //sight.Rotate(Vector2.right * (int)orientation);
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
        //sight.Rotate(((Vector2)rigid.velocity).normalized);//В режиме патрулирования призрак смотрит в ту сторону, в которую движется
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
