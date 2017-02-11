using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер, управляющий пираньей (и прочими плавающими тварями)
/// </summary>
public class PiranhaController : AIController
{

    #region consts

    //protected const float obstNoAnalyseTime = 1f;

    protected const float fishSize = .05f;
    //protected const float maxAvoidDistance = 10f, avoidOffset = .5f;
    protected const float pushBackForce = 75f;

    //protected const float areaSizeX = 1.5f, areaSizeY = 1.5f;//Размеры прямоугольной зоны перемещения пираньи
    //protected const int clockwiseDirection = -1;//Обход контура идёт по часовой или против

    protected const float minThinkTime = .9f, maxThinkTime = 2f;//Минимальное и максимальное время "раздумий" пираньи, решающей, в какую сторону двигаться

    #endregion //consts

    #region fields

    protected WallChecker wallCheck, waterCheck;//Индикаторы земли и воды перед персонажем

    #endregion //fields

    #region parametres

    //protected FourDirectionEnum currentRectangleSide=FourDirectionEnum.up;//По какой стороне прямоугольного контура движется пиранья в данный момент
    //protected virtual float deltaAngle { get { return 12f; } }//Абсолютное значение угла отклонения курса от основного направления
    //protected virtual float rotationSpeed { get { return 5f; } }//Насколько быстро меняется угол курса
    //protected int deltaSign = 1;
    //protected bool noAnalyse = false;

    protected Vector2 currentDirection = Vector2.right;//Направление основного курса пираньи
    protected virtual Vector2 CurrentDirection
    {
        get
        {
            return currentDirection;
        }
        set
        {
            currentDirection = value;
            float angle = Mathf.Sign(currentDirection.y)*Vector2.Angle(Vector2.right, currentDirection);
            if (Mathf.Abs(angle) > 90f)
            {
                Turn(OrientationEnum.left);
                angle = -Mathf.Sign(currentDirection.y) * Vector2.Angle(Vector2.left, currentDirection);
            }
            else
                Turn(OrientationEnum.right);
            transform.eulerAngles = new Vector3(0f, 0f, angle);
            wallCheck.SetPosition(angle / 180f * Mathf.PI, (int)orientation);
            waterCheck.SetPosition(angle / 180f * Mathf.PI, (int)orientation);
        }
    }

    protected virtual float minSwimDistance { get { return .5f; } }//Минимальное и максимальное расстояния передвижений пираньи
    protected virtual float maxSwimDistance { get { return 1.5f; } }
    protected bool preparingSwim = false;//Если true, значит пиранья выбирает следующую позицию, к которой будет плыть
    protected override bool Underwater { get {return base.Underwater; } set { base.Underwater = value; if (value) rigid.gravityScale = 0f; else rigid.gravityScale = 1f;}}

    #endregion //parametres

    protected override void FixedUpdate()
    {
        if (!immobile)
            base.FixedUpdate();
    }

    protected override void Update()
    {
        base.Update();
        if (rigid.velocity.magnitude < minSpeed)
        {
            if (loyalty != LoyaltyEnum.ally)
                Animate(new AnimationEventArgs("idle"));
        }
        else
        {
            Animate(new AnimationEventArgs("fly"));
        }
    }

    protected override void Initialize()
    {
        indicators = transform.FindChild("Indicators");
        if (indicators != null)
        {
            wallCheck = indicators.FindChild("WallCheck").GetComponent<WallChecker>();
            waterCheck = indicators.FindChild("WaterCheck").GetComponent<WallChecker>();
        }

        base.Initialize();

        underWater = true;
        rigid.gravityScale = 0f;

        hitBox.AttackEventHandler += HandleAttackProcess;

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            areaTrigger.InitializeAreaTrigger();
        }

        BecomeCalm();
    }

    /// <summary>
    /// Анализировать окружающую обстановку
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
                    if (direction.magnitude > sightRadius * 0.35f)//Если враг ушёл достаточно далеко
                    {
                        RaycastHit2D hit = Physics2D.Raycast(pos, direction.normalized, direction.magnitude, LayerMask.GetMask(gLName));
                        if (hit)
                            BecomeCalm();
                    }

                    if (wallCheck.WallInFront)
                    {
                        rigid.AddForce(-currentDirection * pushBackForce / 2f);
                    }

                    break;
                }

            case BehaviorEnum.patrol:
                {

                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * currentDirection, currentDirection, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (enemies.Contains(hit.collider.gameObject.tag))
                        {
                            MainTarget = new ETarget(hit.collider.transform);
                            BecomeAgressive();
                        }
                    }

                    if (wallCheck.WallInFront)
                    {
                        rigid.AddForce(-currentDirection * pushBackForce / 2f);
                    }

                    if (loyalty == LoyaltyEnum.ally && !mainTarget.exists) //Если нет основной цели и летучая мышь - союзник героя, то она следует к нему
                    {
                        if (Vector2.SqrMagnitude(beginPosition - pos) < allyDistance)
                        {
                            StopMoving();
                            BecomeCalm();
                        }
                    }

                    break;
                }
            case BehaviorEnum.calm:
                {

                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * currentDirection, currentDirection, sightRadius, LayerMask.GetMask(gLName, cLName));
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
                            GoHome();
                        }
                        CurrentDirection = (beginPosition - pos).normalized;//Всегда быть повёрнутым к герою-союзнику
                    }
                    break;
                }
            default:
                break;
        }

    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        CurrentDirection = (currentTarget - (Vector2)transform.position).normalized;
        Vector2 targetVelocity = (!wallCheck.WallInFront && waterCheck.WallInFront) ? currentDirection * speed : Vector2.zero;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);
    }

    /// <summary>
    /// Двинуться прочь от цели
    /// </summary>
    protected override void MoveAway(OrientationEnum _orientation)
    {
        CurrentDirection = ((Vector2)transform.position - currentTarget).normalized;
        Vector2 targetVelocity = (!wallCheck.WallInFront && waterCheck.WallInFront) ? currentDirection * speed: Vector2.zero;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);
    }

    /// <summary>
    /// Остановить передвижение
    /// </summary>
    protected override void StopMoving()
    {
        rigid.velocity = Vector2.zero;
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    protected override void Turn()
    {
        base.Turn();
        wallCheck.SetPosition(transform.eulerAngles.z / 180f * Mathf.PI, (int)orientation);
        waterCheck.SetPosition(transform.eulerAngles.z / 180f * Mathf.PI, (int)orientation);
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    /// <param name="_orientation">В какую сторону должен смотреть персонаж</param>
    protected override void Turn(OrientationEnum _orientation)
    {
        base.Turn(_orientation);
        wallCheck.SetPosition(transform.eulerAngles.z / 180f * Mathf.PI, (int)orientation);
        waterCheck.SetPosition(transform.eulerAngles.z / 180f * Mathf.PI, (int)orientation);
    }

    /// <summary>
    /// Выбрать случайное направление и позицию на этом направлении, к котором будет стремиться пиранья
    /// </summary>
    protected virtual void ChooseRandomTargetPosition()
    {
        float quarter = Mathf.RoundToInt(Random.Range(0f, 1f));
        float angle = quarter*Mathf.PI + (1-2*quarter)*Random.Range(-Mathf.PI/4, Mathf.PI / 4);
        Vector2 newDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        currentTarget = new ETarget((Vector2)transform.position + newDirection * Random.Range(minSwimDistance, maxSwimDistance));
    }

    /// <summary>
    /// Процесс микростана
    /// </summary>
    protected override IEnumerator Microstun()
    {
        StopCoroutine("PrepareToNextAttackProcess");
        return base.Microstun();
    }

    /// <summary>
    /// Процесс подготовки к следующей атаке
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator PrepareToNextAttackProcess()
    {
        StopMoving();
        immobile = true;
        yield return new WaitForSeconds(attackParametres.preAttackTime+attackParametres.endAttackTime);
        if (GetBuff("StunnedProcess")==null)
            immobile = false;
    }

    /// <summary>
    /// Процесс, при котором пиранья готовится плыть к следующей случайной точке
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator PrepareToNextSwimProcess()
    {
        preparingSwim = true;
        StopMoving();
        yield return new WaitForSeconds(Random.Range(minThinkTime, maxThinkTime));
        ChooseRandomTargetPosition();
        yield return new WaitForSeconds(1f);
        preparingSwim = false;
    }

    /// <summary>
    /// Прекратить процесс подготовки к следющему заплыву
    /// </summary>
    protected virtual void StopSwimProcess()
    {
        preparingSwim = false;
        StopCoroutine("PrepareToNextSwimProcess");
    }

    /// <summary>
    /// Определить начальное направление движения пираньи, учитывая её ориентацию
    /// </summary>
    protected Vector2 GetDirection()
    {
        float angle = transform.eulerAngles.z / 180f * Mathf.PI;
        return Mathf.Sign(transform.lossyScale.x) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    #region behaviorActions

    /// <summary>
    /// Изменить данные персонажа таким образом, чтобы они были пригодны для работы в следующей модели поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        StopSwimProcess();
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        hitBox.SetHitBox(new HitParametres(attackParametres));
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        hitBox.ResetHitBox();
        ChooseRandomTargetPosition();
    }

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
                if ((Vector2.Distance(targetPos, pos) < attackDistance) || (wallCheck.WallInFront || !(waterCheck.WallInFront)))
                {
                    if (!preparingSwim)
                        StartCoroutine("PrepareToNextSwimProcess");
                }
                if ((Vector2.Distance(targetPos, pos) >= attackDistance))
                    Move(OrientationEnum.right);
            }
        }
    }

    //Функция, реализующая агрессивное состояние ИИ
    protected override void AgressiveBehavior()
    {
        if (!mainTarget.exists)
        {
            BecomeCalm();
            return;
        }

        Vector2 targetPosition = currentTarget;
        Vector2 pos = transform.position;
        if (!waiting)
            Move(OrientationEnum.right);
        else
        {
            float sqDistance = Vector2.SqrMagnitude(targetPosition - pos);
            if (sqDistance < waitingNearDistance * waitingNearDistance)
                MoveAway(OrientationEnum.left);
            else if (sqDistance < waitingFarDistance * waitingFarDistance)
            {
                StopMoving();
                CurrentDirection = (targetPosition - pos).normalized;
            }
            else
                Move(OrientationEnum.right);
        }
    }

    /// <summary>
    /// Функция, реализующая состояние ИИ, при котором тот перемещается между текущими точками следования
    /// </summary>
    protected override void PatrolBehavior()
    {
        if (loyalty == LoyaltyEnum.ally && currentTarget.exists)
        {
            Move(OrientationEnum.right);
        }   
    }

    /// <summary>
    /// Выдвинуться к указанной позиции
    /// </summary>
    protected override void GoToThePoint(Vector2 targetPosition)
    {}

    /// <summary>
    /// Выдвинуться в начальную позицию
    /// </summary>
    protected override void GoHome()
    {
        MainTarget = ETarget.zero;
        BecomePatrolling();
        if (loyalty == LoyaltyEnum.ally ? beginPosition.transform != null : false)
            currentTarget = new ETarget(beginPosition.transform);
    }

    /// <summary>
    /// Никак не среагировать на услышанный боевой клич
    /// </summary>
    /// <param name="cryPosition"></param>
    public override void HearBattleCry(Vector2 cryPosition)
    {}

    #endregion //behaviorActions

    #region optimization

    /// <summary>
    /// Сменить модель поведения в связи с выходом из триггера
    /// </summary>
    protected override void AreaTriggerExitChangeBehavior()
    {
        BecomeCalm();
    }

    /// <summary>
    /// Найти следующую позицию в текущем водоёме
    /// </summary>
    protected virtual void FindNextWaterPosition()
    {
        ChooseRandomTargetPosition();
        if (!Physics2D.OverlapCircle(currentTarget, navCellSize / 4f, LayerMask.GetMask(wLName)))
        {
            Vector2 pos = transform.position;
            Vector2 distance = currentTarget - pos;
            for (int i = 9; i >= 0; i++)
                if (Physics2D.OverlapCircle(pos + distance / 10f * i, navCellSize / 4f, LayerMask.GetMask(wLName)))
                {
                    currentTarget = new ETarget(pos + distance / 10f * i);
                    break;
                }
        }
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
                    StopSwimProcess();
                    FindNextWaterPosition();
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

    /// <summary>
    /// Функция, реализующая анализ окружающей персонажа обстановки, когда тот находится в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
        if (behavior != BehaviorEnum.calm)
        {
            if (!followOptPath)
                StartCoroutine("PathPassOptProcess");
        }
        else
        {
            if (!currentTarget.exists)
            {
                StopCoroutine("PathPassOptProcess");//На всякий случай
                FindNextWaterPosition();
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
        Vector2 pos = transform.position;
        if (currentTarget.exists? currentTarget != pos:false)
            CurrentDirection = (currentTarget - pos).normalized;
    }

    /// <summary>
    /// Функция, которая переносит персонажа в ту позицию, в которой он может нормально функционировать для ведения оптимизированной версии 
    /// </summary>
    protected override void GetOptimizedPosition()
    {
        CurrentDirection = Vector2.right;
        if (!underWater)
        {
            //Если персонаж оказался не под водой во время оптимизации, то он должен переместиться под воду. Если же в данный момент под пираньей находится только земля, то она умирает
            RaycastHit2D hit1 = Physics2D.Raycast(transform.position, Vector2.down, navMap.mapSize.magnitude, LayerMask.GetMask(wLName));
            RaycastHit2D hit2 = Physics2D.Raycast(transform.position, Vector2.down, navMap.mapSize.magnitude, LayerMask.GetMask(gLName));
            if (!hit1? true : hit2? false : hit2.point.y>hit1.point.y)
            {
                Death();
            }
            else
            {
                transform.position = hit1.point + Vector2.down * minDistance;
                Underwater = true;
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
        if (!currentTarget.exists)
            BecomeCalm();
        else
        {
            while (currentTarget.exists)
            {
                Vector2 pos = transform.position;
                Vector2 targetPos = currentTarget;

                if (Vector2.SqrMagnitude(pos - targetPos) <= minCellSqrMagnitude)
                {
                    currentTarget.Exists = false;
                    pos = transform.position;                    
                }
                if (currentTarget.exists)
                {
                    Vector2 direction = targetPos - pos;
                    transform.position = pos + direction.normalized * Mathf.Clamp(speed, 0f, direction.magnitude);
                }
                yield return new WaitForSeconds(optTimeStep);
            }
            currentTarget.Exists = false;
            followOptPath = false;
        }
    }

    #endregion //optimization

    #region eventHandlers

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected override void HandleAttackProcess(object sender, HitEventArgs e)
    {
        if (mainTarget.exists)
        {
            if (!immobile)
            {
                StartCoroutine("PrepareToNextAttackProcess");
                Animate(new AnimationEventArgs("attack", "", Mathf.RoundToInt(10 * (attackParametres.preAttackTime + attackParametres.endAttackTime))));
                StartCoroutine("PushBackProcess");
            }
        }
        else
        {
            base.HandleAttackProcess(sender, e);
        }
    }

    /// <summary>
    /// Процесс отталкивания от цели для того, чтобы успешно совершить следующую атаку
    /// </summary>
    protected IEnumerator PushBackProcess()
    {
        Vector2 forceVector = (transform.position - mainTarget).normalized * pushBackForce;
        yield return new WaitForSeconds(attackParametres.preAttackTime);
        rigid.velocity = Vector2.zero;
        rigid.AddForce(forceVector);//При столкновении с врагом пиранья отталкивается назад
    }

    #endregion //eventHandlers

    /*
    /// <summary>
    /// Простейший алгоритм обхода препятствий
    /// </summary>
    protected ETarget FindPath()
    {

        Vector2 pos = transform.position;
        bool a1 = Physics2D.Raycast(pos, Vector2.up, fishSize, LayerMask.GetMask(gLName)) && (mainTarget.y - pos.y > avoidOffset);
        bool a2 = Physics2D.Raycast(pos, Vector2.right, fishSize, LayerMask.GetMask(gLName)) && (mainTarget.x > pos.x);
        bool a3 = Physics2D.Raycast(pos, Vector2.down, fishSize, LayerMask.GetMask(gLName)) && (mainTarget.y - pos.y < avoidOffset);
        bool a4 = Physics2D.Raycast(pos, Vector2.left, fishSize, LayerMask.GetMask(gLName)) && (mainTarget.x < pos.x);

        bool open1 = false, open2 = false;
        Vector2 aimDirection = a1 ? Vector2.up : a2 ? Vector2.right : a3 ? Vector2.down : a4 ? Vector2.left : Vector2.zero;
        if (aimDirection == Vector2.zero)
            return mainTarget;
        else
        {
            Vector2 vect1 = new Vector2(aimDirection.y, aimDirection.x);
            Vector2 vect2 = new Vector2(-aimDirection.y, -aimDirection.x);
            Vector2 pos1 = pos;
            Vector2 pos2 = pos1;
            while (Physics2D.Raycast(pos1, aimDirection, batSize, whatIsGround) && ((pos1 - pos).magnitude < maxAvoidDistance))
                pos1 += vect1 * batSize;
            open1 = !Physics2D.Raycast(pos1, aimDirection, batSize, whatIsGround);
            while (Physics2D.Raycast(pos2, aimDirection, batSize, whatIsGround) && ((pos2 - pos).magnitude < maxAvoidDistance))
                pos2 += vect2 * batSize;
            open2 = !Physics2D.Raycast(pos2, aimDirection, batSize, whatIsGround);
            Vector2 targetPosition = mainTarget;
            Vector2 newTargetPosition = (open1 && !open2) ? pos1 : (open2 && !open1) ? pos2 : ((targetPosition - pos1).magnitude < (targetPosition - pos2).magnitude) ? pos1 : pos2;
            return new ETarget(newTargetPosition);
        }
    }*/


    /*
    #region other

    //Здесь приводится блок функций, которые облегчают расчёт и воспритие весьма своеобразного движения этой рыбины, которое подвержено весьма не случайным законам

    /// <summary>
    /// Определить направление движения пираньи, учитывая её ориентацию
    /// </summary>
    protected Vector2 GetDirection()
    {
        float angle = transform.eulerAngles.z/180f*Mathf.PI;
        return Mathf.Sign(transform.lossyScale.x) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    /// <summary>
    /// Функция, определяющая зону перемещения и начальный курс перемещения, учитывая некоторую ориентацию пираньи (_direction)
    /// </summary>
    protected void DefineAreaAndBeginDirection(Vector2 _direction)
    {
        Vector2 pos = transform.position;
        Vector2 approximateDirection = Vector2.zero;
        if (Mathf.Abs(Vector2.Angle(_direction, Vector2.up)) <= 22.5f)
        {
            approximateDirection = Vector2.up;
            beginPosition = new ETarget(pos + Vector2.left * clockwiseDirection * areaSizeX);
            _direction = Vector2.up;
            currentRectangleSide = clockwiseDirection>0? FourDirectionEnum.right : FourDirectionEnum.left;
        }
        else if (Mathf.Abs(Vector2.Angle(_direction, Vector2.up + Vector2.right)) <=22.5f)
        {
            approximateDirection = Vector2.up + Vector2.right;
            beginPosition = new ETarget(pos + Vector2.left * clockwiseDirection * areaSizeX+Vector2.up*clockwiseDirection*areaSizeY);
            _direction = clockwiseDirection>0?Vector2.up:Vector2.right;
            currentRectangleSide = clockwiseDirection>0?FourDirectionEnum.right :FourDirectionEnum.up;
        }
        else if (Mathf.Abs(Vector2.Angle(_direction, Vector2.right)) <=22.5f)
        {
            approximateDirection = Vector2.right;
            beginPosition = new ETarget(pos + Vector2.up * clockwiseDirection * areaSizeY);
            _direction = Vector2.right;
            currentRectangleSide = clockwiseDirection > 0 ? FourDirectionEnum.down : FourDirectionEnum.up;
        }
        else if (Mathf.Abs(Vector2.Angle(_direction, Vector2.down + Vector2.right)) <= 22.5f)
        {
            approximateDirection = Vector2.down + Vector2.right;
            beginPosition = new ETarget(pos + Vector2.right * clockwiseDirection * areaSizeX + Vector2.up * clockwiseDirection * areaSizeY);
            _direction = clockwiseDirection > 0 ? Vector2.right : Vector2.down;
            currentRectangleSide = clockwiseDirection > 0 ? FourDirectionEnum.down : FourDirectionEnum.right;
        }
        else if (Mathf.Abs(Vector2.Angle(_direction, Vector2.down)) <= 22.5f)
        {
            approximateDirection = Vector2.down;
            beginPosition = new ETarget(pos + Vector2.right * clockwiseDirection * areaSizeX);
            _direction = Vector2.down;
            currentRectangleSide = clockwiseDirection > 0 ? FourDirectionEnum.left : FourDirectionEnum.right;
        }
        else if (Mathf.Abs(Vector2.Angle(_direction, Vector2.down + Vector2.left)) <= 22.5f)
        {
            approximateDirection = Vector2.down + Vector2.left;
            beginPosition = new ETarget(pos + Vector2.right * clockwiseDirection * areaSizeX + Vector2.down * clockwiseDirection * areaSizeY);
            _direction = clockwiseDirection > 0 ? Vector2.down : Vector2.left;
            currentRectangleSide = clockwiseDirection > 0 ? FourDirectionEnum.left : FourDirectionEnum.down;
        }
        else if (Mathf.Abs(Vector2.Angle(_direction, Vector2.left)) <= 22.5f)
        {
            approximateDirection = Vector2.left;
            beginPosition = new ETarget(pos + Vector2.down * clockwiseDirection * areaSizeY);
            _direction = Vector2.left;
            currentRectangleSide = clockwiseDirection > 0 ? FourDirectionEnum.up : FourDirectionEnum.down;
        }
        else if (Mathf.Abs(Vector2.Angle(_direction, Vector2.up + Vector2.left)) <= 22.5f)
        {
            approximateDirection = Vector2.up + Vector2.left;
            beginPosition = new ETarget(pos + Vector2.left * clockwiseDirection * areaSizeX + Vector2.down * clockwiseDirection * areaSizeY);
            _direction = clockwiseDirection > 0 ? Vector2.left : Vector2.up;
            currentRectangleSide = clockwiseDirection > 0 ? FourDirectionEnum.up : FourDirectionEnum.left;
        }
        beginPosition = new ETarget(pos + clockwiseDirection*(approximateDirection.y * Vector2.left * areaSizeX + approximateDirection.x * Vector2.up * areaSizeX));
        if (approximateDirection.x * approximateDirection.y != 0f)
        {
            Vector2 vect1 = approximateDirection.y * Vector2.up, vect2 = approximateDirection.x * Vector2.right;
            if ((approximateDirection.x * vect1.y - approximateDirection.y * vect1.x) * clockwiseDirection > 0f)
            {
                currentDirection = vect1;
            }
        }
        else
            currentDirection = approximateDirection;
        _direction =  ? () : approximateDirection; 

    }

    protected FourDirectionEnum GetFourDirectionEnumFromVector(Vector2 vect)
    {
        if ()
    }

    #endregion //other
    */

}