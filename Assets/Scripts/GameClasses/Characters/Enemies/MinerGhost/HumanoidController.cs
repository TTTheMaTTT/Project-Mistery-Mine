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

    protected override  List<NavigationCell> Waypoints
    {
        get
        {
            return waypoints;
        }
        set
        {
            waypoints = value;
            if (value != null)
            {
                if (currentTarget != null)
                {
                    if (currentTarget != mainTarget && currentTarget != platformObject)
                        Destroy(currentTarget);
                    currentTarget = null;
                }
            }
            else
            {
                StopAvoid();
                LadderOff();
            }
            if (currentPlatform != null)
                CurrentPlatform = null;
            jumping = false;
        }
    }

    protected HeroController hero;//Герой, за которым следит гуманоид
    protected virtual GameObject MainTarget { get { return mainTarget; } set { mainTarget = value; if (hero == null) { hero = SpecialFunctions.player.GetComponent<HeroController>(); } } }

    #endregion //fields

    #region parametres

    [SerializeField]protected float ladderSpeed = 0.8f;//Скорость передвижения по лестнице
    protected bool onLadder = false;//Находится ли монстр на лестнице
    protected bool grounded { get { return groundCheck ? groundCheck.WallInFront() : true; } }//Находится ли монстр на земле

    #region platform

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
            platformObject = value != null ? currentPlatform.gameObject : null;
            platformTransform = platformObject != null ? platformObject.transform : null;
        }
    }
    protected GameObject platformObject;
    protected Transform platformTransform;
    protected bool waitingForPlatform = false;//Ждёт ли персонаж выдвижения движущейся платформы?
    protected NavCellTypeEnum platformConnectionType;//Каким образом надо добираться до плафтормы

    #endregion //platform

    protected override float attackDistance{get{return .12f;}}

    #endregion //parametres

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        Animate(new AnimationEventArgs("groundMove"));
        Analyse();
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            GoToThePoint(SpecialFunctions.player.transform.position);
        }
        Analyse();
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        Transform indicators = transform.FindChild("Indicators");
        if (indicators != null)
        {
            wallCheck = indicators.FindChild("WallCheck").GetComponent<WallChecker>();
            precipiceCheck = indicators.FindChild("PrecipiceCheck").GetComponent<WallChecker>();
            groundCheck = indicators.FindChild("GroundCheck").GetComponent<WallChecker>();

            hearing = indicators.GetComponentInChildren<Hearing>();
            if (hearing!=null)
                hearing.hearingEventHandler += HandleHearingEvent;
        }

        base.Initialize();

        if (areaTrigger!=null)
        {
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            if (hearing!=null)
            {
                areaTrigger.triggerFunctionIn += EnableHearing;
                areaTrigger.triggerFunctionOut += DisableHearing;
            }
        }

    }

    /// <summary>
    /// Передвижение
    /// </summary>
    /// <param name="_orientation">Направление движения (влево/вправо)</param>
    protected override void Move(OrientationEnum _orientation)
    {
        bool wallInFront = wallCheck.WallInFront();
        Vector2 targetVelocity =  wallInFront? new Vector2(0f, rigid.velocity.y) : new Vector2((int)orientation * speed, rigid.velocity.y);
        rigid.velocity = wallInFront? Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration):targetVelocity;

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Взобраться на лестницу
    /// </summary>
    protected override void LadderOn()
    {
        if (waypoints != null ? waypoints[0].cellType == NavCellTypeEnum.ladder : false)
        {
            string lLName = "ladder";
            Collider2D col = Physics2D.OverlapArea(waypoints[0].cellPosition + new Vector2(-minDistance / 2f, minDistance / 2f), waypoints[0].cellPosition + new Vector2(minDistance / 2f, -minDistance / 2f), LayerMask.GetMask(lLName));
            if (col == null)
                return;
            onLadder = true;
            //Animate(new AnimationEventArgs("setLadderMove", "", 1));
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
        //Animate(new AnimationEventArgs("setLadderMove", "", 0));
        base.LadderOff();
    }

    /// <summary>
    /// Перемещение по лестнице
    /// </summary>
    protected override void LadderMove(float direction)
    {
        rigid.velocity = new Vector2(0f, direction * ladderSpeed);
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
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        Animate(new AnimationEventArgs("attack"));
        StartCoroutine(AttackProcess());
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        employment = Mathf.Clamp(employment - 5, 0, maxEmployment);
        yield return new WaitForSeconds(preAttackTime);
        hitBox.SetHitBox(new HitClass(damage, attackTime, attackSize, attackPosition, hitForce));
        yield return new WaitForSeconds(attackTime);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Провести анализ окружающей обстановки
    /// </summary>
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
                    {
                        //Если враг ушёл достаточно далеко
                        if (direction.magnitude > sightRadius*0.75f)
                        {
                            GoToThePoint(mainTarget.transform.position);
                            if (behaviour == BehaviourEnum.agressive)
                            {
                                GoHome();
                                break;
                            }
                            else
                                StartCoroutine(BecomeCalmProcess());
                        }
                    }
                    if (currentTarget == null)
                        break;
                    if (!hit?
                        ((transform.position - prevPosition).sqrMagnitude < speed * Time.fixedDeltaTime / 10f && currentTarget != mainTarget && 
                        (currentPlatform==null? true :(!waitingForPlatform && transform.parent==null))): true)
                    {
                        if (!avoid)
                            StartCoroutine(AvoidProcess());
                    }

                    if (waitingForPlatform)
                    {
                        if (WatchPlatform())
                        {
                            waitingForPlatform = false;
                            if (currentTarget != mainTarget && currentTarget != platformObject)
                                Destroy(currentTarget);
                            currentTarget = platformObject;
                            if (platformConnectionType == NavCellTypeEnum.jump)
                            {
                                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign((currentTarget.transform.position - transform.position).x)));
                                Vector3 pos = transform.position;
                                //transform.position = new Vector3(currentTarget.transform.position.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                                Jump();
                            }
                        }
                    }

                    break;
                }
            case BehaviourEnum.patrol:
                {
                    if (currentTarget == null)
                        break;
                    if ((transform.position - prevPosition).sqrMagnitude < speed * Time.fixedDeltaTime / 10f && !avoid && 
                        (currentPlatform == null ? true : (!waitingForPlatform && transform.parent == null)))
                    {
                        StartCoroutine(AvoidProcess());
                    }
                    Vector2 direction = Vector3.right * (int)orientation;
                    if (mainTarget!=null)
                    {
                        if (Vector2.SqrMagnitude(mainTarget.transform.position - transform.position) < minCellSqrMagnitude)
                            BecomeAgressive();
                    }
                    RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + sightOffset * direction, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (hit.collider.gameObject.CompareTag("player"))
                        {
                            BecomeAgressive();
                        }
                    }

                    if (waitingForPlatform)
                    {
                        if (WatchPlatform())
                        {
                            waitingForPlatform = false;
                            if (currentTarget!=mainTarget && currentTarget!=platformObject)
                                Destroy(currentTarget);
                            currentTarget = platformObject;
                            if (platformConnectionType==NavCellTypeEnum.jump)
                            {
                                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign((currentTarget.transform.position - transform.position).x)));
                                Vector3 pos = transform.position;
                                //transform.position = new Vector3(currentTarget.transform.position.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                                Jump();
                            }
                        }
                    }

                    break;
                }

            case BehaviourEnum.calm:
                {
                    Vector2 direction = Vector3.right * (int)orientation;
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
    /// Определить, нужно ли искать отдельный маршрут до главной цели
    /// </summary>
    /// <returns>Возвращает факт необходимости поиска пути</returns>
    protected override bool NeedToFindPath()
    {
        Vector3 mainPos = mainTarget.transform.position;
        bool onPlatform=false;
        bool changePosition = (grounded || onLadder) && Vector2.SqrMagnitude((Vector2)mainPos - prevTargetPosition) > minCellSqrMagnitude * 16f;
        if (changePosition)
        {
            NavigationCell cell1 = navMap.GetCurrentCell(mainPos), cell2 = navMap.GetCurrentCell(prevTargetPosition);
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
        if (currentPlatform == null || currentTarget == null)
            return false;
        Vector2 platformPos = platformTransform.position;
        Vector2 platformDirection = currentPlatform.Direction;
        Vector2 pos = transform.position;
        if (pos.y < platformPos.y && Vector2.Dot(pos - platformPos, platformDirection) < 0f)
            return false;
        return (Vector2.SqrMagnitude((Vector2)platformTransform.position - (Vector2)currentTarget.transform.position+currentPlatform.Direction*platformOffset) < navCellSize*navCellSize);
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
    public override void TakeDamage(float damage)
    {
        if (onLadder)
            LadderOff();//Сбросить с лестницы
        base.TakeDamage(damage);
        if (behaviour!=BehaviourEnum.agressive)
            BecomeAgressive();
    }

    /// <summary>
    /// Обновить информацию, важную для моделей поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        if (currentTarget != null && currentTarget != mainTarget && currentTarget != platformObject)
            Destroy(currentTarget);
        jumping = false;
        StopAvoid();
        StopCoroutine(BecomeCalmProcess());
        prevTargetPosition = EVector3.zero;
    }

    /// <summary>
    /// Перейти в спокойное состояние
    /// </summary>
    protected override void BecomeCalm()
    {
        RefreshTargets();
        behaviour = BehaviourEnum.calm;
        mainTarget = null;
        currentTarget = null;
        Waypoints = null;
        behaviourActions = CalmBehaviour;
    }

    /// <summary>
    /// Перейти в агрессивное состояние
    /// </summary>
    protected override void BecomeAgressive()
    {
        RefreshTargets();
        behaviour = BehaviourEnum.agressive;
        MainTarget = SpecialFunctions.player;
        waypoints = null;
        if (currentPlatform != null)
            CurrentPlatform = null;
        currentTarget = mainTarget;
        behaviourActions = AgressiveBehaviour;
        //wallCheck.RemoveWallType("character");
    }

    /// <summary>
    /// Перейти в состояние патрулирования
    /// </summary>
    protected override void BecomePatrolling()
    {
        RefreshTargets();
        behaviour = BehaviourEnum.patrol;
        //mainTarget = null;
        currentTarget = null;
        behaviourActions = PatrolBehaviour;
        CurrentPlatform = null;
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
        else if (behaviour != BehaviourEnum.calm)
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
        if (currentTarget != null && currentTarget != mainTarget &&
            (transform.position - _prevPos).sqrMagnitude < speed * Time.fixedDeltaTime / 10f &&
            (currentPlatform!=null?(!waitingForPlatform && transform.parent!=platformObject):true))
        {
            if (currentTarget != platformObject)
                transform.position += (currentTarget.transform.position - transform.position).normalized * navCellSize;
            yield return new WaitForSeconds(avoidTime);
            //Если не получается обойти ставшее на пути препятствие
            if (currentTarget != null && currentTarget != mainTarget &&
                (transform.position - _prevPos).sqrMagnitude < speed * Time.fixedDeltaTime / 10f &&
                (currentPlatform != null ? (!waitingForPlatform && transform.parent != platformObject) : true))
            {
                if (mainTarget != null)
                {
                    if (behaviour==BehaviourEnum.agressive)
                    { 
                        Waypoints = FindPath(mainTarget.transform.position, maxAgressivePathDepth);
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
                if (behaviour == BehaviourEnum.patrol)
                    StartCoroutine(ResetStartPositionProcess(transform.position));

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
        if (Vector2.SqrMagnitude(prevPosition - (Vector2)transform.position) < minCellSqrMagnitude && behaviour == BehaviourEnum.patrol)
        {
            if ((currentPlatform != null ? (!waitingForPlatform && transform.parent != platformObject) : true))
            {
                beginPosition = transform.position;
                beginOrientation = orientation;
                BecomeCalm();
            }
        }
    }

    #region behaviourActions

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehaviour()
    {
        base.AgressiveBehaviour();

        if (mainTarget != null && employment > 2)
        {

            Vector3 targetPosition;
            Vector3 targetDistance;
            Vector3 pos = transform.position;
            Vector3 mainPos = mainTarget.transform.position;
            if (waypoints == null)
            {

                #region directWay

                targetPosition = mainTarget.transform.position;
                targetDistance = targetPosition - pos;
                if (Vector2.SqrMagnitude(targetDistance) > attackDistance*attackDistance)
                {
                    if (!wallCheck.WallInFront() && (precipiceCheck.WallInFront()||!grounded) && (Mathf.Abs((pos - mainPos).y) < navCellSize * 5f? true :!hero.OnLadder))
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
                        if (Vector2.SqrMagnitude(transform.position - mainPos) > minCellSqrMagnitude * 16f &&
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

                #endregion //directWay

            }
            else
            {

                #region complexWay

                if (NeedToFindPath())//Если главная цель сменила своё местоположение
                {
                    Waypoints = FindPath(mainTarget.transform.position, maxAgressivePathDepth);
                    //prevTargetPosition = new EVector3(mainTarget.transform.position, true);
                    return;
                }

                if (waypoints.Count > 0)
                {
                    if (currentTarget == null)
                    {
                        currentTarget = new GameObject("MonsterTarget");
                        currentTarget.transform.position = waypoints[0].cellPosition;
                        if (waypoints[0].cellType == NavCellTypeEnum.movPlatform)
                            FindPlatform(waypoints[0].id);
                    }

                    bool waypointIsAchieved = false;
                    targetPosition = currentTarget.transform.position;
                    targetDistance = targetPosition - pos;
                    if (!waitingForPlatform && (transform.parent != platformTransform || currentPlatform == null))
                    {
                        if (onLadder)
                        {
                            LadderMove(Mathf.Sign(targetDistance.y));
                            waypointIsAchieved = Mathf.Abs(currentTarget.transform.position.y - pos.y) < navCellSize / 2f;
                        }
                        else if (currentPlatform != null ? !grounded : true)
                        {
                            if (Mathf.Abs(targetDistance.x) > navCellSize / 4f)
                                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                            else
                            {
                                transform.position = new Vector3(targetPosition.x, pos.y, pos.z);
                                StopMoving();
                            }
                            waypointIsAchieved = Vector3.SqrMagnitude(currentTarget.transform.position - transform.position) < minCellSqrMagnitude;
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
                            if (currentTarget == platformObject && transform.parent == platformTransform)
                            {
                                transform.position = platformTransform.position + Vector3.up * 0.08f;
                                waypointIsAchieved = true;
                            }
                        }
                    }


                    if (waypointIsAchieved)
                    {
                        NavigationCell currentWaypoint = waypoints[0];
                        if (currentTarget==platformObject)
                        {
                            if (waypoints.Count > 1 ? (waypoints[1].cellPosition - (Vector2)transform.position).x * (int)orientation < 0f:false)
                                Turn();
                            transform.position = platformTransform.position + Vector3.up * .09f;
                        }
                        else 
                            Destroy(currentTarget);

                        waypoints.RemoveAt(0);

                        if (waypoints.Count > 2? !hero.OnLadder :false)//Проверить, не стал ли путь до главной цели прямым и простым, чтобы не следовать ему больше
                        {
                            bool directPath = true;
                            Vector2 cellsDirection = (waypoints[1].cellPosition - waypoints[0].cellPosition).normalized;
                            NavCellTypeEnum cellsType = waypoints[0].cellType;
                            for (int i = 2; i < waypoints.Count; i++)
                            {
                                if (Vector2.Angle((waypoints[i].cellPosition - waypoints[i - 1].cellPosition), cellsDirection) > minAngle || waypoints[i - 1].cellType != cellsType)
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
                            if (!hero.OnLadder)
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
                            NavigationCell nextWaypoint = waypoints[0];
                            NeighborCellStruct neighborConnection = currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb);
                            //Продолжаем следование
                            currentTarget = new GameObject("MonsterTarget");
                            currentTarget.transform.position = nextWaypoint.cellPosition;
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
    protected override void PatrolBehaviour()
    {
        Vector3 pos = transform.position;
        if (waypoints != null ? waypoints.Count > 0 : false)
        {
            if (currentTarget == null)
            {
                currentTarget = new GameObject("MonsterTarget");
                currentTarget.transform.position = waypoints[0].cellPosition;
                if (waypoints[0].cellType == NavCellTypeEnum.movPlatform)
                    FindPlatform(waypoints[0].id);
            }

            bool waypointIsAchieved = false;
            Vector3 targetPosition = currentTarget.transform.position;
            Vector3 targetDistance = targetPosition - pos;
            if (!waitingForPlatform && (transform.parent != platformTransform || currentPlatform == null))
            {
                if (onLadder)
                {
                    LadderMove(Mathf.Sign(targetDistance.y));
                    waypointIsAchieved = Mathf.Abs(currentTarget.transform.position.y - pos.y) < navCellSize/2f;
                }
                else if (currentPlatform != null ? !grounded : true)
                {
                    if (Mathf.Abs(targetDistance.x) > navCellSize / 4f)
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                    else
                    {
                        transform.position = new Vector3(targetPosition.x, pos.y, pos.z);
                        StopMoving();
                    }
                    waypointIsAchieved = Vector3.SqrMagnitude(currentTarget.transform.position - pos) < minCellSqrMagnitude;
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
                    if (currentTarget == platformObject && transform.parent == platformTransform)
                    {
                        transform.position = platformTransform.position + Vector3.up * 0.08f;
                        waypointIsAchieved = true;
                    }
                }
            }

            if (waypointIsAchieved)
            {
                NavigationCell currentWaypoint = waypoints[0];
                if (currentTarget == platformObject)
                {
                    if (waypoints.Count > 1 ? (waypoints[1].cellPosition - (Vector2)pos).x * (int)orientation < 0f : false)
                        Turn();
                    transform.position = platformTransform.position + Vector3.up * .09f;
                }
                else
                    Destroy(currentTarget);

                waypoints.RemoveAt(0);

                if (waypoints.Count == 0)//Если маршрут кончился, перестать следовать ему
                {
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
                    NavigationCell nextWaypoint = waypoints[0];
                    NeighborCellStruct neighborConnection = currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb);
                    //Продолжаем следование
                    currentTarget = new GameObject("MonsterTarget");
                    currentTarget.transform.position = nextWaypoint.cellPosition;
                    if (nextWaypoint.cellType == NavCellTypeEnum.movPlatform && currentPlatform == null)
                    {
                        StopMoving();
                        waitingForPlatform = true;
                        FindPlatform(nextWaypoint.id);
                        platformConnectionType = currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb).connectionType;
                    }
                    else if (neighborConnection.connectionType == NavCellTypeEnum.jump && (grounded||onLadder))
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
                    if (currentWaypoint.cellType == NavCellTypeEnum.ladder && (currentWaypoint.id!=nextWaypoint.id ))
                    {
                        LadderOff();
                        //Jump();
                    }
                    else if (currentWaypoint.cellType == NavCellTypeEnum.movPlatform && nextWaypoint.cellType != NavCellTypeEnum.movPlatform)
                        currentPlatform = null;
                }
            }
        }
    }

    #endregion //behaviourActions

    #region optimization

    /// <summary>
    /// Включить слух
    /// </summary>
    protected override void EnableHearing()
    {
        hearing.gameObject.SetActive(true);
    }

    /// <summary>
    /// Выключить слух
    /// </summary>
    protected override void DisableHearing()
    {
        hearing.gameObject.SetActive(false);
    }

    #endregion //optimization

}