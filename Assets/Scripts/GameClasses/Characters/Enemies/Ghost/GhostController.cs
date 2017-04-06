using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Контроллер, управляющий призраком
/// </summary>
public class GhostController : AIController
{

    #region fields

    protected HitBoxController selfHitBox;//Хитбокс, который атакует персонажа при соприкосновении с пауком. Этот хитбокс всегда активен и не перемещается
    [SerializeField]protected GameObject missile;//Снаряды стрелка

    protected Hearing hearing;//Слух персонажа
    protected Collider2D col;
    protected WallChecker wallCheck;

    #endregion //fields

    #region parametres

    protected override float attackDistance { get { return .15f; } }//На каком расстоянии должен стоять паук, чтобы решить атаковать
    //public override bool Waiting {get {return waiting;} set{waiting = value; if (col != null) col.isTrigger = !value; }}

    public override LoyaltyEnum Loyalty
    {
        get
        {
            return base.Loyalty;
        }

        set
        {
            base.Loyalty = value;
            if (selfHitBox != null)
            {
                selfHitBox.allyHitBox = (value == LoyaltyEnum.ally);
                selfHitBox.SetEnemies(enemies);
            }
            if (hearing != null)
                hearing.AllyHearing = (value == LoyaltyEnum.ally);
            gameObject.layer = LayerMask.NameToLayer(value == LoyaltyEnum.ally ? "hero" : "characterWithoutPlatform");
        }
    }

    public override bool Waiting { get { return base.Waiting; } set { base.Waiting = value; StopCoroutine("AttackProcess"); Animate(new AnimationEventArgs("stop")); } }
    

    protected virtual Vector2 shootPosition { get { return new Vector2(0.0353f, 0.0153f); } }//Откуда стреляет персонаж
    protected virtual float attackRate { get { return 3f; } }//Сколько секунд проходит между атаками

    [SerializeField]
    protected float missileSpeed = 3f;//Скорость снаряда после выстрела

    #endregion //parametres

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        Animate(new AnimationEventArgs("fly"));

    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        //sight = indicators.GetComponentInChildren<SightFrustum>();
        //sight.sightInEventHandler += HandleSightInEvent;
        //sight.sightOutEventHandler += HandleSightOutEvent;

        col = GetComponent<Collider2D>();

        base.Initialize();

        if (indicators != null)
        {
            hearing = indicators.GetComponentInChildren<Hearing>();
            if (hearing != null)
                hearing.hearingEventHandler += HandleHearingEvent;
        }

        wallCheck = transform.GetComponentInChildren<WallChecker>();
        wallCheck.enabled = false;

        selfHitBox = transform.FindChild("SelfHitBox").GetComponent<HitBoxController>();
        if (selfHitBox != null)
        {
            selfHitBox.SetEnemies(enemies);
            selfHitBox.SetHitBox(attackParametres.damage, -1f, 0f,DamageType.Cold,15f,attackParametres.attackPower);
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
        Vector2 targetVelocity = (currentTarget - (Vector2)transform.position).normalized * speed * speedCoof;
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
    protected override void MoveAway(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = ((Vector2)transform.position - currentTarget).normalized * speed * speedCoof;
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
        Animate(new AnimationEventArgs("attack", "", Mathf.RoundToInt(10 * (attackParametres.preAttackTime ))));
        StopMoving();
        StartCoroutine("AttackProcess");
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.preAttackTime);

        Vector2 pos = transform.position;
        Vector2 _shootPosition = pos + new Vector2(shootPosition.x * (int)orientation, shootPosition.y);
        Vector2 direction = (currentTarget - pos).x * (int)orientation >= 0f ? (currentTarget - _shootPosition).normalized : (int)orientation * Vector2.right;
        GameObject newMissile = Instantiate(missile, _shootPosition, Quaternion.identity) as GameObject;
        Rigidbody2D missileRigid = newMissile.GetComponent<Rigidbody2D>();
        missileRigid.velocity = direction * missileSpeed;
        HitBoxController missileHitBox = missileRigid.GetComponentInChildren<HitBoxController>();
        if (missileHitBox != null)
        {
            missileHitBox.SetEnemies(enemies);
            missileHitBox.SetHitBox(new HitParametres(attackParametres));
            missileHitBox.allyHitBox = loyalty == LoyaltyEnum.ally;
            missileHitBox.AttackerInfo = new AttackerClass(gameObject,AttackTypeEnum.range);
        }
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);

        yield return new WaitForSeconds(attackRate);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    public override void TakeDamage(HitParametres hitData)
    {
        base.TakeDamage(hitData);
        if (hitData.attackPower>balance)
            Animate(new AnimationEventArgs("stop"));
    }

    public override void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
        base.TakeDamage(hitData, ignoreInvul);
        if (hitData.attackPower>balance)
            Animate(new AnimationEventArgs("stop"));
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
                        if (enemies.Contains(hit.collider.gameObject.tag))
                        {
                            MainTarget = new ETarget(hit.collider.transform);
                            BecomeAgressive();
                        }
                    }

                    if (loyalty == LoyaltyEnum.ally && !mainTarget.exists) //Если нет основной цели и призрак - союзник героя, то он следует к нему
                    {
                        float sqDistance = Vector2.SqrMagnitude(beginPosition - pos);
                        if (sqDistance > allyDistance * 1.2f && followAlly)
                        {
                            if (Vector2.SqrMagnitude(beginPosition - (Vector2)prevTargetPosition) > minCellSqrMagnitude)
                            {
                                prevTargetPosition = new EVector3(pos);//Динамическое преследование героя-союзника
                                GoHome();
                                StartCoroutine("ConsiderAllyPathProcess");
                            }
                        }
                        else if (sqDistance < allyDistance)
                        {
                            StopMoving();
                            BecomeCalm();
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
                        if (enemies.Contains(hit.collider.gameObject.tag))
                        {
                            MainTarget = new ETarget(hit.collider.transform);
                            BecomeAgressive();
                        }

                        if (loyalty == LoyaltyEnum.ally)
                        {
                            if (Vector2.SqrMagnitude(beginPosition - pos) > allyDistance * 1.2f)
                            {
                                if (Vector2.SqrMagnitude(beginPosition - (Vector2)prevTargetPosition) > minCellSqrMagnitude)
                                {
                                    prevTargetPosition = new EVector3(pos);
                                    GoHome();
                                }
                            }
                            if ((int)orientation * (beginPosition - pos).x < 0f)
                                Turn();//Всегда быть повёрнутым к герою-союзнику
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
    /// Подготовить данные для ведения деятельности в следующей модели поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        col.isTrigger = true;
        StopCoroutine("AttackProcess");
        Animate(new AnimationEventArgs("stop"));
    }

    /// <summary>
    /// Стать агрессивным
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        if (hearing)
        {
            hearing.enabled = false;
            
        }
    }

    /// <summary>
    /// Стать спокойным
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        if (hearing)
            hearing.enabled = true;
    }

    /// <summary>
    /// Стать патрулирующим
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        if (hearing)
            hearing.enabled = true;
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

    #region behaviourActions

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        if (mainTarget.exists && employment > 2)
        {
            Vector2 targetPosition = mainTarget;
            Vector2 pos = transform.position;
            Vector2 direction = targetPosition - pos;
            float sqDistance = direction.sqrMagnitude;

            bool insideWall = wallCheck.CheckWall();
            if (sqDistance < waitingNearDistance * waitingNearDistance && !insideWall)
            {
                col.isTrigger = false;
                if (waiting)
                    MoveAway((OrientationEnum)Mathf.RoundToInt(-Mathf.Sign(direction.x)));
                else if (!waiting && employment > 8)
                {
                    StopMoving();
                    if ((targetPosition - pos).x * (int)orientation < 0f)
                        Turn();
                    Attack();
                }
            }
            else if (sqDistance < waitingFarDistance * waitingFarDistance && !insideWall)
            {
                col.isTrigger = true;
                StopMoving();
                if ((int)orientation * (targetPosition - pos).x < 0f)
                    Turn();

                if (!waiting && employment > 8)
                {
                    StopMoving();
                    Attack();
                }
            }
            else
            {
                col.isTrigger = true;
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(direction.x)));
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

    #region effects

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeStunned(float _time)
    {}

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeBurning(float _time)
    {}

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeCold(float _time)
    {}

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeWet(float _time)
    {}

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomePoisoned(float _time)
    {}

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeFrozen(float _time)
    {}

    #endregion //effects

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
                if (!currentTarget.exists && beginPosition.transform==null)
                {
                    //Если не получается добраться до начальной позиции, то считаем, что текущая позиция становится начальной
                    beginPosition = new ETarget(transform.position);
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
                transform.position = pos + direction.normalized * Mathf.Clamp(optSpeed, 0f, direction.magnitude);
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
