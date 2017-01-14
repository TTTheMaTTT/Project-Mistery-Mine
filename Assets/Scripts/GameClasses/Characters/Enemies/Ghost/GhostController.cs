using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Контроллер, управляющий призраком
/// </summary>
public class GhostController : AIController
{

    #region fields

    protected HitBox selfHitBox;//Хитбокс, который атакует персонажа при соприкосновении с пауком. Этот хитбокс всегда активен и не перемещается

    //protected SightFrustum sight;//Зрение персонажа

    protected Hearing hearing;//Слух персонажа
    protected Collider2D col;

    #endregion //fields

    #region parametres

    protected override float attackTime { get { return .6f; } }
    protected override float preAttackTime { get { return .4f; } }
    protected override float attackDistance { get { return .15f; } }//На каком расстоянии должен стоять паук, чтобы решить атаковать
    //public override bool Waiting {get {return waiting;} set{waiting = value; if (col != null) col.isTrigger = !value; }}

    #endregion //parametres

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        Animate(new AnimationEventArgs("groundMove"));

    }

    protected override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.B))
        {
            GoToThePoint(SpecialFunctions.Player.transform.position);
        }
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        if (indicators != null)
        {
            hearing = indicators.GetComponentInChildren<Hearing>();
            if (hearing != null)
                hearing.hearingEventHandler += HandleHearingEvent;
        }
        //sight = indicators.GetComponentInChildren<SightFrustum>();
        //sight.sightInEventHandler += HandleSightInEvent;
        //sight.sightOutEventHandler += HandleSightOutEvent;

        col = GetComponent<Collider2D>();

        base.Initialize();

        selfHitBox = transform.FindChild("SelfHitBox").GetComponent<HitBox>();
        if (selfHitBox != null)
        {
            selfHitBox.SetEnemies(enemies);
            selfHitBox.SetHitBox(damage, -1f, 0f);
            //selfHitBox.Immobile = true;//На всякий случай
            selfHitBox.AttackEventHandler += HandleAttackProcess;
        }

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            if (selfHitBox != null)
            {
                areaTrigger.triggerFunctionIn += EnableSelfHitBox;
                areaTrigger.triggerFunctionOut += DisableSelfHitBox;
            }
            areaTrigger.InitializeAreaTrigger();
        }

        rigid.gravityScale = 0f;

        BecomeCalm();
    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = (currentTarget - (Vector2)transform.position).normalized * speed;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Двинуться прочь от цели
    /// </summary>
    /// <param name="_orientation">Ориентация персонажа при перемещении</param>
    protected virtual void MoveAway(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = ((Vector2)transform.position - currentTarget).normalized * speed;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Прекратить перемещение
    /// </summary>
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
    /// Анализ окружающей обстановки
    /// </summary>
    protected override void Analyse()
    {
        base.Analyse();

        Vector2 pos = transform.position;
        switch (behavior)
        {
            case BehaviorEnum.agressive:
                {
                    Vector2 direction = mainTarget - pos;
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, direction.magnitude, LayerMask.GetMask(gLName));
                    if (hit)
                    {
                        if (hit.distance > direction.magnitude / 2f)
                        {
                            GoToThePoint(mainTarget);
                            StartCoroutine("BecomeCalmProcess");
                        }
                    }
                    break;
                }
            case BehaviorEnum.patrol:
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

            case BehaviorEnum.calm:
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
    /// Выдвинуться к целевой позиции
    /// </summary>
    /// <param name="targetPosition">Целевая позиция</param>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        BecomePatrolling();
        currentTarget = new ETarget(targetPosition);
    }

    #region eventHandlers

    /// <summary>
    /// Обработка события "Увидел врага"
    /// </summary>
    protected virtual void HandleSightInEvent(object sender, EventArgs e)
    {
        if (behavior != BehaviorEnum.agressive)
            BecomeAgressive();
    }

    /// <summary>
    /// Обработка события "Упустил врага из виду"
    /// </summary>
    protected virtual void HandleSightOutEvent(object sender, EventArgs e)
    {
        if (behavior == BehaviorEnum.agressive)
            GoToThePoint(mainTarget);//Выдвинуться туда, где в последний раз видел врага
    }

    #endregion //eventHandlers

    #region behaviourActions

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        if (mainTarget.exists && employment > 7)
        {
            Vector2 targetPosition = mainTarget;
            Vector2 pos = transform.position;
            Vector2 direction = targetPosition - pos;
            float sqDistance = direction.sqrMagnitude;
            if (waiting)
            {
                if (sqDistance < waitingNearDistance * waitingNearDistance)
                    MoveAway((OrientationEnum)Mathf.RoundToInt(-Mathf.Sign(direction.x)));
                else if (sqDistance < waitingFarDistance * waitingFarDistance)
                    StopMoving();
                else
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(direction.x)));
            }
            else
            {
                if ((Mathf.Abs(direction.x) > attackDistance) ||
                    (Mathf.Abs(direction.y) > (attackDistance / 2f)))
                {
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(direction.x)));
                }
                else
                {
                    StopMoving();
                    Attack();
                }
            }
        }
    }

    /// <summary>
    /// Поведение преследования какой-либо цели
    /// </summary>
    protected override void PatrolBehavior()
    {
        base.PatrolBehavior();

        Vector2 targetPosition = currentTarget;
        Vector2 pos = transform.position;
        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
        //sight.Rotate(((Vector2)rigid.velocity).normalized);//В режиме патрулирования призрак смотрит в ту сторону, в которую движется
        if (Vector3.SqrMagnitude(currentTarget - pos) < minDistance * minDistance)
        {
            StopMoving();
            if (Vector2.SqrMagnitude(targetPosition - beginPosition) > minDistance * minDistance)
            {
                GoHome();
            }
            else
            {
                BecomeCalm();
            }

        }
    }

    #endregion //behaviourActions

    #region optimization

    /// <summary>
    /// Включить собственный хитбокс
    /// </summary>
    protected override void EnableSelfHitBox()
    {
        selfHitBox.gameObject.SetActive(true);
    }

    /// <summary>
    /// Выключить собственный хитбокс
    /// </summary>
    protected override void DisableSelfHitBox()
    {
        selfHitBox.gameObject.SetActive(false);
    }

    /// <summary>
    /// Функция реализующая анализ окружающей персонажа обстановки, когда тот находится в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
        if (behavior!=BehaviorEnum.calm)
            if (!followOptPath)
                StartCoroutine("PathPassOptProcess");
    }

    /// <summary>
    /// Процесс оптимизированного прохождения пути. Заключается в том, что персонаж, зная свой маршрут, появляется в его различиных позициях, не используя 
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator PathPassOptProcess()
    {
        followOptPath = true;
        if (!currentTarget.exists)
        {
            if (Vector2.SqrMagnitude((Vector2)transform.position - beginPosition) < minCellSqrMagnitude)
                BecomeCalm();
            else
            {
                GoHome();
                if (!currentTarget.exists)
                {
                    //Если не получается добраться до начальной позиции, то считаем, что текущая позиция становится начальной
                    beginPosition = transform.position;
                    beginOrientation = orientation;
                    BecomeCalm();
                    followOptPath = false;
                }
                else
                    StartCoroutine("PathPassOptProcess");
            }
        }
        else
        {
            while (currentTarget.exists)
            {
                Vector2 pos = transform.position;
                Vector2 targetPos = currentTarget;

                if (Vector2.SqrMagnitude(pos - targetPos) <= minCellSqrMagnitude)
                {
                    transform.position = targetPos;
                    pos = transform.position;
                    currentTarget.Exists=false;
                    break;
                }
                targetPos = currentTarget;
                yield return new WaitForSeconds(optTimeStep);
                Vector2 direction = targetPos - pos;
                transform.position = pos + direction.normalized * Mathf.Clamp(speed, 0f, direction.magnitude);
            }
            followOptPath = false;
        }
    }

    /// <summary>
    /// Функция, которая восстанавливает положение и состояние персонажа, пользуясь данными, полученными в оптимизированном режиме
    /// </summary>
    protected override void RestoreActivePosition()
    {
        if (currentTarget.exists)
            Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign((currentTarget - (Vector2)transform.position).x)));
    }

    #endregion //optimization

}
