using UnityEngine;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Контроллер гигантской летучей мыши
/// </summary>
public class BatBossController: BossController
{
    #region consts

    protected const float pushBackForce = 800f;
    private const float batSize = .5f;

    private const float maxAvoidDistance = 30f, avoidOffset = 1.5f;

    #endregion //consts

    #region fields

    protected Hearing hearing;//Слух персонажа

    public GameObject heartDrop;//Что выпадает из летучей мыши, если её 2 раза ударить

    protected UsualTrigger batTrigger1, batTrigger2, batTrigger3;
    protected bool inBatTrigger1 { get { if (batTrigger1 == null) return false; else return batTrigger1.playerInside; } }
    protected bool inBatTrigger2 { get { if (batTrigger2 == null) return false; else return batTrigger2.playerInside; } }
    protected bool inBatTrigger3 { get { if (batTrigger3 == null) return false; else return batTrigger3.playerInside; } }
    protected GameObject batPosition1, batPosition2;

    #endregion //fields

    #region parametres

    [SerializeField]
    protected float phase2Health=150f;//С какого здоровья начинается вторая фаза босса?

    protected override float attackDistance { get { return 1.2f; } }

    protected Vector2 currentDirection = Vector2.right;//Направление курса полёта летучей мыши
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

    protected int damageCount = 0;//Подсчёт кол-ва нанесения урона

    [SerializeField]
    protected float healthDrain = 10f;//Сколько летучая мышь восстанавливает себе здоровья при укусе

    [SerializeField]
    private float attackForce = 300f;//С какой силой летучая мышь устремляется к противнику для совершения атаки
    [SerializeField]
    private float attackCooldown = 3f;
    private bool canAttack = true;
    private bool predictTargetNextPosition=false;//Анализируя текущую скорость героя, Летучая мышь будет пытаться предугадать следующее положение цели и пытаться атаковать туда.
    private bool inAttack = false;

    [SerializeField]
    protected SimpleCurveHitParametres lowDiveAttack = SimpleCurveHitParametres.zero, 
                                       mediumDiveAttack = SimpleCurveHitParametres.zero, 
                                       highDiveAttack = SimpleCurveHitParametres.zero;//Особые манёвренные атаки летучей мыши с разными траекториями полёта

    public SimpleCurveHitParametres LowDiveAttack { get { return lowDiveAttack; } set { lowDiveAttack = value; } }
    public SimpleCurveHitParametres MediumDiveAttack { get { return mediumDiveAttack; } set { mediumDiveAttack = value; } }
    public SimpleCurveHitParametres HighDiveAttack { get { return highDiveAttack; } set { highDiveAttack = value; } }

    [SerializeField]
    private int specialAttackBalance = 7;
    [SerializeField]
    private float specialAttackSpeed = .4f;
    [SerializeField]
    private float specialAttackCooldown = 10f;
    [SerializeField]
    private float specialAttackFrequence = 1f;//Как часто летучая мышь будет решать совершить специальную атаку
    [SerializeField]
    private float specialAttackProbability= .2f;//Вероятность нападения при помощи специальной атаки
    [SerializeField]
    private float specialNextAttackProbability = .67f;//Вероятность повторного нападения при помощи специальной атаки
    private int specialAttackTimes = 0;
    private BezierSimpleCurve currentCurve;
    private float currentCurveParameter = 0f;//Параметр передвижения по кривой
    private Vector2 curveEndPoint = Vector2.zero;
    private BezierSimpleCurve CurrentCurve { set { currentCurve = value;  currentCurveParameter = 0f; curveEndPoint = currentCurve.GetBezierPoint(1f); } }
    bool stillAgressive = false;//Переменная, используемая при переходе в патрулирующее состояние. Указывает на то что персонаж ещё не забыл о своём враге, просто временно меняет позицию для нанесения удара

    #endregion //parametres

    protected override void Update()
    {
        base.Update();
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        indicators = transform.FindChild("Indicators");
        hearing = indicators.GetComponentInChildren<Hearing>();
        hearing.hearingEventHandler += HandleHearingEvent;
        hearing.AllyHearing = false;
        base.Initialize();

        hitBox.AttackEventHandler += HandleAttackProcess;

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionIn = NullAreaFunction;
            areaTrigger.triggerFunctionOut = NullAreaFunction;
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            areaTrigger.InitializeAreaTrigger();
        }

        specialAttackTimes = 0;
        damageCount = 0;
        canAttack = true;
        predictTargetNextPosition = false;
        batTrigger1 = GameObject.Find("BatTrigger1").GetComponent<UsualTrigger>();
        batTrigger2 = GameObject.Find("BatTrigger2").GetComponent<UsualTrigger>();
        batTrigger3 = GameObject.Find("BatTrigger3").GetComponent<UsualTrigger>();
        batPosition1 = GameObject.Find("BatPosition1");
        batPosition2 = GameObject.Find("BatPosition2");

        BecomeCalm();

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
        Vector2 targetVelocity = (transform.position - currentTarget).normalized * speed * speedCoof;
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
        rigid.velocity = new Vector2(0f, 0f);
    }

    protected override void Turn()
    {
        if (!inAttack)
            base.Turn();
    }

    public override void Turn(OrientationEnum _orientation)
    {
        if (!inAttack)
            base.Turn(_orientation);
    }

    #endregion //movement

    #region attack

    /// <summary>
    /// Совершить обычную атаку
    /// </summary>
    protected override void Attack()
    {
        Animate(new AnimationEventArgs("stop"));
        hitBox.ResetHitBox();
        Animate(new AnimationEventArgs("attack", "Attack", Mathf.RoundToInt(100*attackParametres.wholeAttackTime)));
        StartCoroutine("AttackProcess");
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator AttackProcess()
    {
        RestartCooldown();
        employment = Mathf.Clamp(employment - 4, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.preAttackTime);
        StopMoving();
        if (predictTargetNextPosition && mainTarget.transform != null)
        {
            Rigidbody2D targetRigid = mainTarget.transform.GetComponent<Rigidbody2D>();
            Vector2 targetDistance = mainTarget - transform.position;
            float flightTime = targetDistance.magnitude / speed / speedCoof;
            Vector2 nextPoint = mainTarget + new Vector2(targetRigid.velocity.x,0f) * flightTime;
            CurrentDirection = (nextPoint - (Vector2)transform.position).normalized;
            rigid.AddForce(currentDirection * attackForce *1.3f* speedCoof);
        }
        else
        {
            CurrentDirection = (mainTarget - transform.position).normalized;
            rigid.AddForce(currentDirection * attackForce * speedCoof);
        }
        inAttack = true;
        hitBox.SetHitBox(new HitParametres(attackParametres));
        hitBox.AttackDirection = currentDirection;
        employment = Mathf.Clamp(employment + 1, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.actTime);
        StopMoving();
        CurrentDirection = Vector2.right * (int)orientation;
        employment = Mathf.Clamp(employment - 1, 0, maxEmployment);
        if (health>=phase2Health || predictTargetNextPosition)
            yield return new WaitForSeconds(attackParametres.endAttackTime);
        employment = Mathf.Clamp(employment + 4, 0, maxEmployment);
        inAttack = false;
        if (predictTargetNextPosition)
            predictTargetNextPosition = false;
        else if (health < phase2Health)
        {
            predictTargetNextPosition = true;
            Attack();
        }
    }

    /// <summary>
    /// Процесс после неудавшейся атаки, когда мышь ничего не может сделать
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator AttackShockProcess()
    {
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(1.1f);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Перезапустить кулдаун
    /// </summary>
    protected virtual void RestartCooldown()
    {
        StopCoroutine("CooldownProcess");
        StartCoroutine("CooldownProcess");
    }

    /// <summary>
    /// Процесс, в течение которого мышь не может совершать атаки
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator CooldownProcess()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    /// <summary>
    /// Принудительно прекратить атаку
    /// </summary>
    protected override void StopAttack()
    {
        base.StopAttack();
        employment = maxEmployment;
        predictTargetNextPosition = false;
        StopCoroutine("SpecialAttackProcess");
        StopCoroutine("AttackShockProcess");
        StartCoroutine("AttackShockProcess");
        balance = usualBalance;
        inAttack = false;
    }

    /// <summary>
    /// Совершить специальную атаку
    /// </summary>
    void SpecialAttack(SimpleCurveHitParametres _attackParametres)
    {
        BecomeAgressive();
        Vector2 pos = transform.position;
        if (pos.x > (batPosition1.transform.position + batPosition2.transform.position).x/2f)
            Turn(OrientationEnum.left);
        else
            Turn(OrientationEnum.right);
        CurrentCurve = new BezierSimpleCurve(_attackParametres.curve, pos, Mathf.Sign(transform.lossyScale.x), false);
        specialAttackTimes++;
        StartCoroutine("SpecialAttackProcess",_attackParametres.hitParametres);
        Animate(new AnimationEventArgs("attack", "Dive", Mathf.RoundToInt(100 * _attackParametres.hitParametres.wholeAttackTime)));
        behaviorActions = SpecialAttackBehavior;
    }

    /// <summary>
    /// Процесс совершения специальной атаки
    /// </summary>
    /// <param name="_attackParametres">параметры атаки</param>
    IEnumerator SpecialAttackProcess(HitParametres _attackParametres)
    {
        balance = specialAttackBalance;
        employment = Mathf.Clamp(employment - 4, 0, maxEmployment);
        yield return new WaitForSeconds(_attackParametres.preAttackTime);
        hitBox.SetHitBox(new HitParametres(_attackParametres));
        yield return new WaitForSeconds(_attackParametres.actTime + _attackParametres.endAttackTime);
        employment = Mathf.Clamp(employment + 4, 0, maxEmployment);
        balance = usualBalance;
    }

    /// <summary>
    /// Процесс кулдауна для специальной атаки
    /// </summary>
    /// <returns></returns>
    IEnumerator SpecialAttackCooldownProcess()
    {
        if (batPosition1 == null)
            yield break;
        yield return new WaitForSeconds(specialAttackCooldown);
        while (true)
        {
            if (employment > 8 && behavior == BehaviorEnum.agressive)
                if (UnityEngine.Random.RandomRange(0f, 1f) < specialAttackProbability)
                {
                    ChooseSpecialAttackPoint();
                    break;
                }
            yield return new WaitForSeconds(specialAttackFrequence);
        }
    }

    /// <summary>
    /// Выбрать, из какой позиции совершать специальную атаку
    /// </summary>
    void ChooseSpecialAttackPoint()
    {
        if (!mainTarget.Exists)
            return;
        Transform targetObject = null;
        stillAgressive = true;
        if (inBatTrigger1)
            targetObject=batTrigger2.transform;
        else if (inBatTrigger2)
            targetObject=batTrigger1.transform;
        else if (inBatTrigger3)
        {
            Vector2 pos = transform.position, point1 = batPosition1.transform.position, point2 = batPosition2.transform.position;
            if (Mathf.Abs(pos.x - point1.x) < Mathf.Abs(pos.x - point2.x))
                targetObject = batPosition1.transform;
            else
                targetObject = batPosition2.transform;
        }
        if (targetObject != null)
        {
            GoToThePoint(targetObject.position);
            currentTarget = new ETarget(targetObject);
        }
        else
            BecomeAgressive();
        stillAgressive = false;
    }

    /// <summary>
    /// Выбрать, какую из специальных атак стоит совершить
    /// </summary>
    void ChooseSpecialAttack()
    {
        CurrentCurve = BezierSimpleCurve.zero;
        Vector2 pos = transform.position;
        if (Vector2.SqrMagnitude(pos - (Vector2)batTrigger1.transform.position) < minDistance || 
            Vector2.SqrMagnitude(pos - (Vector2)batTrigger2.transform.position) < minDistance)
            SpecialAttack(highDiveAttack);
        else if (Vector2.SqrMagnitude(pos - (Vector2)batPosition1.transform.position) < minDistance || 
                  Vector2.SqrMagnitude(pos - (Vector2)batPosition2.transform.position) < minDistance)
        {
            if (UnityEngine.Random.RandomRange(0f, 1f) < .7f)
                SpecialAttack(lowDiveAttack);
            else
                SpecialAttack(mediumDiveAttack);
        }
    }

    /// <summary>
    /// Остановить специальную атаку
    /// </summary>
    void StopSpecialAttack()
    {
        specialAttackTimes = 0;
        if (currentCurve != BezierSimpleCurve.zero)
        {
            CurrentCurve = BezierSimpleCurve.zero;
            StopCoroutine("SpecialAttackProcess");
            BecomeAgressive();
        }
    }

    /// <summary>
    /// Функция, вызываемая при получении урона, оповещающая о субъекте нападения
    /// </summary>
    /// <param name="attacker">Кто атаковал персонажа</param>
    public override void TakeAttackerInformation(AttackerClass attacker)
    {
        if (attacker != null)
        {
            if (mainTarget.transform != attacker.attacker.transform)
                MainTarget = new ETarget(attacker.attacker.transform);
            if (behavior == BehaviorEnum.calm)
                BecomeAgressive();
        }
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(HitParametres hitData)
    {
        if (hitData.damageType != DamageType.Physical)
        {
            if (((DamageType)vulnerability & hitData.damageType) == hitData.damageType)
                hitData.damage *= 1.25f;
            else if (hitData.damageType == attackParametres.damageType)
                hitData.damage *= .9f;//Если урон совпадает с типом атаки персонажа, то он ослабевается (бить огонь огнём - не самая гениальная затея)
        }
        Health = Mathf.Clamp(Health - hitData.damage, 0f, maxHealth);
        if (health <= 0f)
        {
            Death();
            return;
        }

        if ((hitData.damageType != DamageType.Physical) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
            TakeDamageEffect(hitData.damageType);
        bool stunned = GetBuff("StunnedProcess") != null;
        bool frozen = GetBuff("FrozenProcess") != null;
        if (hitData.attackPower > balance || frozen || stunned)
        {
            StopMoving();
            balance = usualBalance;
            if (!frozen && !stunned)
            {
                StopCoroutine("Microstun");
                StartCoroutine("Microstun");
            }
            StopAttack();
            StopSpecialAttack();
            employment = maxEmployment;
            damageCount++;
            if (damageCount >= 5)
            {
                Instantiate(heartDrop, transform.position, transform.rotation);
                damageCount = 0;
            }
            if (behavior == BehaviorEnum.patrol)
                BecomeAgressive();
        }
        Animate(new AnimationEventArgs("hitted", "", hitData.attackPower > balance ? 0 : 1));
    }

    #region damageEffects

    /// <summary>
    /// Оглушить
    /// </summary>
    protected override void BecomeStunned(float _time)
    {
        if (GetBuff("StunnedProcess") != null)//Если на персонаже уже висит стан, то нельзя навесить ещё один
            return;
        StopMoving();
        StartCoroutine("StunnedProcess", _time == 0 ? stunTime : _time);
    }

    /// <summary>
    /// Поджечь
    /// </summary>
    protected override void BecomeBurning(float _time)
    {
        base.BecomeBurning(_time);
        if (GetBuff("BurningProcess") != null)
            return;
        if (GetBuff("WetProcess") != null)
            return;//Нельзя мокрого персонажа
        StopCold();//Согреться
        if (GetBuff("FrozenProcess") != null)
        {
            StopFrozen();//Если персонажа подожгли, когда он был заморожен, то он отмараживается и не получает никакого урона от огня, так как считаем, что всё тепло ушло на разморозку
            return;
        }
        StartCoroutine("BurningProcess", _time == 0 ? burningTime : _time);
    }

    #endregion //damageEffects

    #endregion //attack

    #region behaviourActions

    /// <summary>
    /// Функция, ответственная за анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        Vector2 pos = transform.position;
        if (currentCurve != BezierSimpleCurve.zero)
            return;
        if (rigid.velocity.magnitude < minSpeed && employment>7)
        {
            float angle = 0f, deltaAngle=Mathf.PI/4f;
            Vector2 rayDirection;
            for (int i = 0; i < 8; i++)
            {
                
                rayDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (Physics2D.Raycast(pos, rayDirection, batSize, LayerMask.GetMask(gLName)))
                {
                    rigid.AddForce(-rayDirection * pushBackForce / 2f);
                    break;
                }
                angle += deltaAngle;
            }
        }
        if (behavior == BehaviorEnum.agressive)
        {
            if (currentTarget.exists)
            {
                if (currentTarget != mainTarget)
                {
                    Vector2 direction = (mainTarget - pos).normalized;
                    RaycastHit2D hit = Physics2D.Raycast(pos + direction* batSize, direction, sightRadius, LayerMask.GetMask(cLName));
                    if (hit)
                        if (hit.collider.transform == mainTarget.transform)
                            currentTarget = mainTarget;
                }
                if (Physics2D.Raycast(pos, currentTarget - pos, batSize, LayerMask.GetMask(gLName)))
                {
                    currentTarget = FindPath();
                }
            }
        }
    }

    /// <summary>
    /// Обновить данные, чтобы они стали пригодны для работы моделей поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        CurrentDirection = Vector2.right * (int)orientation;
        StopCoroutine("SpecialAttackProcess");
        balance = usualBalance;
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        rigid.isKinematic = false;
        hearing.enabled = false;
        RestartCooldown();
        if (currentCurve==BezierSimpleCurve.zero)
            StartCoroutine("SpecialAttackCooldownProcess");
        Animate(new AnimationEventArgs("fly"));
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        specialAttackTimes = 0;
        CurrentCurve = BezierSimpleCurve.zero;
        StopCoroutine("SpecialAttackCooldownProcess");
        rigid.isKinematic = true;
        hearing.enabled = true;
    }

    /// <summary>
    /// Перейти в состояние патрулирования
    /// </summary>
    protected override void BecomePatrolling()
    {
        RefreshTargets();
        behavior = BehaviorEnum.patrol;
        //mainTarget.Exists = false;
        if (optimized)
            behaviorActions = PatrolOptBehavior;
        else
            behaviorActions = PatrolBehavior;
        OnChangeBehavior(new BehaviorEventArgs(BehaviorEnum.patrol));
        specialAttackTimes = 0;
        CurrentCurve = BezierSimpleCurve.zero;
        StopCoroutine("SpecialAttackCooldownProcess");
        rigid.isKinematic = false;
        balance = specialAttackBalance;
        if (!stillAgressive)
        {
            hearing.enabled = true;
            TargetCharacter = null;
        }
        Animate(new AnimationEventArgs("fly"));
    }

    /// <summary>
    /// Выдвинуться к целевой позиции
    /// </summary>
    /// <param name="targetPosition">Целевая позиция</param>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        BecomePatrolling();
        currentTarget = new ETarget( targetPosition);
    }

    /// <summary>
    /// Вернуться домой
    /// </summary>
    public override void GoHome()
    {
        BecomePatrolling();
        currentTarget = new ETarget(beginPosition);
    }


    /// <summary>
    /// Простейший алгоритм обхода препятствий
    /// </summary>
    protected ETarget FindPath()
    {
        Vector2 pos = transform.position;

        bool a1 = Physics2D.Raycast(pos, Vector2.up, batSize, LayerMask.GetMask(gLName)) && (mainTarget.y - pos.y > avoidOffset);
        bool a2 = Physics2D.Raycast(pos, Vector2.right, batSize, LayerMask.GetMask(gLName)) && (mainTarget.x > pos.x);
        bool a3 = Physics2D.Raycast(pos, Vector2.down, batSize, LayerMask.GetMask(gLName)) && (mainTarget.y - pos.y < avoidOffset);
        bool a4 = Physics2D.Raycast(pos, Vector2.left, batSize, LayerMask.GetMask(gLName)   ) && (mainTarget.x < pos.x);

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
            while (Physics2D.Raycast(pos1, aimDirection, batSize, LayerMask.GetMask(gLName)) && ((pos1 - pos).magnitude < maxAvoidDistance))
                pos1 += vect1 * batSize;
            open1 = !Physics2D.Raycast(pos1, aimDirection, batSize, LayerMask.GetMask(gLName));
            while (Physics2D.Raycast(pos2, aimDirection, batSize, LayerMask.GetMask(gLName)) && ((pos2 - pos).magnitude < maxAvoidDistance))
                pos2 += vect2 * batSize;
            open2 = !Physics2D.Raycast(pos2, aimDirection, batSize, LayerMask.GetMask(gLName));
            Vector2 newTargetPosition = (open1 && !open2) ? pos1 : (open2 && !open1) ? pos2 : ((mainTarget - pos1).magnitude < (mainTarget - pos2).magnitude) ? pos1 : pos2;
            return new ETarget(newTargetPosition);
        }
    }

    /// <summary>
    /// Спокойное поведение
    /// </summary>
    protected override void CalmBehavior()
    {
        //if (!immobile)
        //{
            if (rigid.velocity.magnitude < minSpeed)
            {
                Animate(new AnimationEventArgs("idle"));
            }
            else
            {
                Animate(new AnimationEventArgs("fly"));
            }
        //}
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        if (employment < 8 || !mainTarget.exists || !currentTarget.exists || immobile)
        {
            Animate(new AnimationEventArgs("fly"));
            return;
        }

        Vector2 targetPosition = currentTarget;
        Vector2 pos = transform.position;
        if (currentTarget == mainTarget)
        {
            Vector2 targetDistance = targetPosition - pos;
            float sqDistance = targetDistance.sqrMagnitude;
            if (canAttack ? sqDistance < attackDistance * attackDistance ? (!Physics2D.Raycast(pos,targetDistance,targetDistance.magnitude,LayerMask.GetMask(gLName))):false: false)
            {
                StopMoving();
                if (targetDistance.x * (int)orientation < 0f)
                    Turn();
                Attack();
            }
            else if (sqDistance > attackDistance * attackDistance / 2f)
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
            else if (sqDistance < attackDistance * attackDistance / 4f)
                MoveAway((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(-targetDistance.x)));
            else
            {
                StopMoving();
                if (targetDistance.x * (int)orientation < 0f)
                    Turn();
            }
        }
        else
        {
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
            if (currentTarget != mainTarget && Vector2.SqrMagnitude(targetPosition - pos) < batSize * batSize)
            {
                currentTarget = FindPath();
            }
        }

        Animate(new AnimationEventArgs("fly"));
    }

    /// <summary>
    /// Полёт летучей мыши при специальной атаке
    /// </summary>
    void SpecialAttackBehavior()
    {
        if (currentCurve == BezierSimpleCurve.zero || immobile)
        {
            BecomeAgressive();
            return;
        }

        Vector2 pos = transform.position;
        currentCurveParameter = Mathf.Clamp(currentCurveParameter + Time.fixedDeltaTime * specialAttackSpeed, 0f, 1f);
        Vector2 nextPosition = currentCurve.GetBezierPoint(currentCurveParameter);
        CurrentDirection = (nextPosition - pos).normalized;
        transform.position = nextPosition;

        if (Vector2.SqrMagnitude(pos - curveEndPoint) < minCellSqrMagnitude)//Закончили перемещение по кривой
        {
            if (specialAttackTimes < 2)
                ChooseSpecialAttack();
            else if (health < phase2Health? UnityEngine.Random.RandomRange(0f, 1f) < Mathf.Pow(specialNextAttackProbability, specialAttackTimes - 1): false)
                    ChooseSpecialAttack();
            else
            {
                specialAttackTimes = 0;
                CurrentCurve = BezierSimpleCurve.zero;
                BecomeAgressive();
            }
        }

    }

    /// <summary>
    /// Поведение преследования какой-либо цели
    /// </summary>
    protected override void PatrolBehavior()
    {

        if (immobile)
        {
            Animate(new AnimationEventArgs("fly"));
            return;
        }
        base.PatrolBehavior();

        Vector2 targetPosition = currentTarget;
        Vector2 pos = transform.position;
        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
        if (Vector2.SqrMagnitude(currentTarget - pos) < minDistance * minDistance)
        {
            StopMoving();
            if (currentTarget.transform != null && mainTarget.exists)
                ChooseSpecialAttack();
            else if (Vector2.SqrMagnitude(targetPosition - beginPosition) > minDistance * minDistance)
                GoHome();
            else
                BecomeCalm();
        }
        Animate(new AnimationEventArgs("fly"));
    }

    #endregion //behaviourActions

    #region events

    /*
    /// <summary>
    /// Обработка события "Услышал врага"
    /// </summary>
    protected virtual void HandleHearingEvent(object sender, EventArgs e)
    {
        BecomeAgressive();
    }
    */

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected override void HandleAttackProcess(object sender, HitEventArgs e)
    {
        if (e.HPDif < 0f)
        {
            Health = Mathf.Clamp(health + healthDrain, 0f, maxHealth);
        }
    }

    public void OnDrawGizmosSelected()
    {
        // отобразим кривые как 50 сегментов
        if (lowDiveAttack.curve.draw)
        {
            BezierSimpleCurve curve1 = new BezierSimpleCurve(lowDiveAttack.curve,transform.position, Mathf.Sign(transform.lossyScale.x));
            Gizmos.color = Color.red;
            for (int i = 1; i < 50; i++)
            {
                float t = (i - 1f) / 49f;
                float t1 = i / 49f;
                Gizmos.DrawLine(curve1.GetBezierPoint(t), curve1.GetBezierPoint(t1));
            }
        }
        if (mediumDiveAttack.curve.draw)
        {
            BezierSimpleCurve curve2 = new BezierSimpleCurve(mediumDiveAttack.curve, transform.position, Mathf.Sign(transform.lossyScale.x));
            Gizmos.color = Color.yellow;
            for (int i = 1; i < 50; i++)
            {
                float t = (i - 1f) / 49f;
                float t1 = i / 49f;
                Gizmos.DrawLine(curve2.GetBezierPoint(t), curve2.GetBezierPoint(t1));
            }
        }
        if (highDiveAttack.curve.draw)
        {
            BezierSimpleCurve curve3 = new BezierSimpleCurve(highDiveAttack.curve, transform.position,Mathf.Sign(transform.lossyScale.x));
            Gizmos.color = Color.green;
            for (int i = 1; i < 50; i++)
            {
                float t = (i - 1f) / 49f;
                float t1 = i / 49f;
                Gizmos.DrawLine(curve3.GetBezierPoint(t), curve3.GetBezierPoint(t1));
            }
        }
    }

    #endregion //events
}

/// <summary>
/// Редактор персонажей с ИИ
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(BatBossController))]
public class BatBossControllerEditor : AIControllerEditor
{

    public override void OnInspectorGUI()
    {
        BatBossController batBoss = (BatBossController)target;

        SerializedObject serBatBoss = new SerializedObject(batBoss);
        SerializedProperty bossName = serBatBoss.FindProperty("bossName");
        SerializedProperty maxHP = serBatBoss.FindProperty("maxHealth");
        SerializedProperty health = serBatBoss.FindProperty("health");
        SerializedProperty phase2Health = serBatBoss.FindProperty("phase2Health");
        SerializedProperty balance = serBatBoss.FindProperty("balance");
        SerializedProperty speed = serBatBoss.FindProperty("speed");
        SerializedProperty loyalty = serBatBoss.FindProperty("loyalty");
        SerializedProperty acceleration = serBatBoss.FindProperty("acceleration");
        SerializedProperty heartDrop = serBatBoss.FindProperty("heartDrop");
        SerializedProperty drop = serBatBoss.FindProperty("drop");
        SerializedProperty healthDrain = serBatBoss.FindProperty("healthDrain");
        SerializedProperty attackForce = serBatBoss.FindProperty("attackForce");
        SerializedProperty attackCooldown = serBatBoss.FindProperty("attackCooldown");
        SerializedProperty specialAttackBalance = serBatBoss.FindProperty("specialAttackBalance");
        SerializedProperty specialAttackSpeed = serBatBoss.FindProperty("specialAttackSpeed");
        SerializedProperty specialAttackCooldown = serBatBoss.FindProperty("specialAttackCooldown");
        SerializedProperty specialAttackFrequence = serBatBoss.FindProperty("specialAttackFrequence");
        SerializedProperty specialAttackProbability = serBatBoss.FindProperty("specialAttackProbability");
        SerializedProperty specialNextAttackProbability = serBatBoss.FindProperty("specialNextAttackProbability");
        SerializedProperty serLowDiveAttack = serBatBoss.FindProperty("lowDiveAttack");
        SerializedProperty serMediumDiveAttack = serBatBoss.FindProperty("mediumDiveAttack");
        SerializedProperty serHighDiveAttack = serBatBoss.FindProperty("highDiveAttack");
        SerializedProperty serAttackParametres = serBatBoss.FindProperty("attackParametres");

        bossName.stringValue = EditorGUILayout.TextField("Boss Name", bossName.stringValue);
        maxHP.floatValue = EditorGUILayout.FloatField("Max HP", maxHP.floatValue);
        EditorGUILayout.PropertyField(health);
        EditorGUILayout.PropertyField(phase2Health);
        balance.intValue = EditorGUILayout.IntField("Balance", balance.intValue);
        speed.floatValue = EditorGUILayout.FloatField("Speed", speed.floatValue);
        EditorGUILayout.PropertyField(loyalty);
        acceleration.floatValue = EditorGUILayout.FloatField("Acceleration", acceleration.floatValue);
        EditorGUILayout.PropertyField(heartDrop);
        EditorGUILayout.PropertyField(drop);
        healthDrain.floatValue = EditorGUILayout.FloatField("Health Drain", healthDrain.floatValue);
        EditorGUILayout.PropertyField(serAttackParametres,true);
        attackForce.floatValue = EditorGUILayout.FloatField("Attack Force", attackForce.floatValue);
        attackCooldown.floatValue = EditorGUILayout.FloatField("Attack Cooldown", attackCooldown.floatValue);
        EditorGUILayout.PropertyField(serLowDiveAttack,true);
        EditorGUILayout.PropertyField(serMediumDiveAttack, true);
        EditorGUILayout.PropertyField(serHighDiveAttack, true);
        specialAttackBalance.intValue = EditorGUILayout.IntField("Special Attack Balance", specialAttackBalance.intValue);
        specialAttackSpeed.floatValue = EditorGUILayout.FloatField("Special Attack Speed", specialAttackSpeed.floatValue);
        specialAttackCooldown.floatValue = EditorGUILayout.FloatField("Special Attack Cooldown", specialAttackCooldown.floatValue);
        specialAttackFrequence.floatValue = EditorGUILayout.FloatField("Special Attack Frequence", specialAttackFrequence.floatValue);
        specialAttackProbability.floatValue = EditorGUILayout.FloatField("Special Attack Probability", specialAttackProbability.floatValue);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Special Next Attack Probability", GUILayout.Width(200f));
        specialNextAttackProbability.floatValue = EditorGUILayout.FloatField(specialNextAttackProbability.floatValue);
        EditorGUILayout.EndHorizontal();

        batBoss.Vulnerability = (byte)(DamageType)EditorGUILayout.EnumMaskPopup(new GUIContent("vulnerability"), (DamageType)batBoss.Vulnerability);

        serBatBoss.ApplyModifiedProperties();
    }

    public void OnSceneGUI()
    {

        BatBossController batBoss = (BatBossController)target;
        BezierSimpleCurve curve1 = new BezierSimpleCurve(batBoss.LowDiveAttack.curve,batBoss.transform.position, Mathf.Sign(batBoss.transform.lossyScale.x));
        BezierSimpleCurve curve2 = new BezierSimpleCurve(batBoss.MediumDiveAttack.curve,batBoss.transform.position, Mathf.Sign(batBoss.transform.lossyScale.x));
        BezierSimpleCurve curve3 = new BezierSimpleCurve(batBoss.HighDiveAttack.curve,batBoss.transform.position, Mathf.Sign(batBoss.transform.lossyScale.x));
        if (curve1.draw)
        {
            //Нарисуем линии манипуляторов
            Handles.DrawLine(curve1.p0, curve1.p1);
            Handles.DrawLine(curve1.p2, curve1.p3);

            // Для каждой контрольной точки создаем манипулятор в виде сферы
            Quaternion rot = Quaternion.identity;
            float size = HandleUtility.GetHandleSize(curve1.p0) * 0.2f;
            curve1.p0 = Handles.FreeMoveHandle(curve1.p0, rot, size, Vector3.zero, Handles.SphereCap);
            curve1.p1 = Handles.FreeMoveHandle(curve1.p1, rot, size, Vector3.zero, Handles.SphereCap);
            curve1.p2 = Handles.FreeMoveHandle(curve1.p2, rot, size, Vector3.zero, Handles.SphereCap);
            curve1.p3 = Handles.FreeMoveHandle(curve1.p3, rot, size, Vector3.zero, Handles.SphereCap);
            if (GUI.changed)
            {
                SimpleCurveHitParametres attack = batBoss.LowDiveAttack;
                float coof = Mathf.Sign(batBoss.transform.lossyScale.x) > 0 ? 1 : -1;
                Vector2 offset = batBoss.transform.position;
                Vector2 v0 = curve1.p0 - offset, v1 = curve1.p1 - offset, v2 = curve1.p2 - offset, v3 = curve1.p3 - offset;
                v0 = new Vector2(v0.x * coof, v0.y); v1 = new Vector2(v1.x * coof, v1.y); v2 = new Vector2(v2.x * coof, v2.y); v3 = new Vector2(v3.x * coof, v3.y);
                BezierSimpleCurve newCurve = new BezierSimpleCurve(v0, v1, v2, v3, true);
                attack.curve = newCurve;
                batBoss.LowDiveAttack = attack;
            }
        }

        if (curve2.draw)
        {
            //Нарисуем линии манипуляторов
            Handles.DrawLine(curve2.p0, curve2.p1);
            Handles.DrawLine(curve2.p2, curve2.p3);

            // Для каждой контрольной точки создаем манипулятор в виде сферы
            Quaternion rot = Quaternion.identity;
            float size = HandleUtility.GetHandleSize(curve2.p0) * 0.2f;
            curve2.p0 = Handles.FreeMoveHandle(curve2.p0, rot, size, Vector3.zero, Handles.SphereCap);
            curve2.p1 = Handles.FreeMoveHandle(curve2.p1, rot, size, Vector3.zero, Handles.SphereCap);
            curve2.p2 = Handles.FreeMoveHandle(curve2.p2, rot, size, Vector3.zero, Handles.SphereCap);
            curve2.p3 = Handles.FreeMoveHandle(curve2.p3, rot, size, Vector3.zero, Handles.SphereCap);
            if (GUI.changed)
            {
                SimpleCurveHitParametres attack = batBoss.MediumDiveAttack;
                float coof = Mathf.Sign(batBoss.transform.lossyScale.x) > 0 ? 1 : -1;
                Vector2 offset = batBoss.transform.position;
                Vector2 v0 = curve2.p0 - offset, v1 = curve2.p1 - offset, v2 = curve2.p2 - offset, v3 = curve2.p3 - offset;
                v0 = new Vector2(v0.x * coof, v0.y); v1 = new Vector2(v1.x * coof, v1.y); v2 = new Vector2(v2.x * coof, v2.y); v3 = new Vector2(v3.x * coof, v3.y);
                BezierSimpleCurve newCurve = new BezierSimpleCurve(v0, v1, v2, v3, true);
                attack.curve = newCurve;
                batBoss.MediumDiveAttack = attack;
            }
        }

        if (curve3.draw)
        {
            //Нарисуем линии манипуляторов
            Handles.DrawLine(curve3.p0, curve3.p1);
            Handles.DrawLine(curve3.p2, curve3.p3);

            // Для каждой контрольной точки создаем манипулятор в виде сферы
            Quaternion rot = Quaternion.identity;
            float size = HandleUtility.GetHandleSize(curve3.p0) * 0.2f;
            curve3.p0 = Handles.FreeMoveHandle(curve3.p0, rot, size, Vector3.zero, Handles.SphereCap);
            curve3.p1 = Handles.FreeMoveHandle(curve3.p1, rot, size, Vector3.zero, Handles.SphereCap);
            curve3.p2 = Handles.FreeMoveHandle(curve3.p2, rot, size, Vector3.zero, Handles.SphereCap);
            curve3.p3 = Handles.FreeMoveHandle(curve3.p3, rot, size, Vector3.zero, Handles.SphereCap);
            if (GUI.changed)
            {
                SimpleCurveHitParametres attack = batBoss.HighDiveAttack;
                float coof = Mathf.Sign(batBoss.transform.lossyScale.x) > 0 ? 1 : -1;
                Vector2 offset = batBoss.transform.position;
                Vector2 v0 = curve3.p0 - offset, v1 = curve3.p1 - offset, v2 = curve3.p2 - offset, v3 = curve3.p3 - offset;
                v0 = new Vector2(v0.x * coof, v0.y); v1 = new Vector2(v1.x * coof, v1.y); v2 = new Vector2(v2.x * coof, v2.y); v3 = new Vector2(v3.x * coof, v3.y);
                BezierSimpleCurve newCurve=new BezierSimpleCurve(v0,v1, v2,v3,true);
                attack.curve = newCurve;
                batBoss.HighDiveAttack = attack;
            }
        }

        // если мы двигали контрольные точки, то мы должны указать редактору, 
        // что объект изменился (стал "грязным")
        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }


}
#endif //UNITY_EDITOR