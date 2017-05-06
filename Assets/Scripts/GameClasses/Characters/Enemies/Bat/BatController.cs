using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, реализующий поведение летучей мыши
/// </summary>
public class BatController : AIController
{

    #region consts

    protected const float pushBackForce = 100f;
    protected const float batSize = .2f;

    protected const float r1 = 0.6f, r2 = 4f, r3 = 1.2f;

    protected const float maxAvoidDistance = 10f, avoidOffset = .5f;

    #endregion //consts

    #region fields

    protected Hearing hearing;//Слух персонажа
    
    public LayerMask whatIsGround;

    #endregion //fields

    #region parametres

    public override LoyaltyEnum Loyalty
    {
        get
        {
            return base.Loyalty;
        }

        set
        {
            base.Loyalty = value;
            if (hearing != null)
                hearing.AllyHearing = (value == LoyaltyEnum.ally);
        }
    }

    protected override float attackDistance { get { return .6f; } }
    [SerializeField] private float attackForce=200f;//С какой силой летучая мышь устремляется к противнику для совершения атаки
    [SerializeField] private float cooldown=3f;
    private bool canAttack=true;

    #endregion //parametres

    protected override void FixedUpdate()
    {
        if (!immobile)
            base.FixedUpdate();
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        indicators = transform.FindChild("Indicators");
        hearing = indicators.GetComponentInChildren<Hearing>();
        hearing.hearingEventHandler += HandleHearingEvent;

        base.Initialize();
        rigid.gravityScale = 0f;
        rigid.isKinematic = true;

        if (hitBox!=null)
            hitBox.AttackEventHandler += HandleAttackProcess;

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            areaTrigger.InitializeAreaTrigger();
        }
        
        BecomeCalm();

    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 pos = transform.position;
        Vector2 targetVelocity = (currentTarget - pos).normalized * speed*speedCoof;
        if (Physics2D.Raycast(pos+targetVelocity.normalized*batSize/4f,targetVelocity.normalized,batSize/4f,LayerMask.GetMask(wLName)))
            targetVelocity = Vector2.zero;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity,Time.fixedDeltaTime*acceleration);
        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Остановить передвижение
    /// </summary>
    protected override void StopMoving()
    {
        rigid.velocity = Vector2.zero;
    }

    /// <summary>
    /// Двинуться прочь от цели
    /// </summary>
    /// <param name="_orientation">Ориентация персонажа при перемещении</param>
    protected override void MoveAway(OrientationEnum _orientation)
    {
        Vector2 pos = transform.position;
        Vector2 targetVelocity = (pos - currentTarget).normalized * speed*speedCoof;
        if (Physics2D.Raycast(pos + targetVelocity.normalized*batSize/4f, targetVelocity.normalized, batSize/4f, LayerMask.GetMask(wLName)))
            targetVelocity = Vector2.zero;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Выйти из водоёма
    /// </summary>
    protected virtual void MoveFromWater()
    {
        Vector2 targetVelocity = Vector2.up * speed * speedCoof;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator AttackProcess()
    {
        StopMoving();
        Animate(new AnimationEventArgs("attack", "", Mathf.RoundToInt(100 * attackParametres.withoutEndAttackTime )));
        StartCoroutine(CooldownProcess());
        employment = Mathf.Clamp(employment - 4, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.preAttackTime);
        StopMoving();
        hitBox.AttackDirection = (MainTarget - transform.position).normalized;
        rigid.AddForce(hitBox.AttackDirection * attackForce*speedCoof);
        hitBox.SetHitBox(new HitParametres(attackParametres));
        employment = Mathf.Clamp(employment + 1, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.actTime);
        Animate(new AnimationEventArgs("stop"));
        Animate(new AnimationEventArgs("fly"));
        StopMoving();
        employment = Mathf.Clamp(employment - 1, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.endAttackTime);
        employment = Mathf.Clamp(employment + 4, 0, maxEmployment);
    }

    /// <summary>
    /// Процесс после неудавшейся атаки, когда мышь ничего не может 
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator AttackShockProcess()
    {
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(1.5f);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Процесс, в течение которого мышь не может совершать атаки
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator CooldownProcess()
    {
        canAttack = false;
        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }

    /// <summary>
    /// Принудительно прекратить атаку
    /// </summary>
    protected override void StopAttack()
    {
        base.StopAttack();
        employment = maxEmployment;
        StopMoving();
        StopCoroutine("AttackShockProcess");
        StartCoroutine("AttackShockProcess");
    }

    /// <summary>
    /// Анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        base.Analyse();
        Vector2 pos = transform.position;

        switch (behavior)
        {
            case BehaviorEnum.agressive:
                {
                    Vector2 targetDistance = mainTarget - pos;
                    if (mainTarget.exists ? Physics2D.Raycast(pos, targetDistance.normalized, targetDistance.magnitude, whatIsGround) : false)
                    {
                        waypoints = FindPath(mainTarget, 0);
                        currentTarget.exists = false;
                    }
                    else
                    {
                        currentTarget = mainTarget;
                        waypoints = null;
                    }

                    if (rigid.velocity.magnitude < minSpeed && employment>7)
                    {
                        float angle = 0f;
                        Vector2 rayDirection;
                        for (int i = 0; i < 8; i++)
                        {
                            rayDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                            if (Physics2D.Raycast(pos, rayDirection, batSize, whatIsGround))
                            {
                                rigid.AddForce(-rayDirection * pushBackForce / 2f);
                                StopAttack();
                                break;
                            }
                            angle += Mathf.PI / 4f;
                        }
                    }

                    //if (Physics2D.OverlapCircle(pos, batSize / 5f, LayerMask.GetMask(wLName)))
                    //{
                    if (underWater)
                        if (employment <= 7)
                        {
                            StopAttack();
                            StopCoroutine("AttackShockProcess");
                            employment = maxEmployment;
                        }
                        /*if (!underWater)
                            Underwater = true;
                    }
                    else if (!Physics2D.OverlapCircle(pos, batSize / 3f, LayerMask.GetMask(wLName)))
                    {
                        if (underWater)
                            Underwater = false;
                    }*/
                    //Если текущая цель убежала достаточно далеко, то мышь просто возвращается домой
                    if (Vector2.SqrMagnitude(mainTarget - transform.position) > r2 * r2)
                        GoHome();
                    break;
                }

            case BehaviorEnum.patrol:
                {
                    if (rigid.velocity.magnitude < minSpeed)
                    {
                        float angle = 0f;
                        Vector2 rayDirection;
                        for (int i = 0; i < 8; i++)
                        {
                            rayDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                            if (Physics2D.Raycast(transform.position, rayDirection, batSize, whatIsGround))
                            {
                                rigid.AddForce(-rayDirection * pushBackForce / 2f);
                                break;
                            }
                            angle += Mathf.PI / 4f;
                        }
                    }

                    if (loyalty == LoyaltyEnum.ally && !mainTarget.exists) //Если нет основной цели и летучая мышь - союзник героя, то она следует к нему
                    {
                        float sqDistance = Vector2.SqrMagnitude(beginPosition - pos);
                        if (followAlly)
                        {
                            if (Vector2.SqrMagnitude(beginPosition - (Vector2)prevTargetPosition) > minCellSqrMagnitude)
                            {
                                prevTargetPosition = new EVector3(pos);//Динамическое преследование героя-союзника
                                GoHome();
                                StartCoroutine("ConsiderAllyPathProcess");
                            }
                        }
                        else if (sqDistance < allyDistance)
                            BecomeCalm();
                    }

                    break;
                }
            case BehaviorEnum.calm:
                {
                    if (loyalty == LoyaltyEnum.ally)
                    {
                        if (Vector2.SqrMagnitude(beginPosition - pos) > allyDistance * 1.2f)
                        {
                            GoHome();    
                        }
                        if ((int)orientation * (beginPosition - pos).x < 0f)
                            Turn();//Всегда быть повёрнутым к герою-союзнику
                    }
                    break;
                }
            default:
                break;
        }

    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        if (!optimized)
            rigid.isKinematic = false;
        hearing.enabled = false;//В агрессивном состоянии персонажу не нужен слух
        StartCoroutine(CooldownProcess());
        Animate(new AnimationEventArgs("fly"));
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        rigid.isKinematic = true;
        hearing.Radius = r1;
        hearing.enabled = true;
    }

    /// <summary>
    /// Перейти в состояние патрулирования
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        hearing.Radius = r3;
        if (!optimized)
            rigid.isKinematic = false;
        hearing.enabled = true;
    }

    #region behaviourActions

    /// <summary>
    /// Функция, реализующая спокойное состояние ИИ
    /// </summary>
    protected override void CalmBehavior()
    {
        if (rigid.velocity.magnitude < minSpeed)
        {
            if (loyalty!=LoyaltyEnum.ally)
                Animate(new AnimationEventArgs("idle"));
        }
        else
        {
            Animate(new AnimationEventArgs("fly"));
        }
    }

    //Функция, реализующая агрессивное состояние ИИ
    protected override void AgressiveBehavior()
    {
        if (employment < 8 || !mainTarget.exists || !currentTarget.exists)
            return;

        Vector2 targetPosition = currentTarget;
        Vector2 pos = transform.position;

        if (underWater)
        {
            MoveFromWater();
            Animate(new AnimationEventArgs("fly"));
            return;
        }

        if (waypoints == null)
        {
            if (!waiting)
            {
                Vector2 targetDistance = targetPosition - pos;
                float sqDistance = targetDistance.sqrMagnitude;
                if (canAttack ? sqDistance < attackDistance * attackDistance : false)
                {
                    StopMoving();
                    if (targetDistance.x * (int)orientation < 0f)
                        Turn();
                    Attack();
                }
                else if (sqDistance > attackDistance * attackDistance / 2f)
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                else
                {
                    StopMoving();
                    if (targetDistance.x * (int)orientation < 0f)
                        Turn();
                }
            }
            else
            {
                float sqDistance = Vector2.SqrMagnitude(targetPosition - pos);
                if (sqDistance < waitingNearDistance * waitingNearDistance)
                    MoveAway((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(pos.x - targetPosition.x)));
                else if (sqDistance < waitingFarDistance * waitingFarDistance)
                {
                    StopMoving();
                    if ((int)orientation * (targetPosition - pos).x < 0f)
                        Turn();
                }
                else
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
            }
        }
        else
        {
            if (!currentTarget.exists)
                currentTarget = new ETarget(waypoints[0].cellPosition);

            targetPosition = currentTarget;
            pos = transform.position;
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
            if (currentTarget != mainTarget && Vector2.SqrMagnitude(currentTarget - pos) < batSize * batSize)
            {
                waypoints.RemoveAt(0);
                if (waypoints.Count == 0)
                {
                    waypoints = null;
                    currentTarget.Exists = false;
                    //Достигли конца маршрута
                    if (Vector3.Distance(beginPosition, transform.position) < batSize)
                    {
                        transform.position = beginPosition;
                        Animate(new AnimationEventArgs("idle"));
                        BecomeCalm();
                    }
                    else
                        GoHome();//Никого в конце маршрута не оказалось, значит, возвращаемся домой
                }
                else
                {
                    //Продолжаем следование
                    currentTarget = new ETarget(waypoints[0].cellPosition);
                }
            }
        }

        Animate(new AnimationEventArgs("fly"));
    }

    /// <summary>
    /// Функция, реализующая состояние ИИ, при котором тот перемещается между текущими точками следования
    /// </summary>
    protected override void PatrolBehavior()
    {

        if (waypoints != null ? waypoints.Count > 0 : false)
        {
            if (!currentTarget.exists)
                currentTarget = new ETarget(waypoints[0].cellPosition);

            Vector2 targetPosition = currentTarget;
            Vector2 pos = transform.position;
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
            if (currentTarget != mainTarget && Vector2.SqrMagnitude(currentTarget - pos) < batSize * batSize)
            {
                waypoints.RemoveAt(0);
                if (waypoints.Count == 0)
                {
                    currentTarget.Exists = false;
                    //Достигли конца маршрута
                    if (Vector3.Distance(beginPosition, transform.position) < batSize)
                    {
                        transform.position = beginPosition;
                        Animate(new AnimationEventArgs("idle"));
                        BecomeCalm();
                    }
                    else
                        GoHome();//Никого в конце маршрута не оказалось, значит, возвращаемся домой
                }
                else
                {
                    //Продолжаем следование
                    currentTarget = new ETarget(waypoints[0].cellPosition);
                }
            }
        }
        else
            GoHome();
        Animate(new AnimationEventArgs("fly"));
    }

    #endregion //behaviourActions

    #region optimization

    /// <summary>
    /// Включить риджидбоди
    /// </summary>
    protected override void EnableRigidbody()
    {
        if (behavior!=BehaviorEnum.calm)
            rigid.isKinematic = false;
    }

    /// <summary>
    /// Функция реализующая анализ окружающей персонажа обстановки, когда тот находится в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
        switch (behavior)
        {
            case BehaviorEnum.agressive:
                {
                    if (!followOptPath)
                        StartCoroutine("PathPassOptProcess");
                    if (behavior == BehaviorEnum.agressive)
                        if (Vector2.SqrMagnitude(mainTarget - (Vector2)transform.position) > r2 * r2)
                            GoHome();
                    break;
                }
            case BehaviorEnum.patrol:
                {
                    if (!followOptPath)
                        StartCoroutine("PathPassOptProcess");
                    break;
                }
            default:
                break;
        }
    }

    /// <summary>
    /// Функция, которая восстанавливает положение и состояние персонажа, пользуясь данными, полученными в оптимизированном режиме
    /// </summary>
    protected override void RestoreActivePosition()
    {
        if (currentTarget.exists)
            Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign((currentTarget - transform.position).x)));
    }

    /// <summary>
    /// Процесс оптимизированного прохождения пути. Заключается в том, что персонаж, зная свой маршрут, появляется в его различиных позициях, не используя 
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator PathPassOptProcess()
    {
        followOptPath = true;
        if (waypoints == null && !currentTarget.exists)
        {
            if (Vector2.SqrMagnitude((Vector2)transform.position - beginPosition) < minCellSqrMagnitude)
                BecomeCalm();
            else
            {
                GoHome();
                if (waypoints == null && beginPosition.transform==null)
                {
                    //Если не получается добраться до начальной позиции (и если эта позиция - не главный герой, за которым следует союзник), то считаем, что текущая позиция становится начальной
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
            while ((waypoints != null ? waypoints.Count > 0 : false) || currentTarget.exists)
            {
                if (!currentTarget.exists)
                    currentTarget = new ETarget(waypoints[0].cellPosition);

                Vector2 pos = transform.position;
                Vector2 targetPos = currentTarget;                

                if (Vector2.SqrMagnitude(pos - targetPos) <= minCellSqrMagnitude)
                {
                    transform.position = targetPos;
                    pos = transform.position;
                    currentTarget.Exists = false;
                    if (waypoints != null ? waypoints.Count > 0 : false)
                    {
                        NavigationCell currentCell = waypoints[0];
                        waypoints.RemoveAt(0);
                        if (waypoints.Count <= 0)
                            break;
                        NavigationCell nextCell = waypoints[0];
                        currentTarget = new ETarget(nextCell.cellPosition);
                    }
                }
                targetPos = currentTarget;
                yield return new WaitForSeconds(optTimeStep);
                Vector2 direction = targetPos - pos;
                transform.position = pos + direction.normalized * Mathf.Clamp(optSpeed, 0f, direction.magnitude);
            }
            waypoints = null;
            currentTarget.Exists = false;
            followOptPath = false;
        }
    }

    #endregion //optimization

    /// <summary>
    /// Функция, которая строит маршрут
    /// </summary>
    /// <param name="endPoint">точка назначения</param>
    ///<param name="maxDepth">Максимальная сложность маршрута</param>
    /// <returns>Навигационные ячейки, составляющие маршрут</returns>
    protected override List<NavigationCell> FindPath(Vector2 endPoint, int _maxDepth)
    {
        if (navMap == null)
            return null;
        return navMap.GetPath(transform.position, endPoint, true);
    }

    /// <summary>
    /// Вернуть тип, используемой карты навигации
    /// </summary>
    public override NavMapTypeEnum GetMapType()
    {
        return NavMapTypeEnum.fly;
    }

    #region eventHandlers

    /*
    /// <summary>
    /// Обработка события "Услышал врага"
    /// </summary>
    protected override void HandleHearingEvent(object sender, EventArgs e)
    {
        if (behaviour!=BehaviourEnum.agressive)
            BecomeAgressive();
    }*/

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected override void HandleAttackProcess(object sender, HitEventArgs e)
    {
        if (mainTarget.exists)
        {
            rigid.velocity = Vector2.zero;
            rigid.AddForce((transform.position - mainTarget).normalized * pushBackForce);//При столкновении с врагом летучая мышь отталкивается назад
        }
        else
        {
            base.HandleAttackProcess(sender, e);
        }
        StopAttack();
    }

    #endregion //eventHandlers

}
