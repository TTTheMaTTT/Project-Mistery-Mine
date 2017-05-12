using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Контролле, управляющий боссом - призраком девушки
/// </summary>
public class GhostLadyController : BossController
{

    #region consts

    protected const float phase1Time = 60f, phase2Time = 60f;//Сколько времени длится

    protected const float diveAttackMinAngle = Mathf.PI * .1f, diveAttackMaxAngle = .36f * Mathf.PI;

    #endregion //consts

    #region fields

    protected Collider2D col;
    protected List<Transform> stalactiteSpots = new List<Transform>();//Позиции, откуда будут падать сталактиты
    protected WallChecker wallCheck, insideWallCheck;//Индикатор, который определяет, находится ли призрак внутри той или иной стены

    [SerializeField]protected GameObject missile;//Снаряд призрака
    [SerializeField]protected GameObject stalactite;//Сталактит, который падает после крика призрака
    
    [SerializeField]protected GameObject platform;//Платформа, которую надо отключить
    [SerializeField]protected List<GameObject> fires;//Огни, что появляются в третьей фазе боя

    //Список кулдаунов
    protected override List<Timer> Timers
    {
        get
        {
            return new List<Timer> { new Timer("attackCooldown",attackRate),
                                     new Timer("usualAttackCooldown", usualAttackCooldown),
                                     new Timer("fastAttackCooldown", fastAttackCooldown),
                                     new Timer("stalactiteAttackCooldown",stalactiteAttackCooldown),
                                     new Timer("hurricaneAttackCooldown",hurricaneAttackCooldown),
                                     new Timer("diveAttackCooldown",diveAttackCooldown)};
        }
    }

    #endregion //fields

    #region parametres

    protected int attackTimes;//Вспомогательная переменная для подсчёта кол-ва совершённых подряд атак одного типа
    protected float attackRate;
    [SerializeField]protected float phase1AttackRate = 1.5f, phase2AttackRate = .5f;//Времена между совершениями атак

    protected override float attackDistance { get { return 3f; } }//Какое максимальное расстояние, при котором персонаж атакует издалека
    protected override float waitingNearDistance { get { return 1f; } }//Максимальное расстояние ближнего боя
    public override LoyaltyEnum Loyalty
    {
        get
        {
            return base.Loyalty;
        }

        set
        {
            base.Loyalty = value;
            gameObject.tag = "boss";
            gameObject.layer = LayerMask.NameToLayer("characterWithoutPlatform");
        }
    }

    #region usualAttack

    [SerializeField]protected Vector2 shootOffset;//Насколько смещён прицел относительно персонажа
    [SerializeField]protected float missileSpeed;//Скорость снаряда
    [SerializeField]protected float usualAttackNearDistance=.5f;//Какое расстояние считается слишком близким для совершения обычной атаки (минимальное расстояние, на которое сближается призрак)
    [SerializeField]protected float preFastAttackTime = .1f, endFastAttackTime = .3f;//Времена перед и после совершения быстрой атаки
    [SerializeField]protected float usualAttackCooldown = 1.5f;
    [SerializeField]protected float fastAttackCooldown = 3.5f;
    [SerializeField]protected int usualAttackTimes = 3;//Кол-во выстрелов при обычной атаке
    [SerializeField]protected float fastAttackTimes = 3;//Кол-во выстрелов при быстрой атаке
    [SerializeField]protected float fastAttackAngleDeviation = 10f;//Отклонение в угле при быстрой атаке
    [SerializeField]protected float fastAttackPushForce = 100f;//Сила, с которой призрак отскочит от игрока

    #endregion //usualAttack

    #region stalactiteAttack

    protected Vector2 stalactiteAttackPosition;//С какой позиции призрак начинает сталактитную атаку
    [SerializeField]protected float preStalactiteAttackTime = 10f, betweenStalactiteAttackTime;//Времена падений сталактитов (время перед всей атакой и между отдельными падениями
    [SerializeField]protected int phase1StalactiteCount = 4, phase2StalactiteCount = 6, phase3StalactiteCount = 10;//Кол-во сталактитов в первой и второй фазе
    [SerializeField] protected int phase1MaxStalactitesAtOneTime = 2, phase2MaxStalactitesAtOneTime = 3, phase3MaxStalactitesAtOneTime = 4;//Сколько сталактитов может упасть одновременно в первой и второй фазах
    [SerializeField]protected float stalactiteAttackCooldown = 12f;

    #endregion //stalactiteAttack

    #region hurricaneAttack

    [SerializeField]protected HitParametres hurricaneAttackParametres;//Параметры атаки в форме урагана
    [SerializeField]protected HitParametres frostAttackParametres;//Постепенный урон холодом при попадании в ураган
    [SerializeField]protected float hurricaneSpeed = 2f;
    [SerializeField]protected float hurricaneAcceleration = 10f;
    protected Vector2 hurricaneCentrePosition;//Центр, относительно которого движется ураган
    [SerializeField]protected float hurricaneAttackRadius = 1f;//Растояние, на которое может отойти призрак от центра атаки в форме урагана
    protected Vector2 hurricaneDirection;//В какую сторону движется ураган в данный момент
    [SerializeField]protected float hurricaneAttackCooldown = 15;//Кулдаун ураганной атаки
    [SerializeField]protected float hurricanePushForce= 500f;//Отталкивание урагана от стенок
    protected bool inHurricane;//Находится ли призрак в состоянии урагана
    protected Vector2 hurricanePrevPosition;
    protected bool hurricanePush;

    #endregion //hurricaneAttack

    #region diveAttack

    protected Vector2 currentDirection = Vector2.right;//Направление курса полёта призрака девушки
    protected virtual Vector2 CurrentDirection
    {
        get
        {
            return currentDirection;
        }
        set
        {
            currentDirection = value;
            float angle = Mathf.Sign(currentDirection.y) * Vector2.Angle(Vector2.right, currentDirection);
            if (Mathf.Abs(angle) > 90f)
            {
                Turn(OrientationEnum.left);
                angle = -Mathf.Sign(currentDirection.y) * Vector2.Angle(Vector2.left, currentDirection);
            }
            else
                Turn(OrientationEnum.right);
            transform.eulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    [SerializeField]protected HitParametres diveAttackParametres;//Параметры атаки при пикировании

    [SerializeField]protected int diveAttackMinTimes = 2, diveAttackMaxTimes = 4;//Кол-во совершаемых подряд атак при пикировании
    [SerializeField] protected float diveAttackPushForce = 120f;//Насколько сильно призрак устремляется к цели при атаки в пикировании
    [SerializeField]protected float preDiveSpeed = 1f, preDiveAcceleration = 5f;//Скорость и ускорение призрака при подготовке к атаке в пикировании
    [SerializeField]protected float diveSpeed = 1f, diveAcceleration = 5f;//Скорость и ускорение призрака при атаке в пикировании
    [SerializeField]protected float diveAttackCooldown = 7f;
    [SerializeField]protected float diveAttackDistance = 1.3f;//Расстояние на котором находится призрак от персонажа при подготовке совершения пикирующей атаки
    protected bool moveClockwise = false;
    protected float diveAttackTangent = 0f;
    protected int diveAttackTimes = 2;

    #endregion //diveAttack

    [SerializeField]protected HitParametres touchAttackParametres;//Параметры атаки при обычном столкновении

    #endregion //parametres

    protected override void Initialize()
    {
        col = GetComponent<Collider2D>();
        wallCheck = transform.FindChild("WallCheck").GetComponentInChildren<WallChecker>();
        insideWallCheck = transform.FindChild("InsideWallCheck").GetComponent<WallChecker>();
        base.Initialize();
        bossPhase = 0;
        wallCheck.enabled = false;
        insideWallCheck.enabled = false;
        hitBox.IgnoreInvul = true;
        rigid.gravityScale = 0f;

        stalactiteSpots = new List<Transform>();
        inHurricane = false;
        attackTimes = 0;

        Transform bossBattleZone = GameObject.Find("BossBattle").transform;
        if (bossBattleZone != null)
        {
            stalactiteAttackPosition = bossBattleZone.FindChild("StalactiteAttackPosition").position;
            Transform stalactiteSpotsTrans = bossBattleZone.FindChild("StalactiteSpots");
            if (stalactiteSpotsTrans != null)
                for (int i = 0; i < stalactiteSpotsTrans.childCount; i++)
                    stalactiteSpots.Add(stalactiteSpotsTrans.GetChild(i));
        }

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionIn = NullAreaFunction;
            areaTrigger.triggerFunctionOut = NullAreaFunction;
            areaTrigger.InitializeAreaTrigger();
        }

        hitBox.SetHitBox(touchAttackParametres);
        hitBox.AttackEventHandler += HandleAttackProcess;

        BecomeCalm();
    }

    /// <summary>
    /// Функция присоединения хп босса к игровому UI
    /// </summary>
    protected override void ConnectToUI()
    {
    }

    /// <summary>
    /// Отсоединиться от игрового интерфейса
    /// </summary>
    protected override void DisconnectFromUI()
    {
    }

    #region movement

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = (currentTarget - transform.position).normalized * speed * speedCoof;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
            Turn(_orientation);

    }

    /// <summary>
    /// Перемещение в форме урагана
    /// </summary>
    protected void HurricaneMove()
    {
        Vector2 targetVelocity =new Vector2(hurricaneSpeed * speedCoof*(int)orientation,rigid.velocity.y);
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * hurricaneAcceleration);
    }

    protected void DivingMove()
    {
        Vector2 targetVelocity = (currentTarget - transform.position).normalized * diveSpeed * speedCoof;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * diveAcceleration);
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(rigid.velocity.x)));
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
        Vector2 targetVelocity = (transform.position - currentTarget).normalized * speed * speedCoof;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Двигаться относительно цели по круговой орбите, держась на определённом расстоянии от неё
    /// </summary>
    /// <param name="clockWise">Двигаться по часовой (true) или против (false)</param>
    protected virtual void MoveAround(bool clockWise)
    {
        if (!currentTarget.exists)
            return;
        Vector2 targetDistance = currentTarget - transform.position;
        Vector2 tangentDirection = targetDistance.normalized;
        tangentDirection = new Vector2(tangentDirection.y, -tangentDirection.x);
        if ((clockWise ? 1f : -1f) * (tangentDirection.x * targetDistance.y - tangentDirection.y * targetDistance.x) > 0f)
            tangentDirection *= -1f;
        Vector2 targetVelocity = tangentDirection.normalized;
        float sqDistance = targetDistance.sqrMagnitude;
        if (sqDistance > diveAttackDistance * diveAttackDistance*1.2f)
            targetVelocity += targetDistance.normalized;
        else if (sqDistance < diveAttackDistance * diveAttackDistance*.8f)
            targetVelocity -= targetDistance.normalized;
        targetVelocity *= preDiveSpeed;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * preDiveAcceleration);
        if (targetDistance.x * (int)orientation < 0f)
            Turn();
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    /// <param name="_orientation">В какую сторону должен смотреть персонаж после поворота</param>
    public override void Turn(OrientationEnum _orientation)
    {
        base.Turn(_orientation);
        wallCheck.SetPosition(0f, (int)orientation);
        insideWallCheck.SetPosition(0f, (int)orientation);
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    protected override void Turn()
    {
        base.Turn();
        wallCheck.SetPosition(0f, (int)orientation);
        insideWallCheck.SetPosition(0f, (int)orientation);
    }

    #endregion //movement

    /// <summary>
    /// Анализировать окружающую обстановку
    /// </summary>
    protected override void Analyse()
    {
        if (behavior != BehaviorEnum.agressive && loyalty==LoyaltyEnum.enemy && !insideWallCheck.CheckWall())
        {
            if (Vector3.Distance(SpecialFunctions.Player.transform.position, transform.position) <= sightRadius)
            {
                MainTarget = new ETarget(SpecialFunctions.player.transform);
                BecomeAgressive();
            }
        }
    }

    #region bossActions

    /// <summary>
    /// Выбрать одну из возможных атак и совершать её
    /// </summary>
    protected void ChooseAttack()
    {

        if (!mainTarget.exists)
            return;

        List<string> possibleActions = new List<string>();
        Vector2 targetDistance = mainTarget - (Vector2)transform.position;
        float sDistance = targetDistance.sqrMagnitude;

        //Проверим, какие атаки вообще можно совершить в данный момент
        if (sDistance < attackDistance * attackDistance)
        {
            if (!IsTimerActive("usualAttackCooldown"))
                possibleActions.Add("usualAttack");
            if (!IsTimerActive("fastAttackCooldown") && sDistance < waitingNearDistance * waitingNearDistance)
                possibleActions.Add("fastAttack");
        }
        if (!IsTimerActive("stalactiteAttackCooldown"))
            possibleActions.Add("stalactiteAttack");
        if (!IsTimerActive("hurricaneAttackCooldown") && sDistance < waitingNearDistance * waitingNearDistance && bossPhase>0)
            possibleActions.Add("hurricaneAttack");
        if (!IsTimerActive("diveAttackCooldown") && bossPhase > 1)
            possibleActions.Add("diveAttack");

        if (possibleActions.Count == 0)
            return;

        if (targetDistance.x * (int)orientation < 0f)
            Turn();
        string nextAction = possibleActions[Random.Range(0, possibleActions.Count)];

        switch (nextAction)
        {
            case "usualAttack":
                {
                    Attack();
                    break;
                }
            case "fastAttack":
                {
                    FastAttack();
                    break;
                }
            case "stalactiteAttack":
                {
                    StartStalactiteAttack();
                    break;
                }
            case "hurricaneAttack":
                {
                    StartHurricaneAttack();
                    break;
                }
            case "diveAttack":
                {
                    StartDiveAttack();
                    break;
                }
            default:
                break;
        }

    }
    
    /// <summary>
    /// Действия, совершаемые боссом в обычном режиме
    /// </summary>
    protected void UsualBossAction()
    {
        if (mainTarget.exists && employment > 5)
        {
            Vector2 targetPosition = mainTarget;
            Vector2 pos = transform.position;
            Vector2 targetDirection = targetPosition - pos;
            float sqDistance = targetDirection.sqrMagnitude;

            if (sqDistance > usualAttackNearDistance*usualAttackNearDistance)
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDirection.x)));
            else
                StopMoving();

            if (sqDistance < attackDistance * attackDistance)
                if (employment > 8)
                {
                    if ((int)orientation * targetDirection.x < 0f)
                        Turn();
                    ChooseAttack();
                }
        }
        Animate(new AnimationEventArgs("fly"));
    }

    /// <summary>
    /// Действия, совершаемые боссом в режиме создания сталактитов
    /// </summary>
    protected void StalactiteBossAction()
    {
        if (employment > 8)
        {
            Vector2 targetDirection = currentTarget - (Vector2)transform.position;
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDirection.x)));
            if (targetDirection.sqrMagnitude < .01f)
            {
                StopMoving();
                StartCoroutine("StalactiteAttackProcess");
            }
            Animate(new AnimationEventArgs("fly"));
        }
    }

    /// <summary>
    /// Начать атаку с сталактитами
    /// </summary>
    protected void StartStalactiteAttack()
    {
        StopAttack();
        bossAction = StalactiteBossAction;
        currentTarget = new ETarget(stalactiteAttackPosition);
    }

    /// <summary>
    /// Процесс совершения атаки со сталактитами
    /// </summary>
    /// <returns></returns>
    protected IEnumerator StalactiteAttackProcess()
    {
        employment = Mathf.Clamp(employment - 6, 0, maxEmployment);
        Animate(new AnimationEventArgs("stalactiteAttack"));
        SpecialFunctions.CamController.ShakeCamera(preStalactiteAttackTime);
        yield return new WaitForSeconds(preStalactiteAttackTime);
        int stalactiteSpawnTimes = bossPhase == 0 ? phase1StalactiteCount : bossPhase == 1 ? phase2StalactiteCount : phase3StalactiteCount;
        int maxStalactitesAtOneTime = bossPhase == 0 ? phase1MaxStalactitesAtOneTime : bossPhase == 1 ? phase2MaxStalactitesAtOneTime : phase3MaxStalactitesAtOneTime;
        for (int i = 0; i < stalactiteSpawnTimes; i++)
        {
            int stalactitesCount = Random.Range(1, maxStalactitesAtOneTime);
            List<int> usedSpotsIndexes = new List<int>();
            for (int j = 0; j < stalactitesCount; j++)
            {
                int stalactiteSpotIndex = -1;
                while (stalactiteSpotIndex < 0 || usedSpotsIndexes.Contains(stalactiteSpotIndex))
                    stalactiteSpotIndex = Random.Range(0, stalactiteSpots.Count);
                GameObject _stalactite = Instantiate(stalactite, stalactiteSpots[stalactiteSpotIndex].position, Quaternion.identity);
                _stalactite.GetComponent<StalactiteScript>().ActivateMechanism();
            }
            SpecialFunctions.CamController.ShakeCamera(betweenStalactiteAttackTime);
            yield return new WaitForSeconds(betweenStalactiteAttackTime);
        }
        employment = Mathf.Clamp(employment + 6, 0, maxEmployment);
        StartTimer("attackCooldown");
        StartTimer("stalactiteAttackCooldown");
        StopAttack();
    }

    /// <summary>
    /// Начать атаку в форме урагана
    /// </summary>
    protected void StartHurricaneAttack()
    {
        StopAttack();
        hurricanePrevPosition = Vector2.zero;
        rigid.gravityScale = 1f;
        bossAction = HurricaneBossAction;
        hitBox.SetHitBox(hurricaneAttackParametres);
        inHurricane = true;
        Vector2 targetPos = mainTarget;
        currentTarget = new ETarget(targetPos);
        hurricaneCentrePosition = transform.position;
        hurricaneDirection = (currentTarget - hurricaneCentrePosition).normalized;
        StartCoroutine("HurricaneProcess");
        Animate(new AnimationEventArgs("hurricaneAttack"));
    }

    /// <summary>
    /// Действия босса в форме урагана
    /// </summary>
    protected void HurricaneBossAction()
    {
        Vector2 pos = transform.position;
        if (employment > 7)
        {
            HurricaneMove();
            if (wallCheck.CheckWall())
                Turn();
            if (!hurricanePush ? (pos - hurricanePrevPosition).magnitude < Time.fixedDeltaTime * hurricaneSpeed * .05f : false)
            {
                Turn();
                rigid.AddForce(hurricanePushForce * Vector2.right * (int)orientation);
                transform.position += .05f * Vector3.right * (int)orientation;
                StartCoroutine("HurricanePushProcess");
            }
            hurricanePrevPosition = transform.position;
        }
    }

    IEnumerator HurricanePushProcess()
    {
        hurricanePush = true;
        yield return new WaitForSeconds(.5f);
        hurricanePush = false;
    }

    /// <summary>
    /// Процесс совершения ураганной атаки
    /// </summary>
    /// <returns></returns>
    protected IEnumerator HurricaneProcess()
    {
        employment = Mathf.Clamp(employment - 5, 0, maxEmployment);
        yield return new WaitForSeconds(hurricaneAttackParametres.preAttackTime);
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);
        yield return new WaitForSeconds(hurricaneAttackParametres.endAttackTime);
        StopAttack();
        currentTarget = mainTarget;
        hitBox.SetHitBox(touchAttackParametres);
        StartTimer("attackCooldown");
        StartTimer("hurricaneAttackCooldown");
    }

    /// <summary>
    /// Начать атаки в пикировании
    /// </summary>
    protected void StartDiveAttack()
    {
        StopAttack();
        Animate(new AnimationEventArgs("setBackground"));
        diveAttackTimes = Random.Range(diveAttackMinTimes, diveAttackMaxTimes + 1);
        moveClockwise = Random.Range(0f, 1f) > .5f;
        diveAttackTangent = Mathf.Tan(Random.Range(diveAttackMinAngle, diveAttackMaxAngle));
        bossAction = DiveBossAction;
        col.isTrigger = true;//Призрак становится совсем неосязаемым
    }

    /// <summary>
    /// Действия босса при атаках в пикировании
    /// </summary>
    protected void DiveBossAction()
    {
        if (!mainTarget.exists)
            return;
        if (employment >= 7)
        {
            Vector2 pos = transform.position;
            Vector2 targetDirection = CurrentTarget - pos;
            MoveAround(moveClockwise);
            if (attackTimes < diveAttackTimes && targetDirection.x != 0 ? targetDirection.sqrMagnitude>diveAttackDistance*diveAttackDistance*.5f && 
                                                                        -targetDirection.y / Mathf.Abs(targetDirection.x) > diveAttackTangent : false)
                StartCoroutine("DiveAttackProcess");
            if (attackTimes >= diveAttackTimes)
                if (!insideWallCheck.CheckWall())
                {
                    StopAttack();
                    col.isTrigger = false;//Призрак снова сталкивается с препятствиями 
                    StartTimer("attackCooldown");
                    StartTimer("diveAttackCooldown");
                }
            Animate(new AnimationEventArgs("fly"));
        }
        else
        {
            DivingMove();
        }
    }

    /// <summary>
    /// Процесс совершения атаки в пикировании
    /// </summary>
    /// <returns></returns>
    protected IEnumerator DiveAttackProcess()
    {
        StopMoving();
        employment = Mathf.Clamp(employment - 5, 0, maxEmployment);
        Animate(new AnimationEventArgs("attack", "DiveAttack", Mathf.RoundToInt(100 * diveAttackParametres.prepareAttackTime)));
        Animate(new AnimationEventArgs("playSound", "Fury", 0));
        yield return new WaitForSeconds(diveAttackParametres.preAttackTime);
        Vector2 targetDirection = (currentTarget - (Vector2)transform.position).normalized;
        rigid.AddForce(targetDirection * diveAttackPushForce);
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDirection.x)));
        hitBox.SetHitBox(diveAttackParametres);
        yield return new WaitForSeconds(diveAttackParametres.endAttackTime);
        StopMoving();
        moveClockwise = Random.Range(0f, 1f) > .5f;
        diveAttackTangent = Mathf.Tan(Random.Range(diveAttackMinAngle, diveAttackMaxAngle));
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);
        attackTimes++;
        hitBox.SetHitBox(touchAttackParametres);
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        StartCoroutine("AttackProcess");
    }

    /// <summary>
    /// Совершить быстрые атаки
    /// </summary>
    protected virtual void FastAttack()
    {
        StopMoving();
        Vector2 targetDistance = CurrentTarget - transform.position;
        rigid.AddForce(-targetDistance.normalized * fastAttackPushForce);
        StartCoroutine("FastAttackProcess");
    }

    /// <summary>
    /// Процесс обычной атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign((currentTarget - transform.position).x)));
        string _attackName = attackTimes > 0 ? "FastAttack" : "Attack";
        float _preTime = attackTimes > 0 ? preFastAttackTime : attackParametres.preAttackTime;
        float _endTime = attackTimes > 0 ? endFastAttackTime : attackParametres.endAttackTime;
        Animate(new AnimationEventArgs("attack", _attackName, Mathf.RoundToInt(100 * (_preTime + _endTime))));

        if (attackTimes<=0)
            employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(_preTime);

        Vector2 pos = transform.position;
        Vector2 targetDistance = mainTarget - pos;
        Vector2 direction = Vector2.zero;
        if (attackTimes / 3 == 2)
        {
            //На третий выстрел призрак пытается предугадать движение персонажа и выстрелить в предсказанную позицию
            Rigidbody2D targetRigid=null;
            if (mainTarget.transform == null ? true : (targetRigid = mainTarget.transform.GetComponent<Rigidbody2D>()) != null)
                direction = targetDistance.x * (int)orientation >= 0f ? (targetDistance - new Vector2(shootOffset.x * (int)orientation, shootOffset.y)).normalized : (int)orientation * Vector2.right;
            else
            {
                float nx = rigid.velocity.normalized.x, ny = rigid.velocity.normalized.y, vt = rigid.velocity.magnitude, vm=missileSpeed, x0 = targetDistance.x, y0 = targetDistance.y;
                float d1 = Mathf.Pow((nx * x0 + ny * y0) * vt / vm, 2) + (x0 * x0 + y0 * y0) * (1 - vt * vt / vm / vm);
                if (d1 < 0f)
                    direction = targetDistance.x * (int)orientation >= 0f ? (targetDistance - new Vector2(shootOffset.x * (int)orientation, shootOffset.y)).normalized : (int)orientation * Vector2.right;
                else
                {
                    float l = ((nx * x0 + ny * y0) * vt / vm + Mathf.Sqrt(d1)) / (1 - vt * vt / vm * vm);
                    float xd = x0 + vt / vm * l * nx, yd=y0+vt/vm*l*ny;
                    Vector2 newTarget = pos + new Vector2(xd, yd);
                    direction= newTarget.x * (int)orientation >= 0f ? (newTarget - new Vector2(shootOffset.x * (int)orientation, shootOffset.y)).normalized : (int)orientation * Vector2.right;
                }

            }
        }
        else
            direction = targetDistance.x * (int)orientation >= 0f ? (targetDistance - new Vector2(shootOffset.x * (int)orientation, shootOffset.y)).normalized : (int)orientation * Vector2.right;

        GameObject newMissile = Instantiate(missile, transform.position + new Vector3(shootOffset.x * (int)orientation, shootOffset.y, 0f),Quaternion.identity) as GameObject;
        Rigidbody2D missileRigid = newMissile.GetComponent<Rigidbody2D>();
        missileRigid.velocity = direction * missileSpeed;
        missileRigid.gravityScale = 0f;
        HitBoxController missileHitBox = missileRigid.GetComponentInChildren<HitBoxController>();
        if (missileHitBox != null)
        {
            missileHitBox.SetEnemies(enemies);
            missileHitBox.SetHitBox(new HitParametres(attackParametres));
            missileHitBox.allyHitBox = false;
            //coalHitBox.SetHitBox(new HitClass(crit? damage*1.5f:damage, -1f, coalHitSize, Vector2.zero, coalForce));
        }
        yield return new WaitForSeconds(_endTime);
        attackTimes++;
        if (attackTimes<usualAttackTimes)
            StartCoroutine("AttackProcess");
        else
        {
            attackTimes = 0;
            employment = Mathf.Clamp(employment + 3, 0, maxEmployment);

            StartTimer("attackCooldown");
            StartTimer("usualAttackCooldown");
        }
    }

    /// <summary>
    /// Процесс совершения быстрой атаки
    /// </summary>
    /// <returns></returns>
    protected IEnumerator FastAttackProcess()
    {
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign((currentTarget - transform.position).x)));
        employment = Mathf.Clamp(employment - 5, 0, maxEmployment);
        Animate(new AnimationEventArgs("attack", "FastAttack",Mathf.RoundToInt(100*(preFastAttackTime+endFastAttackTime))));
        yield return new WaitForSeconds(preFastAttackTime);
        StopMoving();
        Vector2 pos = transform.position;
        Vector2 targetDistance = mainTarget - pos;
        Vector2 direction = targetDistance.x * (int)orientation >= 0f ? (targetDistance - new Vector2(shootOffset.x * (int)orientation, shootOffset.y)).normalized : (int)orientation * Vector2.right;
        for (int i=0; i<3;i++)
        {
            float randAngle = Random.Range(-fastAttackAngleDeviation / 2f, fastAttackAngleDeviation / 2f)*Mathf.PI/180f;
            float cos = Mathf.Cos(randAngle), sin = Mathf.Sin(randAngle);
            Vector2 randDirection = new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
            GameObject newMissile = Instantiate(missile, transform.position + new Vector3(shootOffset.x * (int)orientation, shootOffset.y, 0f), Quaternion.identity) as GameObject;
            Rigidbody2D missileRigid = newMissile.GetComponent<Rigidbody2D>();
            missileRigid.velocity = randDirection * missileSpeed;
            missileRigid.gravityScale = 0f;
            HitBoxController missileHitBox = missileRigid.GetComponentInChildren<HitBoxController>();
            if (missileHitBox != null)
            {
                missileHitBox.SetEnemies(enemies);
                missileHitBox.SetHitBox(new HitParametres(attackParametres));
                missileHitBox.allyHitBox = false;
                //coalHitBox.SetHitBox(new HitClass(crit? damage*1.5f:damage, -1f, coalHitSize, Vector2.zero, coalForce));
            }
            yield return new WaitForSeconds(.1f);
        }
        yield return new WaitForSeconds(endFastAttackTime);
        StartTimer("attackCooldown");
        StartTimer("fastAttackCooldown");
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);
    }

    /// <summary>
    /// Принудительно завершить атаку
    /// </summary>
    protected override void StopAttack()
    {
        base.StopAttack();
        attackTimes = 0;
        inHurricane = false;
        rigid.gravityScale = 0f;
        StopCoroutine("StalactiteAttackProcess");
        StopCoroutine("HurricaneAttackProcess");
        StopCoroutine("DiveAttackProcess");
        StopCoroutine("FastAttackProcess");
        StopTimer("attackCooldown", true);
        bossAction = UsualBossAction;
        if (mainTarget.exists)
            currentTarget = mainTarget;
        hitBox.SetHitBox(touchAttackParametres);
    }

    /// <summary>
    /// Призрак девушки неуязвим к любому типу урона
    /// </summary>
    /// <param name="hitData"></param>
    public override void TakeDamage(HitParametres hitData)
    {
    }

    /// <summary>
    /// Призрак девушки неуязвим к любому типу урона
    /// </summary>
    public override void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
    }

    protected override void Death()
    {
        //Animate(new AnimationEventArgs("playSound", "Death", 0));
        SpecialFunctions.gameController.GetAchievement("THE_PHANTOM");
        base.Death();
    }

    /// <summary>
    /// Процесс нанесения холодного урона определённой цели 
    /// </summary>
    protected IEnumerator FrostAttackProcess(CharacterController _char)
    {
        float _frostTime = frostAttackParametres.actTime;
        _char.AddCustomBuff(new BuffData("FrozenProcess", _frostTime));
        while (_frostTime > 0f)
        {
            yield return new WaitForSeconds(1f);
            _frostTime -= 1f;
            _char.TakeDamage(frostAttackParametres);
        }
    }

    /// <summary>
    /// Процесс перехода босса между фазами со временем
    /// </summary>
    protected IEnumerator BossLifeProcess()
    {
        bossPhase = 0;
        attackRate = phase1AttackRate;
        yield return new WaitForSeconds(phase1Time);
        bossPhase++;
        yield return new WaitForSeconds(phase2Time);
        bossPhase++;
        foreach (GameObject fire in fires)
            fire.SetActive(true);
        attackRate = phase2AttackRate;
    }

    #endregion //bossActions

    #region behaviorActions

    /// <summary>
    /// Обновить информацию, выжную для моделей поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        ResetTimers();
    }

    /// <summary>
    /// Стать агрессивным
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        col.isTrigger = false;
        StartCoroutine("BossLifeProcess");
        currentTarget = mainTarget;
        bossAction = UsualBossAction;
        Collider2D[] platformCols=platform.GetComponents<Collider2D>();
        for (int i = 0; i < platformCols.Length; i++)
            platformCols[i].enabled = false;
    }

    /// <summary>
    /// Стать спокойным
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        col.isTrigger = true;
        StopCoroutine("BossLifeProcess");
    }

    /// <summary>
    /// Перейти в патрулирующее состояние
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        col.isTrigger = true;
        MainTarget = ETarget.zero;
        StopCoroutine("BossLifeProcess");
    }

    /// <summary>
    /// Выдвинуться к указанной точке
    /// </summary>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        currentTarget = new ETarget(targetPosition);
        BecomePatrolling();
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        bossAction.Invoke();
    }

    /// <summary>
    /// Поведение патрулирования
    /// </summary>
    protected override void PatrolBehavior()
    {
        if (!currentTarget.Exists)
            return;
        Vector2 targetDistance = currentTarget - (Vector2)transform.position;
        if (targetDistance.sqrMagnitude > minDistance * minDistance)
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
        else
        {
            if (Vector2.SqrMagnitude(transform.position - beginPosition) < minDistance)
                BecomeCalm();
            else
                GoHome();
        }
    }

    /// <summary>
    /// Сменить главную цель
    /// </summary>
    protected override void ChangeMainTarget()
    {
    }

    #endregion //behaviorActions

    #region optimization

    /// <summary>
    /// Функция анализа в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
    }

    /// <summary>
    /// Перейти в оптимизированную версию
    /// </summary>
    protected override void ChangeBehaviorToOptimized()
    {
        transform.position = beginPosition;
        BecomeCalm();
    }

    #endregion //optimization

    #region eventHandlers

    /// <summary>
    /// Обработка события - была совершена атака
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void HandleAttackProcess(object sender, HitEventArgs e)
    {
        if (inHurricane)
        {
            CharacterController _char = null;
            if ((_char = e.Target.GetComponent<CharacterController>()) != null)
                if (_char.Buffs.Find(x => x.buffName == "FrozenProcess") == null)
                    StartCoroutine(FrostAttackProcess(_char));
        }
    }

    #endregion //eventHandlers

}

/// <summary>
/// Редактор призрака девушки
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(GhostLadyController))]
public class GhostLadyControllerEditor : AIControllerEditor
{

    GhostLadyController ghostLady;
    SerializedObject serGhostLady;

    SerializedProperty bossName,
    maxHP,
    health,
    balance,
    speed,
    acceleration,
    phase1AttackRate, 
    phase2AttackRate,
    loyalty,
    drop,
    fires,
    platform,
    vulnerability;

    bool usualAttackParametresShow = false;
    SerializedProperty missile,
    shootOffset,
    missileSpeed,
    attackParametres,
    usualAttackCooldown,
    usualAttackNearDistance,
    usualAttackTimes;

    bool fastAttackParametresShow = false;
    SerializedProperty preFastAttackTime,
    endFastAttackTime,   
    fastAttackCooldown,
    fastAttackTimes,
    fastAttackAngleDeviation,
    fastAttackPushForce;

    bool stalactiteAttackParametresShow = false;
    SerializedProperty stalactite,
    preStalactiteAttackTime,
    betweenStalactiteAttackTime,
    phase1StalactiteCount,
    phase2StalactiteCount,
    phase3StalactiteCount,
    phase1MaxStalactitesAtOneTime,
    phase2MaxStalactitesAtOneTime,
    phase3MaxStalactitesAtOneTime,
    stalactiteAttackCooldown;

    bool hurricaneAttackParametresShow = false;
    SerializedProperty hurricaneAttackParametres,
    frostAttackParametres,
    hurricaneSpeed,
    hurricaneAcceleration,
    hurricaneAttackRadius,
    hurricanePushForce,
    hurricaneAttackCooldown;

    bool diveAttackParametresShow=false;
    SerializedProperty diveAttackParametres,
    diveAttackMinTimes,
    diveAttackMaxTimes,
    preDiveSpeed, 
    preDiveAcceleration,
    diveSpeed,
    diveAcceleration,
    diveAttackPushForce,
    diveAttackDistance,
    diveAttackCooldown;

    SerializedProperty touchAttackParametres;

    public override void OnEnable()
    {
        ghostLady = (GhostLadyController)target;
        serGhostLady = new SerializedObject(ghostLady);

        bossName = serGhostLady.FindProperty("bossName");
        maxHP = serGhostLady.FindProperty("maxHealth");
        health = serGhostLady.FindProperty("health");
        balance = serGhostLady.FindProperty("balance");
        speed = serGhostLady.FindProperty("speed");
        acceleration = serGhostLady.FindProperty("acceleration");
        phase1AttackRate = serGhostLady.FindProperty("phase1AttackRate");
        phase2AttackRate = serGhostLady.FindProperty("phase2AttackRate");
        loyalty = serGhostLady.FindProperty("loyalty");
        drop = serGhostLady.FindProperty("drop");
        platform = serGhostLady.FindProperty("platform");
        fires = serGhostLady.FindProperty("fires");
        vulnerability = serGhostLady.FindProperty("vulnerability");

        missile = serGhostLady.FindProperty("missile");
        shootOffset = serGhostLady.FindProperty("shootOffset");
        missileSpeed = serGhostLady.FindProperty("missileSpeed");
        attackParametres = serGhostLady.FindProperty("attackParametres");
        usualAttackCooldown = serGhostLady.FindProperty("usualAttackCooldown");
        usualAttackNearDistance = serGhostLady.FindProperty("usualAttackNearDistance");
        usualAttackTimes = serGhostLady.FindProperty("usualAttackTimes");

        preFastAttackTime = serGhostLady.FindProperty("preFastAttackTime");
        endFastAttackTime = serGhostLady.FindProperty("endFastAttackTime");
        fastAttackCooldown = serGhostLady.FindProperty("fastAttackCooldown");
        fastAttackTimes = serGhostLady.FindProperty("fastAttackTimes");
        fastAttackAngleDeviation = serGhostLady.FindProperty("fastAttackAngleDeviation");
        fastAttackPushForce = serGhostLady.FindProperty("fastAttackPushForce");

        stalactite = serGhostLady.FindProperty("stalactite");
        preStalactiteAttackTime = serGhostLady.FindProperty("preStalactiteAttackTime");
        betweenStalactiteAttackTime = serGhostLady.FindProperty("betweenStalactiteAttackTime");
        phase1StalactiteCount = serGhostLady.FindProperty("phase1StalactiteCount");
        phase2StalactiteCount = serGhostLady.FindProperty("phase2StalactiteCount");
        phase3StalactiteCount = serGhostLady.FindProperty("phase3StalactiteCount");
        phase1MaxStalactitesAtOneTime = serGhostLady.FindProperty("phase1MaxStalactitesAtOneTime");
        phase2MaxStalactitesAtOneTime = serGhostLady.FindProperty("phase2MaxStalactitesAtOneTime");
        phase3MaxStalactitesAtOneTime = serGhostLady.FindProperty("phase3MaxStalactitesAtOneTime");
        stalactiteAttackCooldown = serGhostLady.FindProperty("stalactiteAttackCooldown");

        hurricaneAttackParametres = serGhostLady.FindProperty("hurricaneAttackParametres");
        frostAttackParametres = serGhostLady.FindProperty("frostAttackParametres");
        hurricaneSpeed = serGhostLady.FindProperty("hurricaneSpeed");
        hurricaneAcceleration = serGhostLady.FindProperty("hurricaneAcceleration");
        hurricaneAttackRadius = serGhostLady.FindProperty("hurricaneAttackRadius");
        hurricanePushForce = serGhostLady.FindProperty("hurricanePushForce");
        hurricaneAttackCooldown = serGhostLady.FindProperty("hurricaneAttackCooldown");

        diveAttackParametres = serGhostLady.FindProperty("diveAttackParametres");
        diveAttackMinTimes = serGhostLady.FindProperty("diveAttackMinTimes");
        diveAttackMaxTimes = serGhostLady.FindProperty("diveAttackMaxTimes");
        preDiveSpeed = serGhostLady.FindProperty("preDiveSpeed");
        preDiveAcceleration = serGhostLady.FindProperty("preDiveAcceleration");
        diveSpeed = serGhostLady.FindProperty("diveSpeed");
        diveAcceleration = serGhostLady.FindProperty("diveAcceleration");
        diveAttackPushForce = serGhostLady.FindProperty("diveAttackPushForce");
        diveAttackDistance = serGhostLady.FindProperty("diveAttackDistance");
        diveAttackCooldown = serGhostLady.FindProperty("diveAttackCooldown");

        touchAttackParametres = serGhostLady.FindProperty("touchAttackParametres");

    }

    public override void OnInspectorGUI()
    {

        EditorGUILayout.LabelField("General Parametres");

        EditorGUILayout.PropertyField(bossName,true);
        maxHP.floatValue = EditorGUILayout.FloatField("Max Health", maxHP.floatValue);
        EditorGUILayout.PropertyField(health);
        balance.intValue = EditorGUILayout.IntField("Balance", balance.intValue);
        speed.floatValue = EditorGUILayout.FloatField("Speed", speed.floatValue);
        acceleration.floatValue = EditorGUILayout.FloatField("Acceleration", acceleration.floatValue);
        EditorGUILayout.PropertyField(phase1AttackRate);
        EditorGUILayout.PropertyField(phase2AttackRate);
        EditorGUILayout.PropertyField(loyalty);
        EditorGUILayout.PropertyField(drop, true);
        EditorGUILayout.PropertyField(platform);
        EditorGUILayout.PropertyField(fires, true);
        ghostLady.Vulnerability = (byte)(DamageType)EditorGUILayout.EnumMaskPopup(new GUIContent("vulnerability"), (DamageType)ghostLady.Vulnerability);

        EditorGUILayout.Space();

        usualAttackParametresShow = EditorGUILayout.Foldout(usualAttackParametresShow, "Usual Attack Parametres");
        if (usualAttackParametresShow)
        {
            missile.objectReferenceValue = EditorGUILayout.ObjectField("Missile", missile.objectReferenceValue, typeof(GameObject));
            EditorGUILayout.PropertyField(shootOffset);
            EditorGUILayout.PropertyField(missileSpeed);
            EditorGUILayout.PropertyField(attackParametres, true);
            EditorGUILayout.PropertyField(usualAttackTimes);
            EditorGUILayout.PropertyField(usualAttackNearDistance);
            EditorGUILayout.PropertyField(usualAttackCooldown);
        }

        EditorGUILayout.Space();

        fastAttackParametresShow = EditorGUILayout.Foldout(fastAttackParametresShow, "Fast Attack Parametres");
        if (fastAttackParametresShow)
        {
            missile.objectReferenceValue = EditorGUILayout.ObjectField("Missile", missile.objectReferenceValue, typeof(GameObject));
            EditorGUILayout.PropertyField(shootOffset);
            EditorGUILayout.PropertyField(missileSpeed);
            EditorGUILayout.PropertyField(attackParametres, true);
            EditorGUILayout.PropertyField(preFastAttackTime);
            EditorGUILayout.PropertyField(endFastAttackTime);
            EditorGUILayout.PropertyField(fastAttackTimes);
            EditorGUILayout.PropertyField(fastAttackCooldown);
            EditorGUILayout.PropertyField(fastAttackAngleDeviation);
            EditorGUILayout.PropertyField(fastAttackPushForce);
        }

        EditorGUILayout.Space();
        stalactiteAttackParametresShow = EditorGUILayout.Foldout(stalactiteAttackParametresShow, "Stalactite Attack Parametres");
        if (stalactiteAttackParametresShow)
        {
            stalactite.objectReferenceValue = EditorGUILayout.ObjectField("Stalactite", stalactite.objectReferenceValue, typeof(GameObject));
            EditorGUILayout.PropertyField(preStalactiteAttackTime);
            EditorGUILayout.PropertyField(betweenStalactiteAttackTime);
            EditorGUILayout.PropertyField(phase1StalactiteCount);
            EditorGUILayout.PropertyField(phase2StalactiteCount);
            EditorGUILayout.PropertyField(phase3StalactiteCount);
            EditorGUILayout.PropertyField(phase1MaxStalactitesAtOneTime);
            EditorGUILayout.PropertyField(phase2MaxStalactitesAtOneTime);
            EditorGUILayout.PropertyField(phase3MaxStalactitesAtOneTime);
            EditorGUILayout.PropertyField(stalactiteAttackCooldown);
        }

        EditorGUILayout.Space();
        hurricaneAttackParametresShow = EditorGUILayout.Foldout(hurricaneAttackParametresShow, "Hurricane Attack Parametres");
        if (hurricaneAttackParametresShow)
        {
            EditorGUILayout.PropertyField(hurricaneAttackParametres,true);
            EditorGUILayout.PropertyField(frostAttackParametres, true);
            EditorGUILayout.PropertyField(hurricaneSpeed);
            EditorGUILayout.PropertyField(hurricaneAcceleration);
            EditorGUILayout.PropertyField(hurricaneAttackRadius);
            EditorGUILayout.PropertyField(hurricaneAttackCooldown);
            EditorGUILayout.PropertyField(hurricanePushForce);
        }

        EditorGUILayout.Space();
        diveAttackParametresShow = EditorGUILayout.Foldout(diveAttackParametresShow, "Dive Attack Parametres");
        if (diveAttackParametresShow)
        {
            EditorGUILayout.PropertyField(diveAttackParametres, true);
            EditorGUILayout.PropertyField(diveAttackMinTimes);
            EditorGUILayout.PropertyField(diveAttackMaxTimes);
            EditorGUILayout.PropertyField(diveAttackPushForce);
            EditorGUILayout.PropertyField(preDiveSpeed);
            EditorGUILayout.PropertyField(preDiveAcceleration);
            EditorGUILayout.PropertyField(diveSpeed);
            EditorGUILayout.PropertyField(diveAcceleration);
            EditorGUILayout.PropertyField(diveAttackDistance);
            EditorGUILayout.PropertyField(diveAttackCooldown);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(touchAttackParametres,true);

        serGhostLady.ApplyModifiedProperties();
    }

}
#endif //UNITY_EDITOR
