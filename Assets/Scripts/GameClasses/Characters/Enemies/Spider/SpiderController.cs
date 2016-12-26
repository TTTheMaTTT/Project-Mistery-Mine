using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер паука
/// </summary>
public class SpiderController : AIController
{

    #region consts

    protected const float attackTime = .6f, preAttackTime = .3f;

    protected const float patrolDistance = 2f;//По таким дистанциям паук будет патрулировать

    protected const float beCalmTime = 10f;//Время через которое пауки перестают преследовать игрока, если он ушёл из их поля зрения
    protected const float avoidTime = 1f;//Время, спустя которое мжно судить о необходимости обхода препятствия

    protected const float spiderOffset = .052f;//Насколько должен быть смещён паук относительно поверхности земли

    protected const int maxAgressivePathDepth = 20;//Насколько сложен может быть путь паука, в агрессивном состоянии 
                                                   //(этот путь используется в тех случаях, когда невозможно настичь героя прямым путём)

    #endregion //consts

    #region fields

    protected WallChecker wallCheck, precipiceCheck;
    protected SightFrustum sight;//Зрение персонажа
    protected HitBox selfHitBox;//Хитбокс, который атакует персонажа при соприкосновении с пауком. Этот хитбокс всегда активен и не перемещается

    #endregion //fields

    #region parametres

    [SerializeField]protected float jumpForce = 60f;//Сила, с которой паук совершает прыжок
    protected bool jumping = false;
    protected bool avoid = false;//Обходим ли препятствие в данный момент?

    [SerializeField] protected float attackDistance = .2f;//На каком расстоянии должен стоять паук, чтобы решить атаковать

    protected Vector2 waypoint;//Пункт назначения, к которому стремится ИИ
    protected EVector3 prevTargetPosition = new EVector3(Vector3.zero);//Предыдущее местоположение цели
    protected EVector3 prevPosition = EVector3.zero;//Собственное предыдущее местоположение

    protected float navCellSize;

    protected bool moveOut = false;

    protected Vector2 spiderOrientation = new Vector2(0f, 1f);//нормаль поверхности, на которой стоит паук
    protected virtual Vector2 SpiderOrientation
    {
        set
        {
            spiderOrientation = value;
            movementDirection = GetNormal(spiderOrientation);
            if (sight != null ? sight.enabled : false)
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
            }
        }
    }
    protected Vector3 movementDirection = Vector3.right;//В какую сторону движется паук, если он повёрнут вправо

    //protected bool calmDown = false;//Успокаивается ли персонаж
    [SerializeField]protected bool neutral = true;//Является ли паук изначально нейтральным

    #endregion //parametres

    protected override void FixedUpdate()
    {
        if (!immobile)
            base.FixedUpdate();
        else if (moveOut)
            MoveOut();
        Animate(new AnimationEventArgs("groundMove"));
        Analyse();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            GoToThePoint(SpecialFunctions.player.transform.position);
        }
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

            Transform sightParent = indicators.FindChild("Sight");
            sight = sightParent!=null? sightParent.GetComponentInChildren<SightFrustum>():null;
            if (sight != null)
            {
                sight.sightInEventHandler += HandleSightInEvent;
                sight.sightOutEventHandler += HandleSightOutEvent;
                sight.enabled = !neutral;
                if (!neutral)
                    sight.RotateLocal((spiderOrientation.y >= 0 && Mathf.Abs(spiderOrientation.x) - Mathf.Abs(spiderOrientation.y) < 0) ? 0f : 90f);
            }

        }

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
            if (selfHitBox != null)
            {
                areaTrigger.triggerFunctionIn += EnableSelfHitBox;
                areaTrigger.triggerFunctionOut += DisableSelfHitBox;
            }
            if (sight != null)
            {
                areaTrigger.triggerFunctionIn += EnableSight;
                areaTrigger.triggerFunctionOut += DisableSight;
            }
        }

        Patrol();
    }

    protected override void FormDictionaries()
    {
        storyActionBase.Add("moveForward", MoveForwardAction);
    }

    #region movement

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = wallCheck.WallInFront()? new Vector3(0f,rigid.velocity.y,0f):(rigid.gravityScale == 0f ? movementDirection * (int)orientation * speed : new Vector3((int)orientation * speed, rigid.velocity.y));
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    protected override void StopMoving()
    {
        Vector2 projection = Vector2.Dot(rigid.velocity, movementDirection) * movementDirection;
        rigid.velocity -= projection;
    }

    /// <summary>
    /// Процесс обхода препятствия
    /// </summary>
    protected virtual IEnumerator AvoidProcess()
    {
        avoid = true;
        EVector3 _prevPos = prevPosition;
        yield return new WaitForSeconds(avoidTime);
        /*string groundName = "ground";
        bool a1 = Physics2D.OverlapArea((Vector2)transform.position + new Vector2(-navCellSize / 20f * 11f, navCellSize / 20f * 7f),
                                (Vector2)transform.position + new Vector2(-navCellSize / 20f * 9f, -navCellSize / 20f * 7f),
                                LayerMask.GetMask(groundName));
        bool a2 = Physics2D.OverlapArea((Vector2)transform.position + new Vector2(-navCellSize / 20f * 7f, navCellSize / 20f * 11f),
                                        (Vector2)transform.position + new Vector2(navCellSize / 20f * 7f, navCellSize / 20f * 9f),
                                        LayerMask.GetMask(groundName));
        bool a3 = Physics2D.OverlapArea((Vector2)transform.position + new Vector2(navCellSize / 20f * 9f, navCellSize / 20f * 7f),
                                        (Vector2)transform.position + new Vector2(navCellSize / 20f * 11f, -navCellSize / 20f * 7f),
                                        LayerMask.GetMask(groundName));
        bool a4 = Physics2D.OverlapArea((Vector2)transform.position + new Vector2(-navCellSize / 20f * 7f, -navCellSize / 20f * 9f),
                                        (Vector2)transform.position + new Vector2(navCellSize / 20f * 7f, -navCellSize / 20f * 11f),
                                        LayerMask.GetMask(groundName));*/
        if (currentTarget != null && currentTarget!=mainTarget && (transform.position - _prevPos).sqrMagnitude < speed * Time.fixedDeltaTime / 10f)
        {
            transform.position += (currentTarget.transform.position - transform.position).normalized * navCellSize;
        }
        avoid = false;

    }

    protected virtual void StopAvoid()
    {
        StopCoroutine(AvoidProcess());
        avoid = false;
    }

    /// <summary>
    /// Определить следующую точку патрулирования
    /// </summary>
    protected virtual void Patrol()
    {
        waypoint = new Vector3((int)orientation * patrolDistance, 0f,0f) + transform.position;
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
        float minDistance = Mathf.Infinity;
        int pointIndex = -1;
        for (int i = 0; i < colPoints.Length; i++)
        {
            Vector2 point1 = colPoints[i];
            Vector2 point2 = i < colPoints.Length - 1 ? colPoints[i + 1] : colPoints[0];
            Vector2 normal = GetNormal(point1, point2);
            if (Mathf.Abs(Vector2.Angle(spiderOrientation, normal)) >= minAngle)
            {
                Vector2 _connectionPoint = GetConnectionPoint(point1, point2, transform.position);
                float newDistance = Vector2.SqrMagnitude(_connectionPoint - (Vector2)transform.position);
                if (newDistance < minDistance)
                {
                    connectionPoint = _connectionPoint;
                    minDistance = newDistance;
                    pointIndex = i;
                }
            }
        }

        if (pointIndex < 0)
            return;

        Vector2 surfacePoint1 = colPoints[pointIndex], surfacePoint2 = colPoints[pointIndex < colPoints.Length - 1 ? pointIndex + 1 : 0];
        Vector2 _spiderOrientation = GetNormal(surfacePoint1, surfacePoint2);

        //На какой угол надо повернуть паука
        float angle = Vector2.Angle(spiderOrientation, _spiderOrientation)*(spiderOrientation.x*_spiderOrientation.y-spiderOrientation.y*_spiderOrientation.x);
       
        if (Mathf.Abs(angle) < minAngle)
            return;

        transform.eulerAngles = new Vector3(0f, 0f, transform.eulerAngles.z + angle);//Повернём паука
        SpiderOrientation = _spiderOrientation;

        transform.position = connectionPoint + spiderOffset * spiderOrientation;//Расположить паука
        StopMoving();
        if (spiderOrientation.y < 0 || Mathf.Abs(spiderOrientation.x) > Mathf.Abs(spiderOrientation.y))
            rigid.gravityScale = 0f;
        else
            rigid.gravityScale = 1f;
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
        rigid.AddForce(new Vector2(jumpForce*0.5f, jumpForce));
        //rigid.velocity = new Vector2((int)orientation * speed, rigid.velocity.y);//Сразу придать персонажу максимальную горизонтальную скорость для преодоления препятствия
    }

    #endregion //movement

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
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
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
                    if (currentTarget == null)
                        break;
                    if ((transform.position - prevPosition).sqrMagnitude < speed * Time.fixedDeltaTime / 10f && !avoid && currentTarget != mainTarget)
                    {
                        StartCoroutine(AvoidProcess());
                    }
                    break;
                }
            case BehaviourEnum.patrol:
                {
                    if (currentTarget == null)
                        break;
                    if ((transform.position-prevPosition).sqrMagnitude<speed*Time.fixedDeltaTime/10f  && !avoid)
                    {
                        StartCoroutine(AvoidProcess());
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
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        BecomeAgressive();
        JumpDown();//Сбросить паука со стены ударом
    }

    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        jumping = false;
        avoid = false;
        gameObject.layer = LayerMask.NameToLayer("character");
        if (sight != null ? sight.enabled : false)
            sight.RotateLocal(0f);
        StopCoroutine(BecomeCalmProcess());
    }

    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        gameObject.layer = LayerMask.NameToLayer("character");
        jumping = false;
        avoid = false;
        if (neutral)
        {
            neutral = false;//Паук теперь всегда будет нападать на игрока и будет искать его
            sight.enabled = true;
            sight.ChangeSightMod();
        }
        StopCoroutine(BecomeCalmProcess());
        prevTargetPosition = new EVector3(Vector3.zero);
        wallCheck.RemoveWallType("character");
        if (sight != null)
        {
            sight.WhatToSight = LayerMask.GetMask("ground");
            sight.SetSightMod(true);
        }
    }

    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        jumping = false;
        avoid = false;
        wallCheck.RemoveWallType("character");
        gameObject.layer = LayerMask.NameToLayer("characterWithoutPlatform");
        if (sight != null ? sight.enabled : false)
        {
            sight.WhatToSight = LayerMask.GetMask("character", "ground");
            sight.RotateLocal(0f);
            sight.SetSightMod(false);
        }
        StopCoroutine(BecomeCalmProcess());
           
    }

    /// <summary>
    /// Процесс успокаивания персонажа
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator BecomeCalmProcess()
    {
        //calmDown = true;
        yield return new WaitForSeconds(beCalmTime);
        if (Vector2.SqrMagnitude((Vector2)transform.position - beginPosition) > minDistance * minDistance)
            GoHome();
        wallCheck.WhatIsWall.Add("character");
        if (sight != null)
        {
            sight.WhatToSight = LayerMask.GetMask("character", "ground");
            sight.SetSightMod(false);
        }
    }

    #region behaviourActions

    /// <summary>
    /// Спокойное поведение
    /// </summary>
    protected override void CalmBehaviour()
    {
        base.CalmBehaviour();
        if ((Vector2.Distance(waypoint, transform.position) < attackDistance) || (wallCheck.WallInFront() || !(precipiceCheck.WallInFront())))
        {
            Turn();
            Patrol();
        }
        else
        {
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(waypoint.x - transform.position.x)));
        }
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehaviour()
    {
        base.AgressiveBehaviour();
        if (mainTarget != null && employment > 2)
        {
            Vector3 targetPosition = mainTarget.transform.position;
            if (waypoints == null)
            {
                if (Vector2.Distance(targetPosition, transform.position) > attackDistance)
                {
                    if (!wallCheck.WallInFront() && (precipiceCheck.WallInFront()))
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
                    else if ((targetPosition - transform.position).x * (int)orientation < 0f)
                        Turn();
                    else
                    {
                        if (Vector2.SqrMagnitude(transform.position - mainTarget.transform.position) > minDistance * minDistance * 4f &&
                            (Vector2.SqrMagnitude(mainTarget.transform.position - prevTargetPosition) > minDistance * minDistance || !prevTargetPosition.exists))
                        {
                            prevTargetPosition = new EVector3(mainTarget.transform.position, true);
                            waypoints = FindPath(targetPosition, maxAgressivePathDepth);
                            if (waypoints != null)
                                gameObject.layer = LayerMask.NameToLayer("characterWithoutPlatform");
                            else
                                StopMoving();
                        }
                        else
                            StopMoving();
                    }
                }
                else
                {
                    if ((targetPosition - transform.position).x * (int)orientation < 0f)
                        Turn();
                    Attack();
                }

                if (spiderOrientation.y < 0 || spiderOrientation.y<Mathf.Abs(spiderOrientation.x))
                    if (Mathf.Abs(targetPosition.x - transform.position.x) < attackDistance / 2f)
                        JumpDown();

            }
            else
            {
                if (waypoints.Count > 0)
                {
                    if (currentTarget == null)
                    {
                        currentTarget = new GameObject("SpiderTarget");
                        currentTarget.transform.position = waypoints[0].cellPosition;
                    }

                    targetPosition = currentTarget.transform.position;
                    Vector3 direction = targetPosition - transform.position;
                    float projectionLength = Vector2.Dot(direction, movementDirection);
                    if (Mathf.Abs(projectionLength) > spiderOffset || Vector2.SqrMagnitude(transform.position-targetPosition)<spiderOffset*2f)
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(projectionLength)));
                    //else
                    //{
                        //StopMoving();
                        //transform.position += movementDirection * proectionLength;
                    //}
                    if (!jumping)
                    {
                        if (Vector2.SqrMagnitude((Vector2)mainTarget.transform.position - prevTargetPosition) > navCellSize * navCellSize)
                        {
                            FindPath(mainTarget.transform.position, maxAgressivePathDepth);
                            prevTargetPosition = new EVector3(mainTarget.transform.position, true);
                            return;
                        }
                    }

                    if (currentTarget != mainTarget && Vector3.SqrMagnitude(currentTarget.transform.position-transform.position) < navCellSize*navCellSize/4f)
                    {
                        if (jumping)
                            jumping = false;
                        NavigationCell currentWaypoint = waypoints[0];
                        Destroy(currentTarget);
                        waypoints.RemoveAt(0);

                        if (waypoints.Count > 2)
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
                                //Если путь прямой, несложный, то паук может самостоятельно добраться до игрока, не используя маршрута, и атаковать его
                                waypoints = null;
                                prevTargetPosition = EVector3.zero;
                                return;
                            }
                        }
                        

                        if (waypoints.Count == 0)
                        {
                            waypoints = null;
                            prevTargetPosition = EVector3.zero;
                            return;
                        }
                        else
                        {
                            NavigationCell nextWaypoint = waypoints[0];
                            //Продолжаем следование
                            currentTarget = new GameObject("SpiderTarget");
                            currentTarget.transform.position = nextWaypoint.cellPosition;
                            if (currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb).connectionType==NavCellTypeEnum.jump)
                            {
                                //Перепрыгиваем препятствие
                                Vector3 pos = transform.position;
                                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x)));
                                transform.position = new Vector3(currentWaypoint.cellPosition.x + (int)orientation*navCellSize / 2f, pos.y, pos.z);
                                jumping = true;
                                Jump();
                            }
                            else if (spiderOrientation.y<0f)
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
                        if (wallCheck.WallInFront())
                        {
                            RaycastHit2D hit = Physics2D.Raycast(transform.position, (int)orientation * movementDirection, navCellSize, LayerMask.GetMask(gLName));
                            if (hit)
                            {
                                ChangeOrientation(hit.collider);
                            }
                        }
                        else if (!precipiceCheck.WallInFront())
                        {
                            RaycastHit2D hit = Physics2D.Raycast(transform.position - (int)orientation * Vector3.right * navCellSize / 2f, -spiderOrientation, navCellSize, LayerMask.GetMask(gLName));
                            if (hit)
                            {
                                ChangeOrientation(hit.collider);
                            }
                        }
                    }
                }
            }
        }
    }

    protected override void PatrolBehaviour()
    {
        if (waypoints != null ? waypoints.Count > 0 : false)
        {
            if (currentTarget == null)
            {
                currentTarget = new GameObject("SpiderTarget");
                currentTarget.transform.position = waypoints[0].cellPosition;
            }


            Vector3 targetPosition = currentTarget.transform.position;
            Vector3 direction = targetPosition - transform.position;
            float proectionLength = Vector2.Dot(direction, movementDirection);
            if (Mathf.Abs(proectionLength) > spiderOffset || Vector2.SqrMagnitude(transform.position - targetPosition) < spiderOffset * 2f)
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(proectionLength)));
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
            if (currentTarget != mainTarget && Vector3.SqrMagnitude(currentTarget.transform.position-transform.position) < navCellSize*navCellSize/4f)
            {
                if (jumping)
                    jumping = false;
                NavigationCell currentWaypoint = waypoints[0];  
                Destroy(currentTarget);
                waypoints.RemoveAt(0);
                if (waypoints.Count == 0)
                {
                    //Достигли конца маршрута
                    if (Vector3.Distance(beginPosition, currentWaypoint.cellPosition) < navCellSize)
                    {
                        transform.position = beginPosition;
                        BecomeCalm();
                        return;
                    }
                    else
                        GoHome();//Никого в конце маршрута не оказалось, значит, возвращаемся домой
                }
                else
                {
                    NavigationCell nextWaypoint = waypoints[0];
                    //Продолжаем следование
                    currentTarget = new GameObject("SpiderTarget");
                    currentTarget.transform.position = nextWaypoint.cellPosition;
                    if (currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb).connectionType == NavCellTypeEnum.jump)
                    {
                        //Перепрыгиваем препятствие
                        Vector3 pos = transform.position;
                        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x)));
                        transform.position = new Vector3(currentWaypoint.cellPosition.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                        jumping = true;
                        Jump();
                    }
                    else if (spiderOrientation.y<0f)
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
                if (wallCheck.WallInFront())
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, (int)orientation * movementDirection, navCellSize, LayerMask.GetMask(gLName));
                    if (hit)
                    {
                        ChangeOrientation(hit.collider);
                    }
                }
                else if (!precipiceCheck.WallInFront())
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position - (int)orientation * movementDirection * navCellSize / 2f, -spiderOrientation, navCellSize, LayerMask.GetMask(gLName));
                    if (hit)
                    {
                        ChangeOrientation(hit.collider);
                    }
                }
            }
        }
    }

    protected override void GoToThePoint(Vector2 targetPosition)
    {
        base.GoToThePoint(targetPosition);
        NavigationSystem navSystem = SpecialFunctions.statistics.navSystem;
        navCellSize = navSystem.cellSize.magnitude;
    }

    #endregion //behaviourActions

    /// <summary>
    /// Функция, которая строит маршрут для паука
    /// </summary>
    /// <param name="endPoint">точка назначения</param>
    ///<param name="maxDepth">Максимальная сложность маршрута</param>
    /// <returns>Навигационные ячейки, составляющие маршрут</returns>
    protected virtual List<NavigationCell> FindPath(Vector2 endPoint, int _maxDepth)
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
                //что паук обходит слишком большие дистанции, чтобы достичь игрока. Если такое случается, он должен ждать, чему соответствует несуществование подходящего маршрута
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

    #region optimization

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

    /// <summary>
    /// Включить собственный хитбокс
    /// </summary>
    protected virtual void EnableSelfHitBox()
    {
        selfHitBox.gameObject.SetActive(true);
    }

    /// <summary>
    /// Выключить собственный хитбокс
    /// </summary>
    protected virtual void DisableSelfHitBox()
    {
        selfHitBox.gameObject.SetActive(false);
    }

    #endregion //optimization

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
                StartCoroutine(BecomeCalmProcess());
            //}
            //else
               //GoHome();
        }
    }

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected void HandleAttackProcess(object sender, HitEventArgs e)
    {
        //Если игрок случайно наткнулся на паука и получил урон, то паук автоматически становится агрессивным
        if (behaviour!=BehaviourEnum.agressive)
            BecomeAgressive();
    }

    #endregion //eventHandlers

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
    public void MoveForwardAction(StoryAction _action)
    {
        //Animate(new AnimationEventArgs("moveForward"));
        StartCoroutine(MoveForwardProcess());
    }

    /// <summary>
    /// Процесс нападения из засады
    /// </summary>
    IEnumerator MoveForwardProcess()
    {
        moveOut = true;
        rigid.isKinematic = true;
        immobile = true;
        yield return new WaitForSeconds(1f);
        moveOut = false;
        rigid.isKinematic = false;
        immobile = false;
    }

    /// <summary>
    /// Выдвинуться вперёд (из некоторого препятствия)
    /// </summary>
    protected void MoveOut()
    {
        transform.position += new Vector3(speed * Time.fixedDeltaTime*(int)orientation, 0f, 0f);
    }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public override List<string> actionNames()
    {
        return new List<string>(){ };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() { { "moveForward", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() { { "moveForward", new List<string>() { } } };
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
    /// <param name="point2">вторая точка, принадлежащая щаланной прямой</param>
    /// <param name="fromPoint">точка, откуда мы ищем точку пересечения</param>
    /// <returns>Точка пересечения</returns>
    protected Vector2 GetConnectionPoint(Vector2 point1, Vector2 point2, Vector2 fromPoint)
    {
        Vector2 connectionPoint = Vector2.zero;//Точка пересечения 2-х прямых
        Vector2 normal = GetNormal(point1, point2);//Нормаль рассматриваемой поверхности
        //if (Vector2.Angle(spiderOrientation, normal) < minAngle)
            //return Vector2.zero;
        if (point1.x - point2.x == 0)
            connectionPoint = new Vector2(point1.x, normal.y / normal.x * (point1.x - transform.position.x) + transform.position.y);
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
            connectionPoint = (Vector2.SqrMagnitude(connectionPoint - point1) < Vector2.SqrMagnitude(connectionPoint - point2) ? point1 : point2);

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
        string gLName = "ground";

        Vector2 direction = (surfacePoint2 - surfacePoint1).normalized;
        Vector2 normal = new Vector2(1, 0);
        if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(normal, direction)), 1f))
            normal = new Vector2(0, 1);
        else
            normal = (normal - Vector2.Dot(normal, direction) * direction).normalized;
        if (Physics2D.Raycast((surfacePoint2 + surfacePoint1) / 2f + normal / 10f, normal, .02f, LayerMask.GetMask(gLName)))
            normal *= -1;

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

    #endregion //other

}
