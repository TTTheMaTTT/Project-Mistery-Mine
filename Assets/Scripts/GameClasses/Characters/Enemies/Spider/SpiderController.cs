using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер паука
/// </summary>
public class SpiderController : AIController
{

    public enum SpiderStartPositionEnum { usual=0, behindWall=1, underFloor=2}//Енам, характеризующий изначальное положение паука

    #region consts

    protected const float spiderOffset = .026f;//Насколько должен быть смещён паук относительно поверхности земли

    protected const float jumpAttackMaxDistance = .65f, jumpAttackMinDistance = .3f;//Расстояние до цели, для совершения атаки в прыжке 

    #endregion //consts

    #region fields

    protected WallChecker wallCheck, precipiceCheck;
    //protected SightFrustum sight;//Зрение персонажа
    //protected HitBoxController selfHitBox;//Хитбокс, который атакует персонажа при соприкосновении с пауком. Этот хитбокс всегда активен и не перемещается

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
                gameObject.layer = LayerMask.NameToLayer("characterWithoutPlatform");
            }
            else
            {
                StopAvoid();
                if (mainTarget.exists && behavior == BehaviorEnum.agressive)
                {
                    currentTarget = mainTarget;
                }
                gameObject.layer = LayerMask.NameToLayer(loyalty == LoyaltyEnum.ally ? "hero" : "character");
            }
        }
    }

    #endregion //fields

    #region parametres

    protected Vector2 spiderOrientation = new Vector2(0f, 1f);//нормаль поверхности, на которой стоит паук
    protected virtual Vector2 SpiderOrientation
    {
        get
        {
            return spiderOrientation;
        }
        set
        {
            spiderOrientation = value;
            movementDirection = GetNormal(spiderOrientation);
            /*if (sight != null ? sight.enabled : false)
            {
                if (spiderOrientation.y < 0 && Mathf.Abs(spiderOrientation.y) >= Mathf.Abs(spiderOrientation.x))
                {
                    sight.RotateLocal(90f);
                    //canStayOnPlatform = false;//Нет никакого взаимодействия с платформами
                }
                else
                {
                    //canStayOnPlatform = spiderOrientation.y >= Mathf.Abs(spiderOrientation.x);
                    sight.RotateLocal(0f);
                }
            }*/

            //На какой угол надо повернуть паука
            float angle = Vector2.Angle(Vector2.up, spiderOrientation) * Mathf.Sign(-spiderOrientation.x);

            transform.eulerAngles = new Vector3(0f, 0f, angle);//Повернём паука
            if (spiderOrientation.y < 0 || Mathf.Abs(spiderOrientation.x) > Mathf.Abs(spiderOrientation.y))
                rigid.gravityScale = 0f;
            else
                rigid.gravityScale = 1f;
            wallCheck.SetPosition(angle / 180f * Mathf.PI, (int)orientation);
            precipiceCheck.SetPosition(angle / 180f * Mathf.PI, (int)orientation);
        }
    }
    protected Vector2 movementDirection = Vector2.right;//В какую сторону движется паук, если он повёрнут вправо

    protected override float attackDistance { get { return .12f; } }//На каком расстоянии должен стоять ИИ, чтобы решить атаковать
    [SerializeField]protected Vector2 jumpAttackForce;//Сила отталкивания при прыжковой атаке
    [SerializeField]protected HitParametres jumpAttackParametres;//Параметры атаки в прыжке
    [SerializeField]protected float jumpAttackCooldown = 10f;//Кулдаун атаки в прыжке
    protected bool jumpAttackIsPossible = true;//может ли паук совершить атаку в прыжке?

    public override LoyaltyEnum Loyalty
    {
        get
        {
            return base.Loyalty;
        }

        set
        {
            bool noPlatform = (LayerMask.LayerToName(gameObject.layer) == "characterWithoutPlatform");
            base.Loyalty = value;
            /*if (selfHitBox != null)
            {
                selfHitBox.allyHitBox = (value == LoyaltyEnum.ally);
                selfHitBox.SetEnemies(enemies);
            }*/
            if (noPlatform)
                gameObject.layer = LayerMask.NameToLayer("characterWithoutPlatform");
            if (value == LoyaltyEnum.ally)
                wallCheck.WhatIsWall.Remove("character");
            else if (!wallCheck.WhatIsWall.Contains("character"))
                wallCheck.WhatIsWall.Add("character");
        }
    }

    public float patrolDistance = 2f;//По таким дистанциям паук будет патрулировать
    public float appearTime = 0f;//Сколько времени должно пройти, прежде чем паук вылезет из своей норы
    public SpiderStartPositionEnum spiderStartPosition = SpiderStartPositionEnum.usual;//Изначальное положение паука
    protected bool inGround = false;//Прячтся ли паук под землёй в данный момент?
    public bool InGround { get { return inGround; } }

    //protected bool calmDown = false;//Успокаивается ли персонаж
    //[SerializeField]protected bool neutral = true;//Является ли паук изначально нейтральным

    #endregion //parametres

    protected override void FixedUpdate()
    {
        if (!immobile)
        {
            base.FixedUpdate();
            Animate(new AnimationEventArgs("groundMove"));
        }
    }

    /*protected override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.B))
        {
            HearBattleCry(SpecialFunctions.player.transform.position);
        }
    }*/

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


            /*Transform sightParent = indicators.FindChild("Sight");
            sight = sightParent!=null? sightParent.GetComponentInChildren<SightFrustum>():null;
            if (sight != null)
            {
                sight.sightInEventHandler += HandleSightInEvent;
                sight.sightOutEventHandler += HandleSightOutEvent;
                sight.enabled = !neutral;
                if (!neutral)
                    sight.RotateLocal((spiderOrientation.y >= 0 && Mathf.Abs(spiderOrientation.x) - Mathf.Abs(spiderOrientation.y) < 0) ? 0f : 90f);
            }*/

        }

        base.Initialize();

        /*selfHitBox = transform.FindChild("SelfHitBox").GetComponent<HitBoxController>();
        if (selfHitBox != null)
        {
            selfHitBox.SetEnemies(enemies);
            selfHitBox.SetHitBox(attackParametres.damage, -1f, 0f,attackParametres.damageType);
            //selfHitBox.Immobile = true;//На всякий случай
            selfHitBox.AttackEventHandler += HandleAttackProcess;
        }*/

        if (areaTrigger != null)
        {
            /*if (selfHitBox != null)
            {
                areaTrigger.triggerFunctionIn += EnableSelfHitBox;
                areaTrigger.triggerFunctionOut += DisableSelfHitBox;
            }*/
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            /*if (sight != null)
            {
                areaTrigger.triggerFunctionIn += EnableSight;
                areaTrigger.triggerFunctionOut += DisableSight;
            }*/
            areaTrigger.InitializeAreaTrigger();
        }

        if (spiderStartPosition != SpiderStartPositionEnum.usual)
        {
            immobile = true;
            rigid.isKinematic = true;
            DisableColliders();
            Animate(new AnimationEventArgs("setInGround", spiderStartPosition == SpiderStartPositionEnum.behindWall ? "right" : "down", 0));
            inGround = true;
        }
        BecomeCalm();
    }

    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        storyActionBase.Add("moveOut", MoveOutAction);
    }

    #region movement

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = wallCheck.WallInFront ? new Vector2(0f, rigid.velocity.y) : 
                                                        (rigid.gravityScale == 0f ? movementDirection * (int)orientation * speed : new Vector2((int)orientation * speed, rigid.velocity.y));
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Остановить перемещение
    /// </summary>
    protected override void StopMoving()
    {
        Vector2 projection = Vector2.Dot(rigid.velocity, movementDirection) * movementDirection;
        rigid.velocity -= projection;
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    protected override void Turn()
    {
        base.Turn();
        wallCheck.SetPosition(transform.eulerAngles.z/180f*Mathf.PI, (int)orientation);
        precipiceCheck.SetPosition(transform.eulerAngles.z / 180f * Mathf.PI, (int)orientation);
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    /// <param name="_orientation">В какую сторону должен смотреть персонаж</param>
    public override void Turn(OrientationEnum _orientation)
    {
        base.Turn(_orientation);
        wallCheck.SetPosition(transform.eulerAngles.z / 180f * Mathf.PI, (int)orientation);
        precipiceCheck.SetPosition(transform.eulerAngles.z / 180f * Mathf.PI, (int)orientation);
    }

    /*
    /// <summary>
    /// Процесс обхода препятствия
    /// </summary>
    protected override IEnumerator AvoidProcess()
    {
        avoid = true;
        EVector3 _prevPos = prevPosition;
        yield return new WaitForSeconds(avoidTime);
        if (currentTarget != null && currentTarget!=mainTarget && (transform.position - _prevPos).sqrMagnitude < speed * Time.fixedDeltaTime / 10f)
        {
            transform.position += (currentTarget.transform.position - transform.position).normalized * navCellSize;
        }
        avoid = false;

    }

    protected override void StopAvoid()
    {
        StopCoroutine("AvoidProcess");
        avoid = false;
    }
    */

    /// <summary>
    /// Определить следующую точку патрулирования
    /// </summary>
    protected virtual void Patrol()
    {
        Vector2 waypoint = new Vector3((int)orientation * patrolDistance, 0f, 0f) + transform.position;
        currentTarget = new ETarget(waypoint);

    }

    /// <summary>
    /// Сменить ориентацию паука и "прикрепить" его к  заданной поверхности
    /// </summary>
    /// <param name="targetCollider">коллайдер, к которому прикрепляется паук</param>
    protected void ChangeOrientation(Collider2D targetCollider)
    {

        Vector2[] colPoints = GetColliderPoints(targetCollider);

        if (colPoints.Length <= 0)
            return;

        Vector2 connectionPoint = Vector2.zero;

        //Найдём ту сторону коллайдера, которая имеет нормаль, отличную от текущей ориентации паука, и расстояние до которой от текущего положения паука является наименьшим
        float mDistance = Mathf.Infinity;
        int pointIndex = -1;
        for (int i = 0; i < colPoints.Length; i++)
        {
            Vector2 point1 = colPoints[i];
            Vector2 point2 = i < colPoints.Length - 1 ? colPoints[i + 1] : colPoints[0];
            Vector2 normal = GetNormal(point1, point2, targetCollider);
            if (Mathf.Abs(Vector2.Angle(spiderOrientation, normal)) >= minAngle)
            {
                Vector2 _connectionPoint = GetConnectionPoint(point1, point2, transform.position);
                float newDistance = Vector2.SqrMagnitude(_connectionPoint - (Vector2)transform.position);
                if (newDistance < mDistance)
                {
                    connectionPoint = _connectionPoint;
                    mDistance = newDistance;
                    pointIndex = i;
                }
            }
        }

        if (pointIndex < 0)
            return;

        Vector2 surfacePoint1 = colPoints[pointIndex], surfacePoint2 = colPoints[pointIndex < colPoints.Length - 1 ? pointIndex + 1 : 0];
        Vector2 _spiderOrientation = GetNormal(surfacePoint1, surfacePoint2, targetCollider);

        SpiderOrientation = _spiderOrientation;

        transform.position = connectionPoint + spiderOffset * spiderOrientation;//Расположить паука
        StopMoving();
    }

    /// <summary>
    /// Сменить ориентацию паука и "прикрепить" его к  заданной поверхности
    /// </summary>
    /// <param name="surfacePoint1">Первая точка поверхности</param>
    /// <param name="surfacePoint2">Вторая точка поверхности</param>
    /// <param name="connectionPoint">Точка крепления</param>
    /// <param name="_col">Коллайдер, к которому крепимся</param>
    protected void ChangeOrientation(Vector2 surfacePoint1, Vector2 surfacePoint2, Vector2 connectionPoint, Collider2D _col)
    {
        Vector2 _spiderOrientation = GetNormal(surfacePoint1, surfacePoint2, _col);

        //На какой угол надо повернуть паука
        float angle = Vector2.Angle(spiderOrientation, _spiderOrientation) * Mathf.Sign(spiderOrientation.x * _spiderOrientation.y - spiderOrientation.y * _spiderOrientation.x);

        if (Mathf.Abs(angle) < minAngle)
            return;

        SpiderOrientation = _spiderOrientation;
        transform.position = connectionPoint + spiderOffset * spiderOrientation;//Расположить паука
        StopMoving();
    }

    /// <summary>
    /// Спрыгнуть вниз
    /// </summary>
    protected virtual void JumpDown()
    {
        rigid.gravityScale = 1f;
        SpiderOrientation = Vector2.up;
        movementDirection = Vector2.right;
        transform.eulerAngles = Vector3.zero;
    }

    /// <summary>
    /// Совершить прыжок
    /// </summary>
    protected override void Jump()
    {
        SpiderOrientation = Vector2.up;
        rigid.velocity = new Vector3(rigid.velocity.x, 0f, 0f);
        rigid.AddForce(new Vector2(jumpForce * 0.5f, jumpForce));
        //rigid.velocity = new Vector2((int)orientation * speed, rigid.velocity.y);//Сразу придать персонажу максимальную горизонтальную скорость для преодоления препятствия
    }

    #endregion //movement

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
                    Vector2 direction = mainTarget- pos;
                    RaycastHit2D hit = Physics2D.Raycast(pos, direction.normalized, direction.magnitude, LayerMask.GetMask(gLName));
                    if (hit)
                    {
                        if (direction.magnitude > sightOffset / 2f)
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
                    if ((pos - prevPosition).sqrMagnitude < speed * Time.fixedDeltaTime / 10f && !avoid && currentTarget != mainTarget)
                    {
                        StartCoroutine("AvoidProcess");
                        if (!jumping && !precipiceCheck.WallInFront)
                            StartCoroutine("WrongOrientationProcess");
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
                        if ((pos - prevPosition).sqrMagnitude < speed * Time.fixedDeltaTime / 10f)
                        {
                            StartCoroutine("AvoidProcess");
                            if (!jumping && !precipiceCheck.WallInFront)
                                StartCoroutine("WrongOrientationProcess");
                        }
                    }
                    Vector2 direction = spiderOrientation.y < -1 * Mathf.Abs(spiderOrientation.x) ? Vector2.down : movementDirection * (int)orientation;
                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * direction, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (enemies.Contains(hit.collider.gameObject.tag))
                        {
                            MainTarget = new ETarget(hit.collider.transform);
                            BecomeAgressive();
                        }
                    }

                    if (loyalty == LoyaltyEnum.ally ? !mainTarget.exists && !jumping : false) //Если нет основной цели и стоящий на земле паук - союзник героя, то он следует к нему
                    {
                        float sqDistance = Vector2.SqrMagnitude(beginPosition - pos);
                        if (sqDistance > allyDistance * 1.2f && followAlly)
                        {
                            if (Vector2.SqrMagnitude(beginPosition - (Vector2)prevTargetPosition) > minCellSqrMagnitude)
                            {
                                prevTargetPosition = new EVector3(pos);//Динамическое преследование героя-союзника
                                Waypoints = FindPath(beginPosition, maxAgressivePathDepth * 3);
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
                    Vector2 direction = spiderOrientation.y < -1 * Mathf.Abs(spiderOrientation.x) ? Vector2.down : movementDirection * (int)orientation;
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
                                Waypoints = FindPath(beginPosition, maxAgressivePathDepth * 3);
                                if (waypoints != null)
                                    BecomePatrolling();
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

        prevPosition = new EVector3(pos, true);
    }

    /// <summary>
    /// Определить, нужно ли отыскивать путь до главной цели
    /// </summary>
    /// <returns>Есть ли необходимость отыскания пути</returns>
    protected override bool NeedToFindPath()
    {
        return Vector2.SqrMagnitude(mainTarget - (Vector2)prevTargetPosition) > navCellSize * navCellSize;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, int attackPower = 0)
    {
        base.TakeDamage(damage, _dType, attackPower);
        bool stunned = GetBuff("StunnedProcess") != null;
        if (attackPower > balance || stunned)
        {
            JumpDown();//Сбросить паука со стены ударом
        }
    }

    /// <summary>
    /// Совершить атаку в прыжке
    /// </summary>
    protected virtual void JumpAttack()
    {
        balance = attackBalance;
        rigid.AddForce(new Vector2(jumpAttackForce.x*(int)orientation,jumpAttackForce.y));
        Attack();
        StartCoroutine(JumpAttackCooldownProcess());
    }

    /// <summary>
    /// Процесс кулдауна атаки в прыжке
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator JumpAttackCooldownProcess()
    {
        jumpAttackIsPossible = false;
        yield return new WaitForSeconds(jumpAttackCooldown);
        jumpAttackIsPossible = true;
    }

    /// <summary>
    /// Атаковать
    /// </summary>
    protected override void Attack()
    {
        Animate(new AnimationEventArgs("attack", balance==usualBalance?"":"Idle", Mathf.RoundToInt(10 * (attackParametres.preAttackTime + attackParametres.actTime + attackParametres.endAttackTime))));
        StartCoroutine("AttackProcess");
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        HitParametres _attackParametres = balance == usualBalance ? attackParametres : jumpAttackParametres;
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(_attackParametres.preAttackTime);
        hitBox.SetHitBox(new HitParametres(_attackParametres));
        yield return new WaitForSeconds(_attackParametres.actTime + _attackParametres.endAttackTime);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
        balance = usualBalance;
    }

    /// <summary>
    /// Функция, возвращающая информацию об ориентации паука
    /// </summary>
    public Vector2 GetSpiderOrientation()
    {
        return spiderOrientation;
    }

    /// <summary>
    /// Обновить информацию, важную для моделей поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        jumping = false;
        avoid = false;
        StopCoroutine("BecomeCalmProcess");
    }

    /// <summary>
    /// Перейти в спокойное состояние
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        if (!optimized && loyalty!=LoyaltyEnum.ally && !immobile && patrolDistance>.1f)
            Patrol();
        gameObject.layer = LayerMask.NameToLayer(loyalty==LoyaltyEnum.ally?"hero":"character");
        //if (sight != null ? sight.enabled : false)
        //sight.RotateLocal(0f);
    }

    /// <summary>
    /// Перейти в агрессивное состояние
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        gameObject.layer = LayerMask.NameToLayer(loyalty == LoyaltyEnum.ally ? "hero" : "character");
        jumping = false;
        avoid = false;
        if (loyalty==LoyaltyEnum.neutral)
        {
            Loyalty = LoyaltyEnum.enemy;//Паук теперь всегда будет нападать на игрока и будет искать его
            //sight.enabled = true;
            //sight.ChangeSightMod();
        }
        prevTargetPosition = new EVector3(Vector3.zero);
        //wallCheck.WhatIsWall.Remove("character");
        //wallCheck.WhatIsWall.Remove("hero");
        /*if (sight != null)
        {
            sight.WhatToSight = LayerMask.GetMask("ground");
            sight.SetSightMod(true);
        }*/
    }

    /// <summary>
    /// Перейти в состояние патрулирования
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        //wallCheck.WhatIsWall.Remove("character");
        //wallCheck.WhatIsWall.Remove("hero");
        gameObject.layer = LayerMask.NameToLayer("characterWithoutPlatform");
        /*if (sight != null ? sight.enabled : false)
        {
            sight.WhatToSight = LayerMask.GetMask("character", "ground");
            sight.RotateLocal(0f);
            sight.SetSightMod(false);
        }*/

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
        //wallCheck.WhatIsWall.Add("character");
        //wallCheck.WhatIsWall.Add("hero");
        /*if (sight != null)
        {
            sight.WhatToSight = LayerMask.GetMask("character", "ground");
            sight.SetSightMod(false);
        }*/
    }

    /// <summary>
    /// Процесс обхода препятствия
    /// </summary>
    protected override IEnumerator AvoidProcess()
    {
        avoid = true;
        EVector3 _prevPos = prevPosition;
        yield return new WaitForSeconds(avoidTime);
        //Если не сдвигаемся с места, то нужно обойти препятствие
        Vector3 pos = (Vector2)transform.position;
        if ((pos - _prevPos).sqrMagnitude < speed * Time.fixedDeltaTime / 10f && avoid)
        {
            if (currentTarget.exists && currentTarget != mainTarget)
            {
                transform.position += (currentTarget - pos).normalized * navCellSize;
                yield return new WaitForSeconds(avoidTime);
                pos = (Vector2)transform.position;
                //Если всё равно не получается обойти ставшее на пути препятствие
                if (currentTarget != null && currentTarget != mainTarget && (pos - _prevPos).sqrMagnitude < speed * Time.fixedDeltaTime / 10f && avoid)
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
                    if (behavior == BehaviorEnum.patrol)
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
                    if (behavior == BehaviorEnum.patrol)
                        StartCoroutine(ResetStartPositionProcess(pos));
                }
            }
        }
        avoid = false;

    }

    protected override void StopAvoid()
    {
        base.StopAvoid();
        StopCoroutine("WrongOrientationProcess");
    }

    #region behaviourActions

    /// <summary>
    /// Спокойное поведение
    /// </summary>
    protected override void CalmBehavior()
    {
        base.CalmBehavior();
        Vector2 pos = transform.position;
        if (Loyalty != LoyaltyEnum.ally)
        {
            if (currentTarget.exists)
            {
                Vector2 targetPos = currentTarget;
                if (patrolDistance>.1f?(Vector2.Distance(targetPos, pos) < attackDistance) || (wallCheck.WallInFront || !(precipiceCheck.WallInFront)):false)
                {
                    Turn();
                    Patrol();
                }
                else
                {
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPos.x - pos.x)));
                }
            }
        }
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        base.AgressiveBehavior();
        if (mainTarget.exists && employment > 2)
        {
            Vector2 targetPosition = mainTarget;
            Vector2 pos = transform.position;
            if (waypoints == null)
            {

                #region directWay

                if (waiting)
                {
                    #region waiting

                    float sqDistance = Vector2.SqrMagnitude(targetPosition - pos);
                    if (sqDistance < waitingNearDistance * waitingNearDistance)
                    {
                        OrientationEnum nextOrientation = (OrientationEnum)Mathf.Sign(Vector2.Dot(pos-targetPosition, movementDirection));
                        if (!wallCheck.WallInFront && (precipiceCheck.WallInFront))
                            Move(nextOrientation);
                        else if (orientation!=nextOrientation)
                            Turn();
                    }
                    else if (sqDistance < waitingFarDistance * waitingFarDistance)
                    {
                        StopMoving();
                        if ((int)orientation * (targetPosition - pos).x < 0f)
                            Turn();
                    }
                    else
                    {
                        OrientationEnum nextOrientation = (OrientationEnum)Mathf.Sign(Vector2.Dot(targetPosition - pos, movementDirection));
                        if (!wallCheck.WallInFront && (precipiceCheck.WallInFront))
                            Move(nextOrientation);
                        else if (nextOrientation!=orientation)
                            Turn();
                        else
                        {
                            if (Vector2.SqrMagnitude(pos - mainTarget) > minCellSqrMagnitude * 16f &&
                                (Vector2.SqrMagnitude(mainTarget - (Vector2)prevTargetPosition) > minCellSqrMagnitude || !prevTargetPosition.exists))
                            {
                                //prevTargetPosition = new EVector3(mainTarget.transform.position, true);
                                Waypoints = FindPath(targetPosition, maxAgressivePathDepth);
                                if (waypoints==null)
                                    StopMoving();
                            }
                            else
                                StopMoving();
                        }
                    }

                    #endregion //waiting
                }
                else
                {
                    #region active

                    if (employment > 8)
                    {

                        OrientationEnum nextOrientation = (OrientationEnum)Mathf.Sign(Vector2.Dot(targetPosition - pos, movementDirection));
                        float sqDistance = Vector2.SqrMagnitude(targetPosition - pos);

                        if (jumpAttackIsPossible? 
                            Mathf.Approximately(spiderOrientation.y,1f)? 
                                                sqDistance<jumpAttackMaxDistance*jumpAttackMaxDistance && sqDistance>jumpAttackMinDistance*jumpAttackMinDistance :false : false)
                        {
                            if (Vector2.Dot(targetPosition - pos, movementDirection) * (int)orientation < 0f)
                                Turn();
                            StopMoving();
                            JumpAttack();
                        }
                        else if (sqDistance > attackDistance * attackDistance)
                        {
                            if (!wallCheck.WallInFront && (precipiceCheck.WallInFront))
                                Move(nextOrientation);
                            else if (orientation!=nextOrientation)
                                Turn();
                            else
                            {
                                if (Vector2.SqrMagnitude(pos - mainTarget) > minCellSqrMagnitude * 16f &&
                                    (Vector2.SqrMagnitude(mainTarget - (Vector2)prevTargetPosition) > minCellSqrMagnitude || !prevTargetPosition.exists))
                                {
                                    //prevTargetPosition = new EVector3(mainTarget.transform.position, true);
                                    Waypoints = FindPath(targetPosition, maxAgressivePathDepth);
                                    if (waypoints == null)
                                        StopMoving();
                                }
                                else
                                    StopMoving();
                            }
                        }
                        else
                        {
                            if (Vector2.Dot(targetPosition - pos,movementDirection) * (int)orientation < 0f)
                                Turn();
                            StopMoving();
                            Attack();
                        }
                    }

                    /*if (spiderOrientation.y < 0 || spiderOrientation.y < Mathf.Abs(spiderOrientation.x))
                        if (Mathf.Abs(targetPosition.x - pos.x) < attackDistance / 2f)
                            JumpDown();*/

                    #endregion //active
                }

                #endregion //directWay

            }
            else
            {

                #region complexWay

                if (waypoints.Count > 0)
                {
                    if (!currentTarget.exists)
                    {
                        currentTarget = new ETarget(waypoints[0].cellPosition);
                    }

                    targetPosition = currentTarget;
                    Vector2 direction = targetPosition - pos;
                    float projectionLength = Vector2.Dot(direction, movementDirection);
                    if (Mathf.Abs(projectionLength) > spiderOffset || Vector2.SqrMagnitude(direction) < spiderOffset * 2f)
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(projectionLength)));
                    //else
                    //{
                    //StopMoving();
                    //transform.position += movementDirection * proectionLength;
                    //}
                    if (!jumping)
                    {
                        if (NeedToFindPath())
                        {
                            waypoints = FindPath(mainTarget, maxAgressivePathDepth);
                            prevTargetPosition = new EVector3(mainTarget, true);
                            return;
                        }
                    }

                    if (currentTarget != mainTarget && Vector2.SqrMagnitude(currentTarget - pos) < navCellSize * navCellSize / 4f)
                    {
                        if (jumping)
                            jumping = false;
                        ComplexNavigationCell currentWaypoint = (ComplexNavigationCell)waypoints[0];
                        currentTarget.Exists = false;
                        waypoints.RemoveAt(0);

                        if (waypoints.Count > 2)
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
                                //Если путь прямой, несложный, то паук может самостоятельно добраться до игрока, не используя маршрута, и атаковать его
                                Waypoints = null;
                                prevTargetPosition = EVector3.zero;
                                return;
                            }
                        }


                        if (waypoints.Count == 0)
                        {
                            Waypoints = null;
                            prevTargetPosition = EVector3.zero;
                            return;
                        }
                        else
                        {
                            ComplexNavigationCell nextWaypoint = (ComplexNavigationCell)waypoints[0];
                            //Продолжаем следование
                            currentTarget = new ETarget(nextWaypoint.cellPosition);
                            if (currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb).connectionType == NavCellTypeEnum.jump)
                            {
                                //Перепрыгиваем препятствие
                                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x)));
                                transform.position = new Vector3(currentWaypoint.cellPosition.x + (int)orientation * navCellSize / 2f, pos.y,0f);
                                jumping = true;
                                Jump();
                            }
                            else if (spiderOrientation.y < 0f)
                            {
                                if (currentWaypoint.cellPosition.x == nextWaypoint.cellPosition.x && nextWaypoint.cellPosition.y < currentWaypoint.cellPosition.y - 2 * navCellSize)
                                {
                                    //Спрыгиваем вниз
                                    jumping = true;
                                    JumpDown();
                                }
                            }
                        }
                    }
                    if (!jumping)
                    {
                        //Учёт земных поверхностей, к которым может прикрепиться паук
                        if (wallCheck.WallInFront)
                        {
                            RaycastHit2D hit = Physics2D.Raycast(pos, (int)orientation * movementDirection, navCellSize, LayerMask.GetMask(gLName));
                            if (hit)
                            {
                                ChangeOrientation(hit.collider);
                            }
                        }
                        else if (!precipiceCheck.WallInFront)
                        {
                            RaycastHit2D hit = Physics2D.Raycast(pos - (int)orientation * Vector2.right * navCellSize / 2f, -spiderOrientation, navCellSize, LayerMask.GetMask(gLName));
                            if (hit)
                            {
                                ChangeOrientation(hit.collider);
                            }
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
        if (waypoints != null ? waypoints.Count > 0 : false)
        {
            if (!currentTarget.exists)
                currentTarget = new ETarget(waypoints[0].cellPosition);

            Vector2 pos = transform.position;
            Vector2 targetPosition = currentTarget;
            Vector2 direction = targetPosition - pos;
            float projectionLength = Vector2.Dot(direction, movementDirection);
            if (Mathf.Abs(projectionLength) > spiderOffset || Vector2.SqrMagnitude(pos - targetPosition) < spiderOffset * 2f)
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(projectionLength)));
            else
                //{
                StopMoving();
            //transform.position += movementDirection * proectionLength;
            //}

            /*if (transform.parent != null)
            {
                if (transform.GetComponentInParent<MovingPlatform>() != null)
                {
                    if (spiderOrientation.y > Mathf.Abs(spiderOrientation.x))
                        Jump();//Мы не хотим, чтобы паук стоял на платформе, когда он патрулирует
                }
            }*/
            if (currentTarget != mainTarget && Vector3.SqrMagnitude(currentTarget - pos) < navCellSize * navCellSize / 4f)
            {
                if (jumping)
                    jumping = false;
                ComplexNavigationCell currentWaypoint = (ComplexNavigationCell)waypoints[0];
                currentTarget.Exists = false;
                waypoints.RemoveAt(0);
                if (waypoints.Count == 0)
                {
                    StopMoving();
                    //Достигли конца маршрута
                    if (Vector3.SqrMagnitude(beginPosition - currentWaypoint.cellPosition) < minCellSqrMagnitude)
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
                    //Продолжаем следование
                    currentTarget = new ETarget(nextWaypoint.cellPosition);
                    if (currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb).connectionType == NavCellTypeEnum.jump)
                    {
                        //Перепрыгиваем препятствие
                        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x)));
                        transform.position = new Vector3(currentWaypoint.cellPosition.x + (int)orientation * navCellSize / 2f, pos.y);
                        jumping = true;
                        Jump();
                    }
                    else if (spiderOrientation.y < 0f)
                    {
                        if (currentWaypoint.cellPosition.x == nextWaypoint.cellPosition.x && nextWaypoint.cellPosition.y < currentWaypoint.cellPosition.y - 2 * navCellSize)
                        {
                            //Спрыгиваем вниз
                            jumping = true;
                            JumpDown();
                        }
                    }
                }
            }
            if (!jumping)
            {
                //Учёт земных поверхностей, к которым может прикрепиться паук
                if (wallCheck.CheckWall())
                {
                    RaycastHit2D hit = Physics2D.Raycast(pos, (int)orientation * movementDirection, navCellSize, LayerMask.GetMask(gLName));
                    if (hit)
                    {
                        ChangeOrientation(hit.collider);
                    }
                }
                else if (!precipiceCheck.CheckWall())
                {
                    RaycastHit2D hit = Physics2D.Raycast(pos - (int)orientation * movementDirection * navCellSize / 2f, -spiderOrientation, navCellSize, LayerMask.GetMask(gLName));
                    if (hit)
                    {
                        ChangeOrientation(hit.collider);
                    }
                }
            }
        }
        else
            GoHome();
    }

    #endregion //behaviourActions

    /*
    /// <summary>
    /// Функция, которая строит маршрут для паука
    /// </summary>
    /// <param name="endPoint">точка назначения</param>
    ///<param name="maxDepth">Максимальная сложность маршрута</param>
    /// <returns>Навигационные ячейки, составляющие маршрут</returns>
    protected override List<NavigationCell> FindPath(Vector2 endPoint, int _maxDepth)
    {
        if (currentTarget != null && currentTarget != mainTarget)
            Destroy(currentTarget);
        currentTarget = null;
        NavigationSystem navSystem = SpecialFunctions.statistics.navSystem;
        if (navSystem == null)
            return null;
        navCellSize = navSystem.cellSize.magnitude;
        NavigationMap navMap = navSystem.GetMap(GetMapType());
        if (navMap == null)
            return null;

        List<NavigationCell> _path = new List<NavigationCell>();
        NavigationCell beginCell = navMap.GetCurrentCell(transform.position), endCell = navMap.GetCurrentCell(endPoint);

        if (beginCell == null || endCell == null)
            return null;


        int depthOrder = 0, currentDepthCount = 1, nextDepthCount = 0;
        navMap.ClearMap();
        Queue<NavigationCell> cellsQueue = new Queue<NavigationCell>();
        cellsQueue.Enqueue(beginCell);
        beginCell.visited = true;
        while (cellsQueue.Count > 0 && endCell.fromCell == null)
        {
            NavigationCell currentCell = cellsQueue.Dequeue();
            if (currentCell == null)
                return null;
            List<NavigationCell> neighbourCells = currentCell.neighbors.ConvertAll<NavigationCell>(x => navMap.GetCell(x.groupNumb, x.cellNumb));
            foreach (NavigationCell cell in neighbourCells)
            {
                if (cell != null ? !cell.visited : false)
                {
                    cell.visited = true;
                    cellsQueue.Enqueue(cell);
                    cell.fromCell = currentCell;
                    nextDepthCount++;
                }
            }
            currentDepthCount--;
            if (currentDepthCount == 0)
            {
                //Если путь оказался состоящим из слишком большого количества узлов, то не стоит пользоваться этим маршрутом. 
                //Этот алгоритм поиска используется для создания коротких маршрутов, которые можно будет быстро поменять при необходимости. 
                //Эти маршруты используются в агрессивном состоянии, когда не должно быть такого, 
                //что ИИ обходит слишком большие дистанции, чтобы достичь игрока. Если такое случается, он должен ждать, чему соответствует несуществование подходящего маршрута
                depthOrder++;
                if (depthOrder == _maxDepth)
                    return null;
                currentDepthCount = nextDepthCount;
                nextDepthCount = 0;
            }
        }

        if (endCell.fromCell == null)//Невозможно достичь данной точки
            return null;

        //Восстановим весь маршрут с последней ячейки
        NavigationCell pathCell = endCell;
        _path.Insert(0, pathCell);
        while (pathCell.fromCell != null)
        {
            _path.Insert(0, pathCell.fromCell);
            pathCell = pathCell.fromCell;
        }

        #region optimize

        //Удалим все ненужные точки
        for (int i = 0; i < _path.Count - 2; i++)
        {
            NavigationCell checkPoint1 = _path[i], checkPoint2 = _path[i + 1];
            if (checkPoint1.cellType == NavCellTypeEnum.jump || checkPoint1.cellType == NavCellTypeEnum.movPlatform)
                continue;
            if (checkPoint1.cellType != checkPoint2.cellType)
                continue;
            Vector2 movDirection1 = (checkPoint2.cellPosition - checkPoint1.cellPosition).normalized;
            Vector2 movDirection2 = Vector2.zero;
            int index = i + 2;
            NavigationCell checkPoint3 = _path[index];
            while (Vector2.SqrMagnitude(movDirection1 - (checkPoint3.cellPosition - checkPoint2.cellPosition).normalized) < .01f &&
                   checkPoint1.cellType == checkPoint3.cellType &&
                   index < _path.Count)
            {
                index++;
                if (index < _path.Count)
                {
                    checkPoint2 = checkPoint3;
                    checkPoint3 = _path[index];
                }
            }
            for (int j = i + 1; j < index - 1; j++)
            {
                _path.RemoveAt(i + 1);
            }
        }

        #endregion //optimize

        return _path;
    }
    */

    #region optimization

    /// <summary>
    /// Включить риджидбоди
    /// </summary>
    protected override void EnableRigidbody()
    {
        if (inGround)
            return;
        base.EnableRigidbody();
    }

    /// <summary>
    /// Включить все коллайдеры в персонаже
    /// </summary>
    protected override void EnableColliders()
    {
        if (inGround)
            return;
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (Collider2D col in cols)
            col.enabled = true;
    }

    /// <summary>
    /// Включить визуальное отображение
    /// </summary>
    protected override void EnableVisual()
    {
        base.EnableVisual();
        Vector2 localPos = anim.transform.localPosition;
        anim.transform.localPosition = new Vector3(0f, localPos.y);
        if (inGround)
            Animate(new AnimationEventArgs("setInGround", spiderStartPosition == SpiderStartPositionEnum.behindWall ? "right" : "down", 0));
    }

    /// <summary>
    /// Сменить оптимизированную версию на активную
    /// </summary>
    protected override void ChangeBehaviorToOptimized()
    {
        GetOptimizedPosition();
        Optimized = true;
        switch (behavior)
        {
            case BehaviorEnum.calm:
                {
                    behaviorActions = CalmOptBehavior;
                    if (currentTarget.exists && !immobile && patrolDistance>.1f)
                        FindFrontPatrolTarget();
                    break;
                }
            case BehaviorEnum.agressive:
                {
                    behaviorActions = AgressiveOptBehavior;
                    break;
                }
            case BehaviorEnum.patrol:
                {
                    behaviorActions = PatrolOptBehavior;
                    break;
                }
            default:
                break;
        }
    }

    /*
    /// <summary>
    /// Включить зрение
    /// </summary>
    protected virtual void EnableSight()
    {
        sight.gameObject.SetActive(true);
        SpiderOrientation = spiderOrientation;
    }

    /// <summary>
    /// Выключить зрение
    /// </summary>
    protected virtual void DisableSight()
    {
        if (behaviour == BehaviourEnum.agressive)
            GoToThePoint(mainTarget.transform.position);
            //FindPath(mainTarget.transform.position, maxAgressivePathDepth * 5);
        sight.gameObject.SetActive(false);
    }
    */

    /*
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
    */

    /// <summary>
    /// Функция, которая отыскивает навигационную ячейку, находящаяся в доступных пауку пределах (паук стоит на земле и не использует стены и потолок, а также не прыгает) и ставит её текущей целью паука.
    /// </summary>
    protected void FindFrontPatrolTarget()
    {
        Vector2 pos = transform.position;
        if (navMap == null || !(navMap is NavigationBunchedMap))
            return;
        NavigationBunchedMap _map = (NavigationBunchedMap)navMap;
        ComplexNavigationCell currentCell = (ComplexNavigationCell)_map.GetCurrentCell(transform.position);
        if (currentCell == null)
            return;
        bool hasNext = true;
        ComplexNavigationCell nextCell = currentCell;
        while (hasNext)
        {
            Vector2 pos1 = currentCell.cellPosition;
            hasNext = false;
            if (Mathf.Abs(pos1.x - pos.x) > patrolDistance)
            {
                break;
            }
            foreach (NeighborCellStruct neighbor in currentCell.neighbors)
            {
                nextCell = _map.GetCell(neighbor.groupNumb, neighbor.cellNumb);
                Vector2 pos2 = nextCell.cellPosition;
                if (Mathf.Abs(pos1.y - pos2.y) < navCellSize / 2f && (pos2.x - pos1.x) * (int)orientation > 0 && neighbor.connectionType==NavCellTypeEnum.usual)
                {
                    currentCell = nextCell;
                    hasNext = true;
                    break;
                }
            }
        }
        currentTarget = new ETarget(currentCell.cellPosition);
    }

    /// <summary>
    /// Функция реализующая анализ окружающей персонажа обстановки, когда тот находится в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
        if (immobile)
            return;
            if (behavior != BehaviorEnum.calm)
        {
            if (!followOptPath)
                StartCoroutine("PathPassOptProcess");
        }
        else
        {
                if (!currentTarget.exists && patrolDistance>.1f)
                {
                    StopCoroutine("PathPassOptProcess");//На всякий случай
                    Turn();
                    FindFrontPatrolTarget();
                    if (currentTarget.exists)
                        StartCoroutine("PathPassOptProcess");
                }
                else if (!followOptPath)
                    StartCoroutine("PathPassOptProcess");
            }
    }

    /// <summary>
    /// Функция, которая восстанавливает положение и состояние персонажа, пользуясь данными, полученными в оптимизированном режиме
    /// </summary>
    protected override void RestoreActivePosition()
    {
        if (behavior == BehaviorEnum.calm || !currentTarget.exists)
        {
            SpiderOrientation = Vector2.up;
            Turn(beginOrientation);
        }
        else
        {
            if (jumping)
            {
                SpiderOrientation = Vector2.up;
                jumping = false;
            }
            else 
            {
                RestoreSpiderOrientation();
            }
        }
    }

    /// <summary>
    /// Функция, которая переносит персонажа в ту позицию, в которой он может нормально функционировать в оптимизированной версии 
    /// </summary>
    protected override void GetOptimizedPosition()
    {
        StopAvoid();
        SpiderOrientation = Vector2.up;
        if (rigid.gravityScale>0f && !Physics2D.Raycast((Vector2)transform.position - (int)orientation * Vector2.right * navCellSize / 2f, -spiderOrientation, navCellSize, LayerMask.GetMask(gLName)))
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
                    transform.position = hit.point + Vector2.up * spiderOffset;
                }
            }
        }
        //if (behavior == BehaviorEnum.calm)
            //FindFrontPatrolTarget();
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
            while ((waypoints != null ? waypoints.Count > 0 : false) || currentTarget.exists)
            {
                if (!currentTarget.exists)
                {
                    currentTarget = new ETarget(waypoints[0].cellPosition);
                }

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
                        jumping = false;
                        ComplexNavigationCell nextCell = (ComplexNavigationCell)waypoints[0];
                        currentTarget = new ETarget(nextCell.cellPosition);
                        NeighborCellStruct neighborConnection = currentCell.GetNeighbor(nextCell.groupNumb,nextCell.cellNumb);
                        if (neighborConnection.connectionType == NavCellTypeEnum.jump)
                        {
                            jumping = true;
                        }
                        else if (neighborConnection.groupNumb!=-1 &&
                                currentCell.cellPosition.x == nextCell.cellPosition.x && nextCell.cellPosition.y < currentCell.cellPosition.y - 2 * navCellSize)
                        {
                            jumping = true;
                        }
                        if (neighborConnection.groupNumb != -1)
                        {
                            transform.position = nextCell.cellPosition;
                            yield return new WaitForSeconds(optTimeStep);
                            continue;                            
                        }
                    }
                }
                if (currentTarget.exists)
                {
                    targetPos = currentTarget;
                    Vector2 direction = targetPos - pos;
                    transform.position = pos + direction.normalized * Mathf.Clamp(speed, 0f, direction.magnitude);
                }
                yield return new WaitForSeconds(optTimeStep);
            }
            waypoints = null;
            currentTarget.Exists = false;
            followOptPath = false;
        }
    }

    #endregion //optimization

    #region eventHandlers

    /*
    /// <summary>
    /// Обработка события "Увидел врага"
    /// </summary>
    protected virtual void HandleSightInEvent(object sender, EventArgs e)
    {
        if (behaviour != BehaviourEnum.agressive)
            BecomeAgressive();
    }

    /// <summary>
    /// Обработка события "Упустил из виду врага"
    /// </summary>
    protected virtual void HandleSightOutEvent(object sender, EventArgs e)
    {
        if (behaviour==BehaviourEnum.agressive)
        {
            //waypoints = FindPath(mainTarget.transform.position, maxAgressivePathDepth * 5);
            //if (waypoints != null)
            //{
                GoToThePoint(mainTarget.transform.position);//Выдвинуться туда, где в последний раз видел врага
                StartCoroutine("BecomeCalmProcess");
            //}
            //else
               //GoHome();
        }
    }
    */

    /*
    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected void HandleAttackProcess(object sender, HitEventArgs e)
    {
        //Если игрок случайно наткнулся на паука и получил урон, то паук автоматически становится агрессивным
        if (behaviour!=BehaviourEnum.agressive)
            BecomeAgressive();
    }
    */

    #endregion //eventHandlers

    #region id

    /// <summary>
    /// Получить данные о враге с целью сохранить их
    /// </summary>
    public override EnemyData GetAIData()
    {
        SpiderData eData = new SpiderData(this);
        return eData;
    }

    /// <summary>
    /// Настроить персонажа в соответствии с загруженными данными
    /// </summary>
    public override void SetAIData(EnemyData eData)
    {
        if (eData != null)
        {
            transform.position = eData.position;
            SpiderOrientation=((SpiderData)eData).spiderOrientation;

            bool _inGround = ((SpiderData)eData).inGround;
            if (_inGround)
            {
                immobile = true;
                rigid.isKinematic = true;
                DisableColliders();
                Animate(new AnimationEventArgs("setInGround", spiderStartPosition == SpiderStartPositionEnum.behindWall ? "right" : "down", 0));
                inGround = true;
            }
            else
            {
                rigid.isKinematic = false;
                immobile = false;
                inGround = false;
                EnableColliders();
                Loyalty = LoyaltyEnum.enemy;
            }

            if (transform.localScale.x * eData.orientation < 0f)
                Turn((OrientationEnum)eData.orientation);

            string behaviorName = eData.behavior;
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
                        if (eData.waypoints.Count > 0)
                        {
                            waypoints = new List<NavigationCell>();
                            for (int i = 0; i < eData.waypoints.Count; i++)
                                waypoints.Add(navMap.GetCurrentCell(eData.waypoints[i]));
                            ComplexNavigationCell nextCell = (ComplexNavigationCell)waypoints[0];
                            if (nextCell.cellType == NavCellTypeEnum.jump)
                                transform.position = nextCell.cellPosition;
                        }
                        break;
                    }
                case "patrol":
                    {
                        BecomePatrolling();
                        if (eData.waypoints.Count > 0)
                        {
                            waypoints = new List<NavigationCell>();
                            for (int i = 0; i < eData.waypoints.Count; i++)
                                waypoints.Add(navMap.GetCurrentCell(eData.waypoints[i]));
                            ComplexNavigationCell nextCell = (ComplexNavigationCell)waypoints[0];
                            if (nextCell.cellType==NavCellTypeEnum.jump)
                                transform.position = nextCell.cellPosition;
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

            TargetData currentTargetData = eData.currentTargetData;
            TargetData mainTargetData = eData.mainTargetData;

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
            Health = eData.health;
        }
    }

    #endregion //id

    /// <summary>
    /// Вернуть тип, используемой карты навигации
    /// </summary>
    public override NavMapTypeEnum GetMapType()
    {
        return NavMapTypeEnum.crawl;
    }

    #region storyActions

    /// <summary>
    /// Выйти вперёд
    /// </summary>
    public void MoveOutAction(StoryAction _action)
    {
        MoveOut();
    }

    /// <summary>
    /// Процесс нападения из засады
    /// </summary>
    IEnumerator MoveOutProcess()
    {
        yield return new WaitForSeconds(appearTime);
        Animate(new AnimationEventArgs("moveOut"));
        yield return new WaitForSeconds(1f);
        rigid.isKinematic = false;
        immobile = false;
        inGround = false;
        EnableColliders();
        Loyalty = LoyaltyEnum.enemy;
    }

    /// <summary>
    /// Выдвинуться вперёд (из некоторого препятствия)
    /// </summary>
    protected void MoveOut()
    {
        StartCoroutine("MoveOutProcess");
    }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public override List<string> actionNames()
    {
        List<string> _actionNames = base.actionNames();
        _actionNames.Add("moveOut");
        return _actionNames;
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs1()
    {
        Dictionary<string, List<string>> _actionIDs1 = base.actionIDs1();
        _actionIDs1.Add("moveOut", new List<string>() { });
        return _actionIDs1;
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs2()
    {
        Dictionary<string, List<string>> _actionIDs2 = base.actionIDs2();
        _actionIDs2.Add("moveOut", new List<string>() { });
        return _actionIDs2;
    }

    #endregion //IHaveStory

    #region other

    /// <summary>
    /// Функция, возвращающая граничные точки простого коллайдера
    /// </summary>
    /// <param name="col">заданный коллайдер</param>
    /// <returns></returns>
    static Vector2[] GetColliderPoints(Collider2D col)
    {
        if (col is PolygonCollider2D)
        {
            Vector2[] points = ((PolygonCollider2D)col).points;
            for (int i = 0; i < points.Length; i++)
                points[i] = (Vector2)col.transform.TransformPoint((Vector3)points[i]);
            return points;
        }
        else if (col is BoxCollider2D)
        {
            BoxCollider2D bCol = (BoxCollider2D)col;
            float angle = Mathf.Repeat(bCol.transform.eulerAngles.z, 90f) * Mathf.PI / 180f;

            Vector2 e = bCol.bounds.extents;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float cos2 = Mathf.Cos(2 * angle);
            float b = 2 * (e.x * sin - e.y * cos) / -cos2;

            Vector3 b1 = new Vector3(e.x - b * sin, e.y),
                    b2 = new Vector3(e.x, e.y - b * cos);

            Transform bTrans = bCol.transform;
            Vector3 vect = bCol.transform.position;
            Vector2[] points = new Vector2[] { vect + b1, vect + b2, vect - b1, vect - b2 };
            return points;
        }
        return null;
    }

    /// <summary>
    /// Узнать точку пересечения заданной прямой и ортогонального ей вектора, пущенного из точки
    /// </summary>
    /// <param name="point1">первая точка, принадлежащая заданной прямой</param>
    /// <param name="point2">вторая точка, принадлежащая заданной прямой</param>
    /// <param name="fromPoint">точка, откуда мы ищем точку пересечения</param>
    /// <returns>Точка пересечения</returns>
    protected Vector2 GetConnectionPoint(Vector2 point1, Vector2 point2, Vector2 fromPoint)
    {
        Vector2 connectionPoint = Vector2.zero;//Точка пересечения 2-х прямых
        Vector2 normal = GetNormal(point1, point2);//Нормаль рассматриваемой поверхности
        //if (Vector2.Angle(spiderOrientation, normal) < minAngle)
            //return Vector2.zero;
        if (point1.x - point2.x == 0)
            connectionPoint = new Vector2(point1.x, normal.y / normal.x * (point1.x - fromPoint.x) + fromPoint.y);
        else if (normal.x == 0)
            connectionPoint = new Vector2(fromPoint.x, (point2.y - point1.y) / (point2.x - point1.x) * (fromPoint.x - point1.x) + point1.y);
        else
        {
            float newX = ((normal.y / normal.x) * fromPoint.x -
                                        (point2.y - point1.y) / (point2.x - point1.x) * point1.x +
                                                            (point1.y - fromPoint.y)) /
                            (normal.y / normal.x - (point2.y - point1.y) / (point2.x - point1.x));
            float newY = normal.y / normal.x * (newX - fromPoint.x) + fromPoint.y;
            connectionPoint = new Vector2(newX, newY);
        }
        //Если точка крепления по какой-то причине оказалась не между двумя точками заданной прямой, то установить точкой крепления ближайшую из этой точек
        if ((connectionPoint.x - point1.x) * (connectionPoint.x - point2.x) > 0 || (connectionPoint.y - point1.y) * (connectionPoint.y - point2.y) > 0)
            connectionPoint = (Vector2.SqrMagnitude(connectionPoint - point1) < Vector2.SqrMagnitude(connectionPoint - point2) ? point1 + (point2-point1).normalized*spiderOffset : 
                                                                                                                                 point2+(point1 - point2).normalized * spiderOffset);

        return connectionPoint;
    }

    /// <summary>
    /// Возвращает вектор нормали заданной поверхности земли
    /// </summary>
    /// <param name="surfacePoint1">Первая точка, заданной прямой</param>
    /// <param name="surfacePoint2">Вторая точка, заданной прямой</param>
    /// <returns>Вектор нормали</returns>
    protected Vector2 GetNormal(Vector2 surfacePoint1, Vector2 surfacePoint2)
    {

        Vector2 direction = (surfacePoint2 - surfacePoint1).normalized;
        Vector2 normal = new Vector2(1, 0);
        if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(normal, direction)), 1f))
            normal = new Vector2(0, 1);
        else
            normal = (normal - Vector2.Dot(normal, direction) * direction).normalized;

        return normal;
    }

    /// <summary>
    /// Возвращает вектор нормали заданной поверхности земли
    /// </summary>
    /// <param name="surfacePoint1">Первая точка, заданной прямой</param>
    /// <param name="surfacePoint2">Вторая точка, заданной прямой</param>
    /// <returns>Вектор нормали</returns>
    protected Vector2 GetNormal(Vector2 surfacePoint1, Vector2 surfacePoint2, Collider2D gCol)
    {
        Vector2 direction = (surfacePoint2 - surfacePoint1).normalized;
        Vector2 normal = new Vector2(1, 0);
        if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(normal, direction)), 1f))
            normal = new Vector2(0, 1);
        else
            normal = (normal - Vector2.Dot(normal, direction) * direction).normalized;
        Vector2 _point = (surfacePoint1 + surfacePoint2) / 2f+normal * 0.02f;
        if (gCol.OverlapPoint(_point))
            normal *= -1f;

        return normal;
    }

    /// <summary>
    /// Возвращает вектор нормали по отношению к заданному вектору. Причём от этой нормали до заданного вектора должен быть кратчайший поворот против часовой стрелки
    /// </summary>
    /// <param name="direction">Заданное направление, по отношению к которому строим нормаль</param>
    /// <returns>Вектор нормали</returns>
    protected Vector2 GetNormal(Vector2 direction)
    {
        Vector2 normal = new Vector2(1, 0);
        if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(normal, direction)), 1f))
            normal = new Vector2(0, 1);
        else
            normal = (normal - Vector2.Dot(normal, direction) * direction).normalized;
        if (normal.x*direction.y-normal.y*direction.x<0)
            normal *= -1;

        return normal;
    }

    /// <summary>
    /// Восстановить пространственную ориентацию паука
    /// </summary>
    protected virtual void RestoreSpiderOrientation()
    {
        Vector2 pos = transform.position;
        Vector2 direction = currentTarget - pos;
        Collider2D[] cols = Physics2D.OverlapAreaAll(pos + new Vector2(-navCellSize, navCellSize), pos + new Vector2(navCellSize, -navCellSize), LayerMask.GetMask(gLName));
        if (cols == null)
        {
            SpiderOrientation = Vector2.up;
            Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.x - pos.x)));
            return;
        }

        //Найдём ту сторону того коллайдера, которая имеет наименьшее расстояние до текущего положения паука
        float mDistance = Mathf.Infinity;
        Vector2[] chosenPoints = null;
        int chosenIndex = -1;
        Vector2 connectionPoint = Vector2.zero;
        Vector2[] colPoints = null;
        Collider2D chosenCol = null;

        for (int i = 0; i < cols.Length; i++)
        {

            colPoints = GetColliderPoints(cols[i]);

            if (colPoints.Length <= 0)
                continue;

            for (int j = 0; j < colPoints.Length; j++)
            {
                Vector2 point1 = colPoints[j];
                Vector2 point2 = j < colPoints.Length - 1 ? colPoints[j + 1] : colPoints[0];
                Vector2 normal = GetNormal(point1, point2);
                Vector2 _connectionPoint = GetConnectionPoint(point1, point2, transform.position);
                float newDistance = Vector2.SqrMagnitude(_connectionPoint - (Vector2)transform.position);
                if (newDistance < mDistance)
                {
                    connectionPoint = _connectionPoint;
                    mDistance = newDistance;
                    chosenPoints = colPoints;
                    chosenIndex = j;
                    chosenCol = cols[i];
                }
            }
        }

        if (chosenIndex < 0)
            return;

        Vector2 surfacePoint1 = chosenPoints[chosenIndex], surfacePoint2 = chosenPoints[chosenIndex < chosenPoints.Length - 1 ? chosenIndex + 1 : 0];

        ChangeOrientation(surfacePoint1, surfacePoint2, connectionPoint, chosenCol);
        float projectionLength = Vector2.Dot(direction, movementDirection);
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(projectionLength)));
    }

    /// <summary>
    /// Процесс, который вызывается, когда паук находится в непраильной ориентации
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator WrongOrientationProcess()
    {
        Vector2 currentPosition = transform.position;
        yield return new WaitForSeconds(0.5f);
        if (!precipiceCheck.WallInFront && !jumping && Vector2.SqrMagnitude((Vector2)transform.position - currentPosition) < minCellSqrMagnitude)
            SpiderOrientation = Vector2.up;
    }

    #endregion //other

}