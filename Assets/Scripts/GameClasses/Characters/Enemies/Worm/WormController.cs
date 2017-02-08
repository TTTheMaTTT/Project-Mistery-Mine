using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер червя
/// </summary>
public class WormController : AIController
{

    #region consts

    protected const float areaRadius = 3f;//В пределах какого расстояния от своей начальной позиции осуществляет свою деятельность червь

    protected const float undergroundWaitTime = 3f;//Сколько времени червь ждёт, чтобы совершить атаку из-под земли
    protected const float usualToUndergroundThinkTime = 5f;//Как часто червь задумывается о переходе в подземный режим
    protected const float fromUndergroundTime = 2f;//Сколько времени червь вылезает из земли
    protected const float usualToUndergroundProbability = .5f;//Вероятность перехода в подземный режим из обычного
    protected const float undergroundToUndergroundProbability = .66f;//Вероятность того, что червь снова перейдёт в режим копания после совершения атаки из-под земли

    protected const float usualColSizeX = 0.23f, usualColSizeY = .06f, usualColOffsetX = -0.01f, usualColOffsetY = 0f;//Параметры коллайдера червя в обычном режиме
    protected const float undergroundColSizeX = .1f, undergroundColSizeY = .21f, undergroundColOffsetX = .007f, undergroundColOffsetY = .09f;//Параметры коллайдера червя при атаке из-под земли
    protected const float wormOffset = .04f, wormLength = 1f;

    #endregion //consts

    #region fields

    protected BoxCollider2D col;//Коллайдер персонажа
    protected WallChecker precipiceCheck;//Индикатор пропасти
    protected Hearing hearing;//Слух персонажа

    #endregion //fields

    #region parametres

    protected WormStateEnum wormState = WormStateEnum.usualState;//В каком режиме находится червь
    protected WormStateEnum WormState
    {
        set
        {
            WormStateEnum preWormState = wormState;
            wormState = value;
            switch (value)
            {
                case WormStateEnum.usualState:
                    {
                        employment = Mathf.Clamp(employment + 1, 0, maxEmployment);
                        if (!optimized)
                        {
                            if (preWormState == WormStateEnum.undergroundState)
                                StartCoroutine("GoFromUndergroundProcess");
                            else
                            {
                                col.enabled = true;
                                rigid.isKinematic = false;
                            }
                            anim.gameObject.SetActive(true);
                        }
                        col.size = new Vector2(usualColSizeX, usualColSizeY);
                        col.offset = new Vector2(usualColOffsetX, usualColOffsetY);
                        if (preWormState == WormStateEnum.undergroundState)
                        {
                            Animate(new AnimationEventArgs("groundInteraction", "from", 0));
                            if (behavior == BehaviorEnum.agressive)
                                StartCoroutine("GoIntoGroundThinkProcess");
                        }
                        precipiceCheck.enabled = true;
                        undergroundAttackTimes = 0;
                        break;
                    }
                case WormStateEnum.undergroundState:
                    {
                        StopMoving();
                        StopCoroutine("GoIntoGroundThinkProcess");
                        col.enabled = false;
                        rigid.isKinematic = true;
                        precipiceCheck.enabled = false;
                        if (hearing != null)
                            hearing.enabled = false;
                        if (preWormState == WormStateEnum.usualState)
                        {
                            Animate(new AnimationEventArgs("groundInteraction", "in", 0));
                        }
                        else if (preWormState == WormStateEnum.upState)
                            Animate(new AnimationEventArgs("groundInteraction", "up", 0));
                        if (preWormState != WormStateEnum.undergroundState)
                            StartCoroutine("UndergroundProcess");
                        break;
                    }
                case WormStateEnum.upState:
                    {
                        if (!optimized)
                        {
                            col.enabled = true;
                            rigid.isKinematic = false;
                        }
                        col.size = new Vector2(undergroundColSizeX, undergroundColSizeY);
                        col.offset = new Vector2(undergroundColOffsetX, undergroundColOffsetY);
                        employment = Mathf.Clamp(employment + 1, 0, maxEmployment);
                        break;
                    }
                default:
                    break;
            }
        }
    }
    public WormStateEnum wState { get { return wormState; } }

    [SerializeField]protected HitParametres upAttackParametres;//Параметры атаки из-под земли, производимой червём

    protected override float attackDistance { get { return .23f; } }//На каком расстоянии должен стоять червь в обычном режиме, чтобы решить атаковать
    protected bool waitingUnderground = false;//Если true - значит червь всё ещё находится под землёй и не может предпринять действия на поверхности
    protected int undergroundAttackTimes = 0;//Сколько раз подряд червь атаковал из-под земли

    public override bool Waiting
    {
        get
        {
            return base.Waiting;
        }

        set
        {
            base.Waiting = value;
            if (wormState == WormStateEnum.undergroundState)
            {
                if (value)
                {
                    StopUndergroundProcess();
                    undergroundAttackTimes = 0;
                }
                else
                {
                    StartCoroutine("UndergroundProcess");
                    undergroundAttackTimes = 0;
                }
            }
            else if (wormState == WormStateEnum.usualState ? behavior == BehaviorEnum.agressive : false)
            {
                if (value)
                    StopCoroutine("GoIntoGroundThinkProcess");
                else
                    StartCoroutine("GoIntoGroundThinkProcess");
            }
        }
    }

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

    #endregion //parametres

    protected override void FixedUpdate()
    {
        if (!immobile)
            base.FixedUpdate();
    }

    protected override void Update()
    {
        base.Update();
        if (wormState != WormStateEnum.undergroundState)
            Animate(new AnimationEventArgs("groundMove", wormState == WormStateEnum.upState ? "upState" : "", 0));
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        indicators = transform.FindChild("Indicators");
        if (indicators != null)
        {
            precipiceCheck = indicators.FindChild("PrecipiceCheck").GetComponent<WallChecker>();
            hearing = indicators.GetComponentInChildren<Hearing>();
            if (hearing != null)
                hearing.hearingEventHandler += HandleHearingEvent;
        }
        col = GetComponent<BoxCollider2D>();
        base.Initialize();

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            areaTrigger.InitializeAreaTrigger();
        }

        BecomeCalm();
    }

    #region movement

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = !precipiceCheck.WallInFront ? new Vector2(0f, rigid.velocity.y) : (new Vector2((int)orientation * speed, rigid.velocity.y));
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    protected override void Turn()
    {
        base.Turn();
        precipiceCheck.SetPosition(0f, (int)orientation);
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    /// <param name="_orientation">В какую сторону должен смотреть персонаж</param>
    protected override void Turn(OrientationEnum _orientation)
    {
        base.Turn(_orientation);
        precipiceCheck.SetPosition(0f, (int)orientation);
    }

    /// <summary>
    /// Подземное перемещение
    /// </summary>
    /// <param name="surfPos">Точка поверхности, к которой надо переместиться</param>
    protected virtual void UndergroundMove(Vector2 surfPos)
    {
        transform.position = surfPos + Vector2.up * wormOffset;
    }

    /// <summary>
    /// Процесс перемещения под землёй (точнее выжидания под землёй без предпринятия каких либо действий на поверхности)
    /// </summary>
    protected virtual IEnumerator UndergroundProcess()
    {
        waitingUnderground = true;
        yield return new WaitForSeconds(undergroundWaitTime);
        waitingUnderground = false;
    }

    /// <summary>
    /// Завершить процесс ожидания под землёй
    /// </summary>
    protected virtual void StopUndergroundProcess()
    {
        waitingUnderground = false;
        StopCoroutine("UndergroundProcess");
    }

    /// <summary>
    /// Процесс, при котором червь может случайно перейти под землю через заданные промежутки времени
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator GoIntoGroundThinkProcess()
    {
        while (true)
        {
            yield return new WaitForSeconds(usualToUndergroundThinkTime);
            float rand = Random.Range(0f, 1f);
            if (rand < usualToUndergroundProbability && !immobile && employment >= 8)
                WormState = WormStateEnum.undergroundState;
        }
    }

    /// <summary>
    /// Процесс вылезания из-под земли
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator GoFromUndergroundProcess()
    {
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(fromUndergroundTime / 2f);
        col.enabled = true;
        rigid.isKinematic = false;
        if (hearing != null && behavior != BehaviorEnum.agressive)
            hearing.enabled = true;
        yield return new WaitForSeconds(fromUndergroundTime / 2f);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);

    }

    #endregion //movement


    #region attack

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        StartCoroutine("AttackProcess");
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        if (wormState == WormStateEnum.undergroundState)
        {
            anim.gameObject.SetActive(true);
            Animate(new AnimationEventArgs("attack", "UpAttackPreparation", Mathf.RoundToInt(10 * upAttackParametres.preAttackTime)));
            yield return new WaitForSeconds(upAttackParametres.preAttackTime);
            WormState = WormStateEnum.upState;
            Animate(new AnimationEventArgs("attack", "UpAttack", Mathf.RoundToInt(10 * (upAttackParametres.preAttackTime / 2f + upAttackParametres.actTime + upAttackParametres.endAttackTime))));
            undergroundAttackTimes++;
            yield return new WaitForSeconds(upAttackParametres.preAttackTime / 2f);
            hitBox.SetHitBox(new HitParametres(upAttackParametres));
            yield return new WaitForSeconds(upAttackParametres.actTime + upAttackParametres.endAttackTime);
        }
        else
        {
            Animate(new AnimationEventArgs("attack", "Attack", Mathf.RoundToInt(10 * (attackParametres.preAttackTime + attackParametres.actTime))));
            yield return new WaitForSeconds(attackParametres.preAttackTime);
            hitBox.SetHitBox(new HitParametres(attackParametres));
            yield return new WaitForSeconds(attackParametres.actTime + attackParametres.endAttackTime);
        }
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, bool _microstun = true)
    {
        if (GetBuff("StunnedProcess") != null || GetBuff("FrozenProcess") != null)
            base.TakeDamage(damage, _dType, _microstun);
        else
            base.TakeDamage(damage, _dType, false);
    }

    /// <summary>
    /// Функция получения урона, которая, возможно, игнорирует состояние инвула
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, bool ignoreInvul, bool _microstun)
    {
        if (GetBuff("StunnedProcess") != null || GetBuff("FrozenProcess") != null)
            base.TakeDamage(damage, _dType, _microstun);
        else
            base.TakeDamage(damage, _dType, false);
    }

    /// <summary>
    /// Функция смерти
    /// </summary>
    protected override void Death()
    {
        if (wormState != WormStateEnum.usualState)
            WormState = WormStateEnum.usualState;
        base.Death();
    }

    #endregion //attack

    /// <summary>
    /// Провести анализ окружающей обстановки
    /// </summary>
    protected override void Analyse()
    {
        if (employment < 8)
            return;
        Vector2 pos = transform.position;
        switch (behavior)
        {
            case BehaviorEnum.agressive:
                {
                    Vector2 direction = mainTarget - pos;
                    if (wormState != WormStateEnum.undergroundState)
                    {
                        RaycastHit2D hit = Physics2D.Raycast(pos, direction.normalized, direction.magnitude, LayerMask.GetMask(gLName));
                        if (hit)
                        {
                            if (direction.magnitude > sightOffset / 2f)
                            {
                                GoToThePoint(mainTarget);
                                if (behavior != BehaviorEnum.agressive)
                                    StartCoroutine("BecomeCalmProcess");
                            }
                        }
                    }

                    if (Vector2.SqrMagnitude(mainTarget - (Vector2)beginPosition) > areaRadius * areaRadius)
                        ChangeMainTarget();

                    break;
                }
            case BehaviorEnum.patrol:
                {
                    if (wormState != WormStateEnum.undergroundState)
                    {
                        RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * (int)orientation * Vector2.right, (int)orientation * Vector2.right, sightRadius, LayerMask.GetMask(gLName, cLName));
                        if (hit)
                        {
                            if (enemies.Contains(hit.collider.gameObject.tag))
                            {
                                MainTarget = new ETarget(hit.collider.transform);
                                BecomeAgressive();
                            }
                        }

                        if (loyalty == LoyaltyEnum.ally ? !mainTarget.exists && wormState == WormStateEnum.usualState : false) //Если нет основной цели и стоящий на земле паук - союзник героя, то он следует к нему
                        {
                            float sqDistance = Vector2.SqrMagnitude(beginPosition - pos);
                            if (sqDistance < allyDistance)
                            {
                                StopMoving();
                                BecomeCalm();
                                Turn(beginOrientation);
                            }
                        }
                    }

                    break;
                }

            case BehaviorEnum.calm:
                {
                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * (int)orientation * Vector2.right, (int)orientation * Vector2.right, sightRadius, LayerMask.GetMask(gLName, cLName));
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
                            BecomePatrolling();
                            currentTarget = beginPosition;
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
    }

    #region behavior

    /// <summary>
    /// Обновить информацию, важную для моделей поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        StopCoroutine("BecomeCalmProcess");
        StopUndergroundProcess();
        StopCoroutine("GoIntoGroundThinkProcess");
        undergroundAttackTimes = 0;
        prevTargetPosition = EVector3.zero;
        if (wormState == WormStateEnum.undergroundState)
            StartCoroutine("UndergroundProcess");
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        if (hearing != null && wormState == WormStateEnum.usualState)
            hearing.enabled = true;
    }

    /// <summary>
    /// Перейти в агрессивное состояние
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        if (loyalty == LoyaltyEnum.neutral)
        {
            Loyalty = LoyaltyEnum.enemy;
        }
        if (wormState == WormStateEnum.usualState)
            StartCoroutine("GoIntoGroundThinkProcess");
        if (hearing != null)
            hearing.enabled = false;//В агрессивном состоянии персонажу не нужен слух
    }

    /// <summary>
    /// Перейти в состояние патрулирования
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        if (hearing != null && wormState==WormStateEnum.usualState)
            hearing.enabled = true;
    }

    /// <summary>
    /// Процесс успокаивания персонажа
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator BecomeCalmProcess()
    {
        yield return new WaitForSeconds(beCalmTime);
        GoHome();
    }

    /// <summary>
    /// Среагировать на услышанный боевой клич
    /// </summary>
    /// <param name="cryPosition">Место, откуда издался клич</param>
    public override void HearBattleCry(Vector2 cryPosition)
    {
    }

    /// <summary>
    /// Сменить главную цель
    /// </summary>
    protected override void ChangeMainTarget()
    {
        Transform prevTarget = mainTarget.transform;
        MainTarget = ETarget.zero;
        Transform obj = SpecialFunctions.battleField.GetNearestCharacter(transform.position, loyalty == LoyaltyEnum.ally, prevTarget);
        if (obj != null)
        {
            MainTarget = new ETarget(obj);
            GoToThePoint(mainTarget);
            if (!currentTarget.exists) 
                BecomeAgressive();
        }
        else
        {
            GoHome();
        }
        if (waitingUnderground)
        {
            StopUndergroundProcess();
            StartCoroutine("UndergroundProcess");
        }
    }

    /// <summary>
    /// Выдвинуться к указанной точке (если это возможно)
    /// </summary>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        EVector3 surfPosition = GetSurfacePointUnderPosition(targetPosition);
        if (surfPosition.exists && Vector2.SqrMagnitude(surfPosition-(Vector2)beginPosition)<areaRadius*areaRadius)
        {
            BecomePatrolling();
            currentTarget = new ETarget(surfPosition);
        }
        else
            currentTarget.exists = false;
    }

    /// <summary>
    /// Направиться к изначальной позиции
    /// </summary>
    protected override void GoHome()
    {
        MainTarget = ETarget.zero;
        BecomePatrolling();
        currentTarget = beginPosition;
    }

    /// <summary>
    /// Спокойное поведение
    /// </summary>
    protected override void CalmBehavior()
    {
        //if (wormState != WormStateEnum.usualState)
            //WormState = WormStateEnum.usualState;
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        base.AgressiveBehavior();
        if (!mainTarget.exists)
        {
            GoHome();
            return;
        }
        Vector2 targetPosition = mainTarget;
        Vector2 pos = transform.position;

        switch (wormState)
        {
            case WormStateEnum.usualState:
                {

                    #region usualState

                    if (waiting)
                    {
                        #region waiting

                        float sqDistance = Vector2.SqrMagnitude(targetPosition - pos);
                        if (sqDistance < waitingNearDistance * waitingNearDistance)
                        {
                            if (employment >= 8)
                            {
                                if (precipiceCheck.WallInFront)
                                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(pos.x - targetPosition.x)));
                                else if ((targetPosition - pos).x * (int)orientation < 0f)
                                    Turn();
                            }
                        }
                        else if (sqDistance < waitingFarDistance * waitingFarDistance)
                        {
                            StopMoving();
                            if ((int)orientation * (targetPosition - pos).x < 0f && employment >= 8)
                                Turn();
                        }
                        else
                        {
                            if (employment >= 8)
                            {
                                if (precipiceCheck.WallInFront)
                                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
                                else if ((targetPosition - pos).x * (int)orientation < 0f)
                                    Turn();
                            }
                        }

                        #endregion //waiting
                    }
                    else
                    {
                        #region active

                        if (employment >= 8)
                        {

                            if (Vector2.SqrMagnitude(targetPosition - pos) > attackDistance * attackDistance)
                            {
                                if (precipiceCheck.WallInFront)
                                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
                                else if ((targetPosition - pos).x * (int)orientation < 0f)
                                    Turn();
                            }
                            else
                            {
                                if ((targetPosition - pos).x * (int)orientation < 0f)
                                    Turn();
                                StopMoving();
                                Attack();
                            }
                        }

                        #endregion //active

                    }

                    #endregion //usualState

                    break;
                }
            case WormStateEnum.undergroundState:
                {

                    #region undergroundState

                    if (!waitingUnderground)
                    {
                        if (employment > 9)
                        {
                            //Решим, будем ли мы атаковать, или просто выйдем из-под земли
                            float rand = Random.Range(0f, 1f);
                            if (rand < Mathf.Pow(undergroundToUndergroundProbability, undergroundAttackTimes))
                                employment = Mathf.Clamp(employment - 1, 0, maxEmployment);//Готовимся к атаке
                            else
                                WormState = WormStateEnum.usualState;//либо выходим из земли
                        }
                        if (employment >= 8)
                        {
                            EVector3 targetSurfPoint = GetSurfacePointUnderPosition(mainTarget);
                            if (targetSurfPoint.exists)
                            {
                                UndergroundMove(targetSurfPoint);
                                Attack();
                            }
                        }
                    }

                    #endregion //undergroundState

                    break;
                }
            case WormStateEnum.upState:
                {
                    if (employment >= 8)
                        WormState = WormStateEnum.undergroundState;
                    break;
                }

            default:
                break;
        }
    }

    /// <summary>
    /// Поведение патрулирования
    /// </summary>
    protected override void PatrolBehavior()
    {
        if (!currentTarget.exists)
        {
            GoHome();
            return;
        }

        switch(wormState)
        {
            case WormStateEnum.usualState:
                {
                    if (employment > 8)
                    {
                        Vector2 pos = transform.position;
                        if (Vector2.SqrMagnitude(currentTarget-pos)<minCellSqrMagnitude)
                        {
                            if (Vector2.SqrMagnitude(currentTarget - (Vector2)beginPosition) < minCellSqrMagnitude)
                                BecomeCalm();
                            else
                                GoHome();
                            return;
                        }
                        if (!prevTargetPosition.exists ? true : Vector2.SqrMagnitude(prevTargetPosition - (Vector2)currentTarget) < minCellSqrMagnitude)
                        {
                            Collider2D col1 = GetUnderCollider(pos);
                            Collider2D col2 = GetUnderCollider(currentTarget);
                            bool a1 = col1 && col2 ? Vector2.SqrMagnitude(col1.transform.position - col2.transform.position) < minCellSqrMagnitude : false;
                            bool a2 = Mathf.Abs(pos.y - currentTarget.y) < wormLength;
                            bool a3 = !Physics2D.Raycast(pos, (currentTarget - pos).normalized, (currentTarget - pos).magnitude, LayerMask.GetMask(gLName));
                            if (a1 && a2 && a3)//До текущей цели можно добраться, ползая
                                prevTargetPosition = new EVector3(currentTarget);
                            else
                            {
                                //До текущей цели можно добраться только сквозь землю
                                prevTargetPosition = EVector3.zero;
                                WormState = WormStateEnum.undergroundState;
                                return;
                            }
                        }

                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.x - pos.x)));

                    }
                    break;
                }
            case WormStateEnum.undergroundState:
                {
                    if (!waitingUnderground)
                    {
                        EVector3 vect = GetSurfacePointUnderPosition(currentTarget);
                        if (vect.exists)
                        {
                            UndergroundMove(vect);
                            WormState = WormStateEnum.usualState;
                        }
                    }

                    break;
                }
            case WormStateEnum.upState:
                {
                    WormState = WormStateEnum.undergroundState;
                    break;
                }
            default:
                break;
        }

    }

    #endregion //behavior

    /// <summary>
    /// Вернуть тип карты, используемой персонажем
    /// </summary>
    public override NavMapTypeEnum GetMapType()
    {
        return NavMapTypeEnum.crawl;
    }

    #region optimization

    /// <summary>
    /// Включить риджидбоди
    /// </summary>
    protected override void EnableRigidbody()
    {
        if (wormState!=WormStateEnum.undergroundState)
            rigid.isKinematic = false;
    }

    /// <summary>
    /// Включить все коллайдеры в персонаже
    /// </summary>
    protected override void EnableColliders()
    {
        if (wormState != WormStateEnum.undergroundState)
            col.enabled = true;
    }

    /// <summary>
    /// Выключить все коллайдеры в персонаже
    /// </summary>
    protected override void DisableColliders()
    {
        col.enabled = false;
    }

    /// <summary>
    /// Включить визуальное отображение персонажа
    /// </summary>
    protected override void EnableVisual()
    {
        if (wormState != WormStateEnum.undergroundState)
            anim.gameObject.SetActive(true);
    }

    /// <summary>
    /// Функция реализующая анализ окружающей персонажа обстановки, когда тот находится в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
        if (behavior != BehaviorEnum.calm)
        {
            if (!currentTarget.exists)
            {
                GoHome();
                WormState = WormStateEnum.usualState;
                WormState = WormStateEnum.undergroundState;
                return;
            }
            if (wormState == WormStateEnum.undergroundState)
            {
                if (!waitingUnderground)
                {
                    UndergroundMove(currentTarget);
                    WormState = WormStateEnum.usualState;
                    if (Vector2.SqrMagnitude(currentTarget - (Vector2)beginPosition) < minCellSqrMagnitude)
                    {
                        BecomeCalm();
                        Turn(beginOrientation);
                    }
                    else
                        currentTarget = ETarget.zero;
                }
            }
            else
            {
                WormState = WormStateEnum.undergroundState;
            }
        }
    }

    /// <summary>
    /// Функция, которая переносит персонажа в ту позицию, в которой он может нормально функционировать в оптимизированной версии 
    /// </summary>
    protected override void GetOptimizedPosition()
    {
        if (wormState!=WormStateEnum.usualState && !Physics2D.Raycast(transform.position, Vector2.down, navCellSize, LayerMask.GetMask(gLName)))
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
                    transform.position = hit.point + Vector2.up * wormOffset;
                }
            }
        }
        if (wormState != WormStateEnum.undergroundState && behavior==BehaviorEnum.patrol)
            WormState = WormStateEnum.undergroundState;
    }

    /// <summary>
    /// Процесс оптимизированного движения по маршруту
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator PathPassOptProcess()
    {
        yield return 0;
    }

    #endregion //optimization

    #region id

    public override EnemyData GetAIData()
    {
        return new WormData(this);
    }

    /// <summary>
    /// Настроить персонажа в соответствии с загруженными данными
    /// </summary>
    public override void SetAIData(EnemyData eData)
    {
        if (eData != null ? !(eData is WormData) : true)
            return;
        WormData wData = (WormData)eData;
        transform.position = wData.position;
        if (transform.localScale.x * wData.orientation < 0f)
            Turn((OrientationEnum)wData.orientation);

        string behaviorName = wData.behavior;
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
                    break;
                }
            case "patrol":
                {
                    BecomePatrolling();
                    break;
                }
            default:
                {
                    BecomeCalm();
                    break;
                }
        }
        WormState = wData.wormState == "underground" ? WormStateEnum.undergroundState : wData.wormState == "usual" ? WormStateEnum.usualState : WormStateEnum.upState;

        TargetData currentTargetData = wData.currentTargetData;
        TargetData mainTargetData = wData.mainTargetData;

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
        Health = wData.health;
    }

    #endregion //id

    #region other

    /// <summary>
    /// Функция, которая определяет, есть ли под заданной точкой поверхность земли, и если есть, то возвращает точку поверхности под заданной позицией
    /// </summary>
    /// <param name="pos">Заданная позиция</param>
    protected EVector3 GetSurfacePointUnderPosition(Vector2 pos)
    {
        EVector3 vect = EVector3.zero;
        Collider2D hCol = GetUnderCollider(pos);
        if (!hCol)
            return vect;
        Vector2[] colPoints = GetColliderPoints(hCol);

        if (colPoints != null? colPoints.Length <= 0: true)
            return vect;

        Vector2 connectionPoint = Vector2.zero;

        //Найдём ту сторону коллайдера, которая имеет нормаль, слабо отличающуюся от (0,1), и расстояние до которой от заданной позиции является наименьшим
        float mDistance = Mathf.Infinity;
        int pointIndex = -1;
        for (int i = 0; i < colPoints.Length; i++)
        {
            Vector2 point1 = colPoints[i];
            Vector2 point2 = i < colPoints.Length - 1 ? colPoints[i + 1] : colPoints[0];
            Vector2 normal = GetNormal(point1, point2, hCol);
            if (Mathf.Abs(Vector2.Angle(Vector2.up, normal)) <= minAngle)
            {
                Vector2 _connectionPoint = GetConnectionPoint(point1, point2, pos);
                float newDistance = Vector2.SqrMagnitude(_connectionPoint - pos);
                if (newDistance < mDistance)
                {
                    connectionPoint = _connectionPoint;
                    mDistance = newDistance;
                    pointIndex = i;
                }
            }
        }

        if (pointIndex < 0)
            return vect;

        vect = new EVector3(connectionPoint);
        vect.exists = true;
        return vect;
    }

    /// <summary>
    /// Возвращает коллайдер, что находится под заданной позицией
    /// </summary>
    /// <param name="pos">Заданная позиция</param>
    protected virtual Collider2D GetUnderCollider(Vector2 pos)
    {
        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, wormLength, LayerMask.GetMask(gLName));
        if (!hit)
            return null;
        return hit.collider;
    }

    /// <summary>
    /// Функция, возвращающая граничные точки простого коллайдера
    /// </summary>
    /// <param name="col">заданный коллайдер</param>
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
            connectionPoint = (Vector2.SqrMagnitude(connectionPoint - point1) < Vector2.SqrMagnitude(connectionPoint - point2) ? point1 + (point2 - point1).normalized * wormOffset :
                                                                                                                                 point2 + (point1 - point2).normalized * wormOffset);

        return connectionPoint;
    }

    /// <summary>
    /// Возвращает вектор нормали заданной поверхности земли
    /// </summary>
    /// <param name="surfacePoint1">Первая точка, заданной прямой</param>
    /// <param name="surfacePoint2">Вторая точка, заданной прямой</param>
    /// <param name="gCol">Коллайдер, который имеет рассматриваемую поверхность</param>
    /// <returns>Вектор нормали</returns>
    protected Vector2 GetNormal(Vector2 surfacePoint1, Vector2 surfacePoint2, Collider2D gCol)
    {
        Vector2 direction = (surfacePoint2 - surfacePoint1).normalized;
        Vector2 normal = new Vector2(1, 0);
        if (Mathf.Approximately(Mathf.Abs(Vector2.Dot(normal, direction)), 1f))
            normal = new Vector2(0, 1);
        else
            normal = (normal - Vector2.Dot(normal, direction) * direction).normalized;
        Vector2 _point = (surfacePoint1 + surfacePoint2) / 2f + normal * 0.02f;
        if (gCol.OverlapPoint(_point))
            normal *= -1f;

        return normal;
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

    #endregion //other

}

/// <summary>
/// Енам, характеризующий режим червя
/// </summary>
public enum WormStateEnum : byte { usualState=0, undergroundState=1, upState=2 }