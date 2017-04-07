using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер, управляющий гуманоидом
/// </summary>
public class HumanoidController : AIController
{

    #region consts

    protected const float platformOffset = .35f;
    protected const float jumpTime = .2f;

    #endregion //consts

    #region fields

    protected WallChecker wallCheck, precipiceCheck;
    protected WallChecker groundCheck;

    protected Hearing hearing;//Слух персонажа

    protected override List<NavigationCell> Waypoints
    {
        get
        {
            return waypoints;
        }
        set
        {
            StopFollowOptPath();
            waypoints = value;
            if (value != null)
            {
                currentTarget.Exists = false;
            }
            else
            {
                StopAvoid();
                LadderOff();
                if (mainTarget.exists && behavior == BehaviorEnum.agressive)
                {
                    currentTarget = mainTarget;
                }
            }
            if (currentPlatform != null)
                CurrentPlatform = null;
            jumping = false;
        }
    }

    #endregion //fields

    #region parametres

    [SerializeField]protected float ladderSpeed = 0.8f;//Скорость передвижения по лестнице
    protected bool onLadder = false;//Находится ли монстр на лестнице
    public override bool OnLadder { get { return onLadder; } }
    protected bool grounded { get { return groundCheck ? groundCheck.WallInFront : true; } }//Находится ли монстр на земле

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
                hearing.AllyHearing = (value==LoyaltyEnum.ally);
            if (value == LoyaltyEnum.ally)
                wallCheck.WhatIsWall.Remove("character");
            else
                if (!wallCheck.WhatIsWall.Contains("character"))
                    wallCheck.WhatIsWall.Add("character");
        }
    }

    #region platform

    protected ETarget platformTarget;//Целевая движущаяся платформа
    public ETarget PlatformTarget { get { return platformTarget; } }
    protected MovingPlatform currentPlatform = null;//Движущаяся платформа, которая является предметом интереса для ИИ
    protected MovingPlatform CurrentPlatform
    {
        get
        {
            return currentPlatform;
        }
        set
        {
            currentPlatform = value;
            if (currentPlatform == null)
                waitingForPlatform = false;
            platformTarget = value != null ? new ETarget(currentPlatform.transform) : ETarget.zero;
        }
    }
    protected bool waitingForPlatform = false;//Ждёт ли персонаж выдвижения движущейся платформы?
    protected NavCellTypeEnum platformConnectionType;//Каким образом надо добираться до плафтормы

    #endregion //platform

    protected override float attackDistance { get { return .12f; } }

    #endregion //parametres

    protected override void FixedUpdate()
    {
        if (!immobile)
        {
            base.FixedUpdate();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (onLadder)
        {
            Animate(new AnimationEventArgs("ladderMove"));
        }
        else if (!grounded)
        {
            Animate(new AnimationEventArgs("airMove"));
        }
        else
        {
            Animate(new AnimationEventArgs("groundMove"));
        }
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        indicators = transform.FindChild("Indicators");
        if (indicators != null)
        {
            wallCheck = indicators.FindChild("WallCheck").GetComponent<WallChecker>();
            precipiceCheck = indicators.FindChild("PrecipiceCheck").GetComponent<WallChecker>();
            groundCheck = indicators.FindChild("GroundCheck").GetComponent<WallChecker>();

            hearing = indicators.GetComponentInChildren<Hearing>();
            if (hearing != null)
                hearing.hearingEventHandler += HandleHearingEvent;
        }

        base.Initialize();

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            areaTrigger.InitializeAreaTrigger();
        }

        BecomeCalm();

    }

    /// <summary>
    /// Передвижение
    /// </summary>
    /// <param name="_orientation">Направление движения (влево/вправо)</param>
    protected override void Move(OrientationEnum _orientation)
    {
        bool wallInFront = wallCheck.WallInFront;
        Vector2 targetVelocity = wallInFront ? new Vector2(0f, rigid.velocity.y) : new Vector2((int)orientation * speed * speedCoof, rigid.velocity.y);
        rigid.velocity = wallInFront ? Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration) : targetVelocity;

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    /// <param name="_orientation">В какую сторону должен смотреть персонаж после поворота</param>
    public override void Turn(OrientationEnum _orientation)
    {
        base.Turn(_orientation);
        wallCheck.SetPosition(0f, (int)orientation);
        precipiceCheck.SetPosition(0f, (int)orientation);
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    protected override void Turn()
    {
        base.Turn();
        wallCheck.SetPosition(0f, (int)orientation);
        precipiceCheck.SetPosition(0f, (int)orientation);
    }

    /// <summary>
    /// Взобраться на лестницу
    /// </summary>
    protected override void LadderOn()
    {
        if (waypoints != null ? ((ComplexNavigationCell)waypoints[0]).cellType == NavCellTypeEnum.ladder : false)
        {
            string lLName = "ladder";
            Collider2D col = Physics2D.OverlapArea(waypoints[0].cellPosition + new Vector2(-minDistance / 2f, minDistance / 2f), waypoints[0].cellPosition + new Vector2(minDistance / 2f, -minDistance / 2f), LayerMask.GetMask(lLName));
            if (col == null)
                return;
            onLadder = true;
            Animate(new AnimationEventArgs("setLadderMove", "", 1));
            Vector3 vect = transform.position;
            transform.position = new Vector3(col.gameObject.transform.position.x, vect.y, vect.z);
            base.LadderOn();
        }
    }

    /// <summary>
    /// Слезть с лестницы
    /// </summary>
    protected override void LadderOff()
    {
        if (onLadder)
        {
            if (rigid.velocity.y > 0f && !jumping)
            {
                rigid.AddForce(new Vector2(0f, jumpForce / 2f));
            }
            onLadder = false;
        }
        rigid.gravityScale = 1f;
        Animate(new AnimationEventArgs("setLadderMove", "", 0));
        base.LadderOff();
    }

    /// <summary>
    /// Перемещение по лестнице
    /// </summary>
    protected override void LadderMove(float direction)
    {
        rigid.velocity = new Vector2(0f, direction * ladderSpeed * speedCoof);
    }

    /// <summary>
    /// Совершить прыжок
    /// </summary>
    protected override void Jump()
    {
        rigid.velocity = new Vector3(rigid.velocity.x, 0f, 0f);
        rigid.AddForce(new Vector2(jumpForce * 0.5f, jumpForce));
        StartCoroutine(JumpProcess());
        if (onLadder)
            LadderOff();
    }

    /// <summary>
    /// Процесс самого прыжка
    /// </summary>
    protected IEnumerator JumpProcess()
    {
        jumping = true;
        yield return new WaitForSeconds(jumpTime);
        jumping = false;
    }

    /// <summary>
    /// Провести анализ окружающей обстановки
    /// </summary>
    protected override void Analyse()
    {
        Vector2 pos = transform.position;
        switch (behavior)
        {
            case BehaviorEnum.agressive:
                {

                    Vector2 direction = mainTarget - pos;
                    RaycastHit2D hit = Physics2D.Raycast(pos, direction.normalized, direction.magnitude, LayerMask.GetMask(gLName));
                    if (hit)
                    {
                        //Если враг ушёл достаточно далеко
                        if (direction.magnitude > sightRadius * 0.75f)
                        {
                            GoToThePoint(mainTarget);
                            if (behavior == BehaviorEnum.agressive)
                            {
                                GoHome();
                                break;
                            }
                            else
                                StartCoroutine("BecomeCalmProcess");
                        }
                    }
                    //if (currentTarget == null)
                    //break;
                    if (!hit ?
                        ((pos - prevPosition).sqrMagnitude < speed * speedCoof * Time.fixedDeltaTime / 10f && currentTarget != mainTarget &&
                        (platformTarget.exists ? true : (!waitingForPlatform && transform.parent == null))) : true)
                    {
                        if (!avoid)
                            StartCoroutine("AvoidProcess");
                    }

                    if (waitingForPlatform)
                    {
                        if (WatchPlatform())
                        {
                            waitingForPlatform = false;
                            currentTarget = platformTarget;
                            if (platformConnectionType == NavCellTypeEnum.jump)
                            {
                                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign((currentTarget - pos).x)));
                                //transform.position = new Vector3(currentTarget.transform.position.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                                Jump();
                            }
                        }
                    }

                    break;
                }
            case BehaviorEnum.patrol:
                {
                    if (!currentTarget.exists)
                    {
                        if (!avoid)
                        {
                            StartCoroutine("AvoidProcess");
                        }
                        break;
                    }
                    if (!avoid)
                    {
                        if ((pos - prevPosition).sqrMagnitude < speed * speedCoof * Time.fixedDeltaTime / 10f &&
                            (platformTarget.exists ? true : (!waitingForPlatform && transform.parent == null)))
                            StartCoroutine("AvoidProcess");
                    }
                    Vector2 direction = Vector3.right * (int)orientation;
                    if (mainTarget.exists)
                    {
                        if (Vector2.SqrMagnitude(mainTarget - pos) < minCellSqrMagnitude)
                        {
                            MainTarget = mainTarget;
                            BecomeAgressive();
                        }
                    }
                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * direction, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (enemies.Contains(hit.collider.gameObject.tag))
                        {
                            MainTarget = new ETarget(hit.collider.transform);
                            BecomeAgressive();
                        }
                    }

                    if (waitingForPlatform)
                    {
                        if (WatchPlatform())
                        {
                            waitingForPlatform = false;
                            currentTarget = platformTarget;
                            if (platformConnectionType == NavCellTypeEnum.jump)
                            {
                                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign((currentTarget - pos).x)));
                                //transform.position = new Vector3(currentTarget.transform.position.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                                Jump();
                            }
                        }
                    }
                    //Если нет основной цели и стоящий на земле гуманоид - союзник героя, то он следует к нему
                    if (loyalty == LoyaltyEnum.ally? !mainTarget.exists && grounded: false) 
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
                        else
                        if (sqDistance < allyDistance)
                        {
                            if (grounded || onLadder)
                                StopMoving();
                            BecomeCalm();
                        }
                    }

                    break;
                }

            case BehaviorEnum.calm:
                {
                    Vector2 direction = Vector3.right * (int)orientation;
                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * direction, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (enemies.Contains(hit.collider.gameObject.tag))
                        {
                            MainTarget = new ETarget(hit.collider.transform);
                            BecomeAgressive();
                        }
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
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        balance = attackBalance;
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.preAttackTime);
        hitBox.SetHitBox(new HitParametres(attackParametres));
        hitBox.AttackDirection = Vector2.right * (int)orientation;
        yield return new WaitForSeconds(attackParametres.actTime + attackParametres.endAttackTime);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
        balance = usualBalance;
    }

    /// <summary>
    /// Определить, нужно ли искать отдельный маршрут до главной цели
    /// </summary>
    /// <returns>Возвращает факт необходимости поиска пути</returns>
    protected override bool NeedToFindPath()
    {
        Vector2 mainPos = mainTarget;
        bool onPlatform = false;
        bool changePosition = (grounded || onLadder) && Vector2.SqrMagnitude(mainPos - prevTargetPosition) > minCellSqrMagnitude * 16f;
        if (changePosition)
        {
            ComplexNavigationCell cell1 = (ComplexNavigationCell)navMap.GetCurrentCell(mainPos), cell2 = (ComplexNavigationCell)navMap.GetCurrentCell(prevTargetPosition);
            if (cell1 == null || cell2 == null)
                onPlatform = false;
            else
                onPlatform = (cell1.cellType == NavCellTypeEnum.movPlatform) && (cell1.id == cell2.id);
        }
        return changePosition && !onPlatform;

    }

    /// <summary>
    /// Функция, которая следит за перемещением движеущейся платформы, интересующую ИИ
    /// </summary>
    /// <returns>Платформа находится рядом с текущей целью ИИ?</returns>
    protected virtual bool WatchPlatform()
    {
        if (!platformTarget.exists || !currentTarget.exists)
            return false;
        Vector2 platformPos = platformTarget;
        Vector2 platformDirection = currentPlatform.Direction;
        Vector2 pos = transform.position;
        if (pos.y < platformPos.y && Vector2.Dot(pos - platformPos, platformDirection) < 0f)
            return false;
        return (Vector2.SqrMagnitude(platformTarget + currentPlatform.Direction * platformOffset - currentTarget) < navCellSize * navCellSize);
    }

    /// <summary>
    /// Найти нужную платформу по её ID и сделать её предметом интереса ИИ
    /// </summary>
    /// <param name="platformID">Идентификатор движущейся платформы</param>
    protected virtual void FindPlatform(int platformID)
    {
        GameObject platforms = GameObject.Find("platforms");
        for (int i = 0; i < platforms.transform.childCount; i++)
        {
            MovingPlatform _platform = platforms.transform.GetChild(i).GetComponent<MovingPlatform>();
            if (_platform == null)
                continue;
            if (_platform.GetID() == platformID)
            {
                CurrentPlatform = _platform;
                break;
            }
        }
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(HitParametres hitData)
    {
        base.TakeDamage(hitData);
        bool stunned = GetBuff("StunnedProcess") != null;
        bool frozen = GetBuff("FrozenProcess") != null;
        if (hitData.attackPower > balance || stunned || frozen)
        {
            if (onLadder && (!frozen))
                LadderOff();//Сбросить с лестницы
        }
    }

    /// <summary>
    /// Обновить информацию, важную для моделей поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        currentTarget.Exists = false;
        jumping = false;
        StopAvoid();
        StopCoroutine("BecomeCalmProcess");
        prevTargetPosition = EVector3.zero;
        StopFollowOptPath();
        StopAttack();
        Waiting = false;
    }

    /// <summary>
    /// Перейти в спокойное состояние
    /// </summary>
    protected override void BecomeCalm()
    {
        RefreshTargets();
        behavior = BehaviorEnum.calm;
        mainTarget.Exists = false;
        TargetCharacter = null;
        Waypoints = null;
        if (optimized)
            behaviorActions = CalmOptBehavior;
        else
            behaviorActions = CalmBehavior;
        OnChangeBehavior(new BehaviorEventArgs(BehaviorEnum.calm));
        if (hearing!=null)
            hearing.enabled = true;
    }

    /// <summary>
    /// Перейти в агрессивное состояние
    /// </summary>
    protected override void BecomeAgressive()
    {
        if (!mainTarget.Exists)
            return;
        RefreshTargets();
        behavior = BehaviorEnum.agressive;
        if (onLadder || !grounded)
        {
            Waypoints = FindPath(mainTarget, maxAgressivePathDepth);
            if (waypoints == null)
                currentTarget = mainTarget;
        }
        else
        {
            waypoints = null;
            currentTarget = mainTarget;
        }
        if (currentPlatform != null)
            CurrentPlatform = null;
        if (optimized)
            behaviorActions = AgressiveOptBehavior;
        else
            behaviorActions = AgressiveBehavior;
        //wallCheck.RemoveWallType("character");
        if (hearing!=null)
            hearing.enabled = false;//В агрессивном состоянии персонажу не нужен слух
        OnChangeBehavior(new BehaviorEventArgs(BehaviorEnum.agressive));
    }

    /// <summary>
    /// Перейти в состояние патрулирования
    /// </summary>
    protected override void BecomePatrolling()
    {
        RefreshTargets();
        behavior = BehaviorEnum.patrol;
        //mainTarget = null;
        CurrentPlatform = null;
        TargetCharacter = null;
        if (optimized)
            behaviorActions = PatrolOptBehavior;
        else
            behaviorActions = PatrolBehavior;
        if (hearing != null)
            hearing.enabled = true;
        OnChangeBehavior(new BehaviorEventArgs(BehaviorEnum.patrol));
    }

    /// <summary>
    /// Процесс успокаивания персонажа
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator BecomeCalmProcess()
    {
        //calmDown = true;
        yield return new WaitForSeconds(beCalmTime);
        if (Vector2.SqrMagnitude((Vector2)transform.position - beginPosition) > minDistance)
            GoHome();
        else if (behavior != BehaviorEnum.calm)
            BecomeCalm();
        //wallCheck.WhatIsWall.Add("character");
    }

    /// <summary>
    /// Процесс обхода препятствия
    /// </summary>
    protected override IEnumerator AvoidProcess()
    {
        avoid = true;
        EVector3 _prevPos = prevPosition;
        yield return new WaitForSeconds(avoidTime);
        Vector3 pos = (Vector2)transform.position;
        if ((transform.position - _prevPos).sqrMagnitude < speed * speedCoof * Time.fixedDeltaTime / 10f && avoid)
        {
            if (currentTarget.exists &&
                (platformTarget.exists ? (!waitingForPlatform && transform.parent != platformTarget.transform) : true))
            {
                if (currentTarget != platformTarget && currentTarget != mainTarget)
                    transform.position += (currentTarget - pos).normalized * navCellSize;
                yield return new WaitForSeconds(avoidTime);
                pos = (Vector2)transform.position;
                //Если не получается обойти ставшее на пути препятствие
                if (currentTarget.exists && currentTarget != mainTarget && avoid &&
                    (pos - _prevPos).sqrMagnitude < speed * speedCoof * Time.fixedDeltaTime / 10f &&
                    (platformTarget.exists ? (!waitingForPlatform && transform.parent != platformTarget.transform) : true))
                {
                    if (mainTarget.exists)
                    {
                        if (behavior == BehaviorEnum.agressive)
                        {
                            Waypoints = FindPath(mainTarget, maxAgressivePathDepth);
                            if (waypoints == null)
                                GoHome();
                        }
                        else
                            GoHome();
                    }
                    else
                    {
                        if (waypoints != null ? waypoints.Count > 0 : false)
                            GoToThePoint(waypoints[waypoints.Count - 1].cellPosition);
                        else
                            GoHome();
                    }
                    if (behavior == BehaviorEnum.patrol && beginPosition.transform == null)
                        StartCoroutine(ResetStartPositionProcess(transform.position));

                }
            }
            else if (!currentTarget.exists)
            {
                yield return new WaitForSeconds(avoidTime);
                pos = (Vector2)transform.position;
                if (!currentTarget.exists)
                {
                    GoHome();
                    if (behavior == BehaviorEnum.patrol && beginPosition.transform == null)
                        StartCoroutine(ResetStartPositionProcess(pos));
                }
            }
        }
        avoid = false;
    }

    /// <summary>
    /// Специальный процесс, учитывающий то, что персонаж долгое время не может достичь какой-то позиции. Тогда, считаем, что опорной позицией персонажа станет текущая позиция (если уж он не может вернуться домой)
    /// </summary>
    /// <param name="prevPosition">Предыдущая позиция персонажа. Если спустя какое-то время ИИ никак не сдвинулся относительно неё, значит нужно привести в действие данный процесс</param>
    /// <returns></returns>
    protected override IEnumerator ResetStartPositionProcess(Vector2 prevPosition)
    {
        yield return new WaitForSeconds(avoidTime);
        Vector2 pos = transform.position;
        if (Vector2.SqrMagnitude(prevPosition - pos) < minCellSqrMagnitude && behavior == BehaviorEnum.patrol)
        {
            if ((platformTarget.exists ? (!waitingForPlatform && transform.parent != platformTarget.transform) : true))
            {
                beginPosition = new ETarget(pos);
                beginOrientation = orientation;
                BecomeCalm();
            }
        }
    }

    #region behaviourActions

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {

        if (mainTarget.exists && employment > 2)
        {

            Vector2 targetPosition;
            Vector2 targetDistance;
            Vector2 pos = transform.position;
            Vector2 mainPos = mainTarget;
            if (waypoints == null)
            {

                #region directWay

                targetPosition = mainTarget;
                targetDistance = targetPosition - pos;
                float sqDistance = targetDistance.sqrMagnitude;

                if (waiting)
                {

                    #region waiting

                    if (sqDistance < waitingNearDistance)
                    {
                        if (!wallCheck.WallInFront && (precipiceCheck.WallInFront || !grounded))
                            Move((OrientationEnum)Mathf.RoundToInt(-Mathf.Sign(targetDistance.x)));
                    }
                    else if (sqDistance < waitingFarDistance)
                    {
                        StopMoving();
                        if ((int)orientation * (targetPosition - pos).x < 0f)
                            Turn();
                    }
                    else
                    {
                        if (!wallCheck.WallInFront && (precipiceCheck.WallInFront || !grounded) && (Mathf.Abs((pos - mainPos).y) < navCellSize * 5f ? true : !targetCharacter.OnLadder))
                        {
                            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                        }
                        else if ((targetPosition - pos).x * (int)orientation < 0f)
                            Turn();
                        else
                        {
                            StopMoving();
                            if (Vector2.SqrMagnitude(pos - mainPos) > minCellSqrMagnitude * 16f &&
                                (Vector2.SqrMagnitude(mainPos - prevTargetPosition) > minCellSqrMagnitude || !prevTargetPosition.exists))
                            {
                                Waypoints = FindPath(targetPosition, maxAgressivePathDepth);
                                if (waypoints == null)
                                    StopMoving();
                            }
                        }
                    }

                    #endregion //waiting

                }

                else
                {

                    if (employment > 8)
                    {

                        if (Vector2.SqrMagnitude(targetDistance) > attackDistance * attackDistance)
                        {
                            if (!wallCheck.WallInFront && (precipiceCheck.WallInFront || !grounded) && (Mathf.Abs((pos - mainPos).y) < navCellSize * 5f ? true : !targetCharacter.OnLadder))
                            {
                                if (Mathf.Abs(targetDistance.x) > attackDistance)
                                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                                else
                                    StopMoving();
                            }
                            else if ((targetPosition - pos).x * (int)orientation < 0f)
                                Turn();
                            else
                            {
                                StopMoving();
                                if (Vector2.SqrMagnitude(pos - mainPos) > minCellSqrMagnitude * 16f &&
                                    (Vector2.SqrMagnitude(mainPos - prevTargetPosition) > minCellSqrMagnitude || !prevTargetPosition.exists))
                                {
                                    Waypoints = FindPath(targetPosition, maxAgressivePathDepth);
                                    if (waypoints == null)
                                        StopMoving();
                                }
                            }

                        }
                        else
                        {
                            StopMoving();
                            if ((targetPosition - pos).x * (int)orientation < 0f)
                                Turn();
                            Attack();
                        }
                    }
                }
                #endregion //directWay

            }
            else
            {

                #region complexWay

                if (NeedToFindPath())//Если главная цель сменила своё местоположение
                {
                    Waypoints = FindPath(mainPos, maxAgressivePathDepth);
                    //prevTargetPosition = new EVector3(mainTarget.transform.position, true);
                    return;
                }

                if (waypoints.Count > 0)
                {
                    if (!currentTarget.exists)
                    {
                        currentTarget = new ETarget(waypoints[0].cellPosition);
                        if (((ComplexNavigationCell)waypoints[0]).cellType == NavCellTypeEnum.movPlatform)
                            FindPlatform(((ComplexNavigationCell)waypoints[0]).id);
                    }

                    bool waypointIsAchieved = false;
                    targetPosition = currentTarget;
                    targetDistance = targetPosition - pos;
                    if (!waitingForPlatform && (transform.parent != platformTarget.transform || !platformTarget.exists))
                    {
                        if (onLadder)
                        {
                            LadderMove(Mathf.Sign(targetDistance.y));
                            waypointIsAchieved = Mathf.Abs(currentTarget.y - pos.y) < navCellSize / 2f;
                        }
                        else if (currentPlatform != null ? !grounded : true)
                        {
                            if (Mathf.Abs(targetDistance.x) > navCellSize / 4f)
                                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                            else
                            {
                                transform.position = new Vector3(targetPosition.x, pos.y);
                                StopMoving();
                            }
                            waypointIsAchieved = Vector3.SqrMagnitude(currentTarget - transform.position) < minCellSqrMagnitude;
                        }
                    }
                    else
                    {
                        StopMoving();
                        if (!waitingForPlatform)
                        {
                            Vector2 platformDirection = currentPlatform.Direction;
                            float projection = Vector2.Dot(targetDistance, platformDirection);
                            Turn((OrientationEnum)Mathf.Sign(platformDirection.x));
                            waypointIsAchieved = Mathf.Abs(projection) < navCellSize / 2f;
                            if (currentTarget == platformTarget && transform.parent == platformTarget.transform)
                            {
                                transform.position = platformTarget + Vector3.up * 0.08f;
                                waypointIsAchieved = true;
                            }
                        }
                    }


                    if (waypointIsAchieved)
                    {
                        ComplexNavigationCell currentWaypoint = (ComplexNavigationCell)waypoints[0];
                        if (currentTarget == platformTarget)
                        {
                            if (waypoints.Count > 1 ? (waypoints[1].cellPosition - (Vector2)transform.position).x * (int)orientation < 0f : false)
                                Turn();
                            transform.position = platformTarget + Vector3.up * .09f;
                        }
                        else
                            currentTarget.Exists = false;

                        waypoints.RemoveAt(0);

                        if (waypoints.Count > 2 ? !targetCharacter.OnLadder : false)//Проверить, не стал ли путь до главной цели прямым и простым, чтобы не следовать ему больше
                        {
                            bool directPath = true;
                            Vector2 cellsDirection = (waypoints[1].cellPosition - waypoints[0].cellPosition).normalized;
                            NavCellTypeEnum cellsType = ((ComplexNavigationCell)waypoints[0]).cellType;
                            for (int i = 2; i < waypoints.Count; i++)
                            {
                                if (Vector2.Angle((waypoints[i].cellPosition - waypoints[i - 1].cellPosition), cellsDirection) > minAngle || ((ComplexNavigationCell)waypoints[i - 1]).cellType != cellsType)
                                {
                                    directPath = false;
                                    break;
                                }
                            }

                            if (directPath)
                            {
                                //Если путь прямой, несложный, то монстр может самостоятельно добраться до игрока, не используя маршрут, и атаковать его
                                Waypoints = null;
                                return;
                            }
                        }

                        if (waypoints.Count == 0)//Если маршрут кончился, перестать следовать ему
                        {
                            if (!targetCharacter.OnLadder)
                            {
                                Waypoints = null;
                                return;
                            }
                            else
                            {
                                if (onLadder)
                                    StopLadderMoving();
                            }
                        }
                        else
                        {
                            ComplexNavigationCell nextWaypoint = (ComplexNavigationCell)waypoints[0];
                            NeighborCellStruct neighborConnection = currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb);
                            //Продолжаем следование
                            currentTarget = new ETarget(nextWaypoint.cellPosition);
                            if (nextWaypoint.cellType == NavCellTypeEnum.movPlatform && !platformTarget.exists)
                            {
                                StopMoving();
                                waitingForPlatform = true;
                                FindPlatform(nextWaypoint.id);
                                platformConnectionType = neighborConnection.connectionType;
                            }
                            else if (neighborConnection.connectionType == NavCellTypeEnum.jump && (grounded || onLadder))
                            {
                                //Перепрыгиваем препятствие
                                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x)));
                                //transform.position = new Vector3(currentWaypoint.cellPosition.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                                Jump();
                            }
                            /*else if (Mathf.Abs(currentWaypoint.cellPosition.x-nextWaypoint.cellPosition.x)<2*navCellSize &&
                                       currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb).connectionType == NavCellTypeEnum.jump &&
                                       nextWaypoint.cellPosition.y < currentWaypoint.cellPosition.y - 2 * navCellSize)
                            {
                                //Спрыгиваем вниз
                                jumping = true;
                            }*/
                            else if (!onLadder ?
                                currentWaypoint.cellType == NavCellTypeEnum.ladder && nextWaypoint.cellType == NavCellTypeEnum.ladder && Mathf.Approximately(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x, 0f) : false)
                            {
                                LadderOn();
                            }
                            if (currentWaypoint.cellType == NavCellTypeEnum.ladder && (currentWaypoint.id != nextWaypoint.id))
                            {
                                LadderOff();
                                //Jump();
                            }
                            else if (currentWaypoint.cellType == NavCellTypeEnum.movPlatform && nextWaypoint.cellType != NavCellTypeEnum.movPlatform)
                                CurrentPlatform = null;
                        }
                    }
                }
            }

            #endregion //complexWay

        }
    }

    /// <summary>
    /// Поведение патрулирования
    /// </summary>
    protected override void PatrolBehavior()
    {
        Vector2 pos = transform.position;
        if (waypoints != null ? waypoints.Count > 0 : false)
        {
            if (!currentTarget.exists)
            {
                currentTarget = new ETarget(waypoints[0].cellPosition);
                if (((ComplexNavigationCell)waypoints[0]).cellType == NavCellTypeEnum.movPlatform)
                    FindPlatform(((ComplexNavigationCell)waypoints[0]).id);
            }

            bool waypointIsAchieved = false;
            Vector2 targetPosition = currentTarget;
            Vector2 targetDistance = targetPosition - pos;
            if (!waitingForPlatform && (transform.parent != platformTarget.transform || !platformTarget.exists))
            {
                if (onLadder)
                {
                    LadderMove(Mathf.Sign(targetDistance.y));
                    waypointIsAchieved = Mathf.Abs(currentTarget.y - pos.y) < navCellSize / 2f;
                }
                else if (currentPlatform != null ? !grounded : true)
                {
                    if (Mathf.Abs(targetDistance.x) > navCellSize / 4f)
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                    else
                    {
                        transform.position = new Vector3(targetPosition.x, pos.y);
                        StopMoving();
                    }
                    waypointIsAchieved = Vector2.SqrMagnitude(currentTarget - pos) < minCellSqrMagnitude;
                }
            }
            else
            {
                StopMoving();
                if (!waitingForPlatform)
                {
                    Vector2 platformDirection = currentPlatform.Direction;
                    float projection = Vector2.Dot(targetDistance, platformDirection);
                    Turn((OrientationEnum)Mathf.Sign(platformDirection.x));
                    waypointIsAchieved = Mathf.Abs(projection) < navCellSize / 2f;
                    if (currentTarget == platformTarget && transform.parent == platformTarget.transform)
                    {
                        transform.position = platformTarget + Vector3.up * 0.08f;
                        waypointIsAchieved = true;
                    }
                }
            }

            if (waypointIsAchieved)
            {
                ComplexNavigationCell currentWaypoint = (ComplexNavigationCell)waypoints[0];
                if (currentTarget == platformTarget)
                {
                    if (waypoints.Count > 1 ? (waypoints[1].cellPosition - pos).x * (int)orientation < 0f : false)
                        Turn();
                    transform.position = platformTarget + Vector3.up * .09f;
                }
                else
                    currentTarget.Exists = false;

                waypoints.RemoveAt(0);

                if (waypoints.Count == 0)//Если маршрут кончился, перестать следовать ему
                {
                    StopMoving();
                    //Достигли конца маршрута
                    if (Vector3.Distance(beginPosition, currentWaypoint.cellPosition) < navCellSize)
                    {
                        transform.position = beginPosition;
                        Turn(beginOrientation);
                        BecomeCalm();
                        return;
                    }
                    else
                        GoHome();//Никого в конце маршрута не оказалось, значит, возвращаемся домой
                }
                else
                {
                    ComplexNavigationCell nextWaypoint = (ComplexNavigationCell)waypoints[0];
                    NeighborCellStruct neighborConnection = currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb);
                    //Продолжаем следование
                    currentTarget = new ETarget(nextWaypoint.cellPosition);
                    if (nextWaypoint.cellType == NavCellTypeEnum.movPlatform && currentPlatform == null)
                    {
                        StopMoving();
                        waitingForPlatform = true;
                        FindPlatform(nextWaypoint.id);
                        platformConnectionType = currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb).connectionType;
                    }
                    else if (neighborConnection.connectionType == NavCellTypeEnum.jump && (grounded || onLadder))
                    {
                        //Перепрыгиваем препятствие
                        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x)));
                        //transform.position = new Vector3(currentWaypoint.cellPosition.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                        Jump();
                    }
                    else if (!onLadder ?
                        currentWaypoint.cellType == NavCellTypeEnum.ladder && nextWaypoint.cellType == NavCellTypeEnum.ladder && Mathf.Approximately(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x, 0f) : false)
                    {
                        LadderOn();
                    }
                    if (currentWaypoint.cellType == NavCellTypeEnum.ladder && (currentWaypoint.id != nextWaypoint.id))
                    {
                        LadderOff();
                        //Jump();
                    }
                    else if (currentWaypoint.cellType == NavCellTypeEnum.movPlatform && nextWaypoint.cellType != NavCellTypeEnum.movPlatform)
                        currentPlatform = null;
                }
            }
        }
        else
            GoHome();
    }

    public override void GoHome()
    {
        StopLadderMoving();
        base.GoHome();
    }

    #endregion //behaviourActions

    #region optimization

    /// <summary>
    /// Функция реализующая анализ окружающей персонажа обстановки, когда тот находится в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
        if (behavior != BehaviorEnum.calm)
            if (!followOptPath)
                StartCoroutine("PathPassOptProcess");
    }

    /// <summary>
    /// Функция, которая восстанавливает положение и состояние персонажа, пользуясь данными, полученными в оптимизированном режиме
    /// </summary>
    protected override void RestoreActivePosition()
    {
        if (!currentTarget.exists)
        {
            Turn(beginOrientation);
        }
        else
        {
            Vector2 pos = transform.position;
            if (platformTarget.exists)
            {
                transform.position = platformTarget + Vector3.up * 0.08f;
            }
            else if (onLadder)
            {
                string lLName = "ladder";
                Collider2D col = Physics2D.OverlapArea(pos + new Vector2(-minDistance / 2f, minDistance / 2f), pos + new Vector2(minDistance / 2f, -minDistance / 2f), LayerMask.GetMask(lLName));
                if (col == null)
                {
                    onLadder = false;
                }
                else
                {
                    transform.position = new Vector2(col.gameObject.transform.position.x, pos.y);
                    if (orientation == OrientationEnum.left)
                    {
                        Turn(OrientationEnum.right);
                    }
                    rigid.velocity = Vector3.zero;
                    rigid.gravityScale = 0f;
                }
            }
            if (!onLadder)
                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.x - pos.x)));
        }
    }

    /// <summary>
    /// Функция, которая переносит персонажа в ту позицию, в которой он может нормально функционировать для ведения оптимизированной версии 
    /// </summary>
    protected override void GetOptimizedPosition()
    {
        if (onLadder)
        {
            LadderOff();
            onLadder = true;
        }
        if (platformTarget.exists)
        {
            transform.SetParent(null);
            if (currentTarget == platformTarget)
            {
                if (waypoints != null ? (waypoints.Count > 0 ? ((ComplexNavigationCell)waypoints[0]).cellType == NavCellTypeEnum.movPlatform : false) : false)
                {
                    currentTarget = new ETarget(waypoints[0].cellPosition);
                    transform.position = waypoints[0].cellPosition;
                }
                else
                {
                    currentTarget.Exists = false;
                }
                CurrentPlatform = null;
            }
        }
        StopAvoid();
        if (!grounded && !onLadder)
        {
            if (waypoints == null)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, navMap.mapSize.magnitude, LayerMask.GetMask(gLName));
                if (!hit)
                {
                    Death();
                }
                else
                {
                    transform.position = hit.point + Vector2.up * 0.02f;
                }
            }
        }
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
                if (waypoints == null)
                {
                    if (beginPosition.transform == null)
                    {
                        //Если не получается добраться до начальной позиции, то считаем, что текущая позиция становится начальной
                        beginPosition = new ETarget(transform.position);
                        beginOrientation = orientation;
                        BecomeCalm();
                        followOptPath = false;
                    }
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
                    currentTarget.Exists = false;
                    pos = transform.position;
                    if (waypoints != null ? waypoints.Count > 0 : false)
                    {
                        ComplexNavigationCell currentCell = (ComplexNavigationCell)waypoints[0];
                        waypoints.RemoveAt(0);
                        if (waypoints.Count <= 0)
                            break;
                        ComplexNavigationCell nextCell = (ComplexNavigationCell)waypoints[0];
                        currentTarget = new ETarget(nextCell.cellPosition);

                        NeighborCellStruct neighborConnection = currentCell.GetNeighbor(nextCell.groupNumb, nextCell.cellNumb);
                        int timeCoof = 1;
                        if (currentCell.cellType == NavCellTypeEnum.ladder && (currentCell.id != nextCell.id))
                        {
                            onLadder = false;
                        }
                        if (nextCell.cellType == NavCellTypeEnum.movPlatform)
                        {
                            while (nextCell.cellType == NavCellTypeEnum.movPlatform && waypoints.Count > 0)
                            {
                                nextCell = (ComplexNavigationCell)waypoints[0];
                                waypoints.RemoveAt(0);
                                timeCoof += Mathf.FloorToInt(Mathf.Abs(Vector2.Distance(currentCell.cellPosition, nextCell.cellPosition)) / optSpeed);
                            }
                            if (timeCoof > 1)
                            {
                                currentTarget.Exists = false;
                            }
                        }
                        else if (!onLadder ?
                            currentCell.cellType == NavCellTypeEnum.ladder && nextCell.cellType == NavCellTypeEnum.ladder && Mathf.Approximately(nextCell.cellPosition.x - currentCell.cellPosition.x, 0f) : false)
                        {
                            onLadder = true;
                        }

                        if (neighborConnection.groupNumb != -1 || timeCoof > 1)
                        {
                            transform.position = nextCell.cellPosition;
                            yield return new WaitForSeconds(optTimeStep * timeCoof);
                            continue;
                        }
                    }
                }
                if (currentTarget.exists)
                {
                    targetPos = currentTarget;
                    Vector2 direction = targetPos - pos;
                    transform.position = pos + direction.normalized * Mathf.Clamp(optSpeed, 0f, direction.magnitude);
                }
                yield return new WaitForSeconds(optTimeStep);
            }
            waypoints = null;
            currentTarget.Exists = false;
            followOptPath = false;
        }
    }

    #endregion //optimization

    #region id

    public override EnemyData GetAIData()
    {
        return new HumanoidData(this);
    }

    /// <summary>
    /// Настроить персонажа в соответствии с загруженными данными
    /// </summary>
    public override void SetAIData(EnemyData eData)
    {
        if (eData != null ? !(eData is HumanoidData) : true)
            return;
        HumanoidData hData = (HumanoidData)eData;
        transform.position = hData.position;
        if (transform.localScale.x * hData.orientation < 0f)
            Turn((OrientationEnum)hData.orientation);

        string behaviorName = hData.behavior;
        switch (behaviorName)
        {
            case "calm":
                {
                    BecomeCalm();
                    break;
                }
            case "agressive":
                {
                    BecomeAgressive();
                    if (hData.waypoints.Count > 0)
                    {
                        waypoints = new List<NavigationCell>();
                        for (int i = 0; i < hData.waypoints.Count; i++)
                            waypoints.Add(navMap.GetCurrentCell(hData.waypoints[i]));
                        transform.position = waypoints[0].cellPosition;
                        ComplexNavigationCell nextCell = (ComplexNavigationCell)waypoints[0];
                        if (nextCell.cellType == NavCellTypeEnum.jump)
                            transform.position = nextCell.cellPosition;
                        else if (hData.onLadder)
                            LadderOn();
                        else if (hData.platformId != -1)
                        {
                            FindPlatform(hData.platformId);
                            transform.position = platformTarget + Vector3.up * 0.08f;
                        }
                    }
                    break;
                }
            case "patrol":
                {
                    BecomePatrolling();
                    if (hData.waypoints.Count > 0)
                    {
                        waypoints = new List<NavigationCell>();
                        for (int i = 0; i < hData.waypoints.Count; i++)
                            waypoints.Add(navMap.GetCurrentCell(hData.waypoints[i]));
                        transform.position = waypoints[0].cellPosition;
                        ComplexNavigationCell nextCell = (ComplexNavigationCell)waypoints[0];
                        if (nextCell.cellType == NavCellTypeEnum.jump)
                            transform.position = nextCell.cellPosition;
                        else if (hData.onLadder)
                            LadderOn();
                        else if (hData.platformId != -1)
                        {
                            FindPlatform(hData.platformId);
                            transform.position = platformTarget + Vector3.up*0.08f;
                        }    
                    }
                    break;
                }
            default:
                {
                    behavior = BehaviorEnum.calm;
                    BecomeCalm();
                    break;
                }
        }

        TargetData currentTargetData = hData.currentTargetData;
        TargetData mainTargetData = hData.mainTargetData;

        if (currentTargetData.targetName != string.Empty)
            currentTarget = new ETarget(GameObject.Find(currentTargetData.targetName).transform);
        else
            currentTarget = new ETarget(currentTargetData.position);

        if (mainTargetData.targetName != string.Empty)
            MainTarget = new ETarget(GameObject.Find(mainTargetData.targetName).transform);
        else
            mainTarget = new ETarget(mainTargetData.position);
        if (behavior != BehaviorEnum.agressive)
            TargetCharacter = null;

        SetBuffs(eData.bListData);
        Health = hData.health;
    }

    #endregion //id

}