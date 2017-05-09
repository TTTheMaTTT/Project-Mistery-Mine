using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Базовый класс для персонажей, управляемых ИИ
/// </summary>
public class AIController : CharacterController
{

    #region consts

    protected const float sightOffset = 0.1f;
    protected const float microStun = .4f;
    protected const float minDistance = .04f;
    protected const float minAngle = 1f;
    protected const float minSpeed = .1f;

    protected const string gLName = "ground";//Название слоя земли
    protected const string wLName = "Water";//Название слоя воды

    protected float underwaterCheckOffset = 0.053f;//Вертикальное смещение точки проверки на местонахождение под водой относительно положения персонажа
    protected const float waterRadius = .01f;

    protected const float optTimeStep = .75f;//Как часто работает оптимизированная версия персонажа

    #endregion //consts

    #region delegates

    protected delegate void ActionDelegate();

    #endregion //delegates

    #region eventHandlers

    public EventHandler<BehaviorEventArgs> BehaviorChangeEvent;//Событие о смене модели поведения
    public EventHandler<LoyaltyEventArgs> LoyaltyChangeEvent;//Событие о смене стороны конфликта
    public EventHandler<HealthEventArgs> healthChangedEvent;

    #endregion //eventHandlers

    #region fields

    [SerializeField]protected int id;//ID монстра, по которому его можно отличить
    public virtual int ID
    {
        get
        {
            return id;
        }
        set
        {
            id = value;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif //UNITY_EDITOR 
        }
    }

    [SerializeField]protected string monsterName;//Как называется монстр

    protected ETarget mainTarget = ETarget.zero;//Что является целью ИИ
    protected CharacterController targetCharacter = null;//Какой персонаж является главной целью ИИ
    protected virtual CharacterController TargetCharacter
    {
        get
        {
            return targetCharacter;
        }
        set
        {
            if (targetCharacter != null)
                targetCharacter.CharacterDeathEvent -= HandleTargetDeathEvent;
            targetCharacter = value;
            if (targetCharacter != null)
                targetCharacter.CharacterDeathEvent += HandleTargetDeathEvent;   
        }
    }

    public virtual ETarget MainTarget
    {
        get
        {
            return mainTarget;
        }
        set
        {
            mainTarget = value;
            prevTargetPosition = EVector3.zero;
            if (mainTarget.transform != null)
            {
                TargetCharacter = mainTarget.transform.GetComponent<CharacterController>();
            }
            else
                TargetCharacter = null;
        }
    }

    protected ETarget currentTarget = ETarget.zero;//Что является текущей целью ИИ
    public virtual ETarget CurrentTarget { get { return currentTarget; } }
    protected List<NavigationCell> waypoints;//Маршрут следования
    protected virtual List<NavigationCell> Waypoints { get { return waypoints; } set { StopFollowOptPath(); waypoints = value; } }

    protected ActionDelegate behaviorActions;
    protected ActionDelegate analyseActions;

    protected AreaTrigger areaTrigger;//Триггер области вхождения монстра. Если герой находится в ней, то у монстра включаются все функции
    public AreaTrigger ATrigger { get { return areaTrigger; } }

    protected NavigationMap navMap;

    [SerializeField]
    protected List<GameObject> drop = new List<GameObject>();//что скидывается с персонажа после смерти
    [SerializeField][Range(0f,1)]protected float dropProbability = 0f;//Вероятность, что с персонажа выпадет дроп

    #endregion //fields

    #region parametres

    [SerializeField]protected HitParametres attackParametres;//Какие параметры атаки, производимой персонажем
    public HitParametres AttackParametres { get { return attackParametres; } set { attackParametres = value; } }
    protected string attackName = "";//Название атаки, совершаемой в данный момент. 
                                    //Используется, чтобы поставить кулдаун на прерванную атаку, если что-то сбило персонажа с процесса атаки
    protected int usualBalance;

    protected virtual float attackDistance { get { return .2f; } }//На каком расстоянии должен стоять ИИ, чтобы решить атаковать
    protected virtual int attackBalance { get { return 3; } }//Баланс персонажа при совершении особой атаки
    protected virtual float sightRadius { get { return 1.9f; } }
    protected virtual float beCalmTime { get { return 10f; } }//Время через которое ИИ перестаёт преследовать игрока, если он ушёл из их поля зрения
    protected virtual float avoidTime { get { return 1f; } }//Время, спустя которое можно судить о необходимости обхода препятствия
    protected virtual int maxAgressivePathDepth { get { return 60; } }//Насколько сложен может быть путь ИИ, в агрессивном состоянии 
                                                                      //(этот путь используется в тех случаях, когда невозможно настичь героя прямым путём)

    protected virtual float allyDistance { get { return .25f; } }//На каком расстоянии держится от своего союзника персонаж (возвращается квадрат расстояния
    protected virtual float allyTime { get { return .9f; } }
    protected bool followAlly = true;

    protected BehaviorEnum behavior = BehaviorEnum.calm;
    public BehaviorEnum Behavior { get { return behavior; } set { behavior = value; } }

    protected string cLName = "hero";//Название слоя персонажа
    [SerializeField]protected LoyaltyEnum loyalty = LoyaltyEnum.enemy;//Как относится ИИ к главному герою
    public virtual LoyaltyEnum Loyalty
    {
        get
        {
            return loyalty;
        }
        set
        {
            OnChangeLoyalty(new LoyaltyEventArgs(value));
            if (loyalty == LoyaltyEnum.ally && value != LoyaltyEnum.ally)
                return;//Если персонаж стал союзником, то считаем, что он не сможет снова сменить свою лояльность
            loyalty = value;
            if (value == LoyaltyEnum.enemy)
            {
                gameObject.layer = LayerMask.NameToLayer("character");
                gameObject.tag = "enemy";
                cLName = "hero";
                enemies.Remove("enemy");
                enemies.Add("player");
                enemies.Add("ally");
                Animate(new AnimationEventArgs("stopAlly"));
                if (hitBox != null)
                {
                    hitBox.allyHitBox = false;
                    hitBox.SetEnemies(enemies);
                }
            }
            else if (value == LoyaltyEnum.ally)
            {
                gameObject.layer = LayerMask.NameToLayer("hero");
                gameObject.tag = "ally";
                cLName = "character";
                enemies.Remove("player");
                enemies.Add("enemy");
                enemies.Remove("ally");
                attackParametres.damage = maxHealth / 10f;//Урон должен быть соизмеримым с здоровьм персонажа
                maxHealth = SpecialFunctions.Player.GetComponent<HeroController>().MaxHealth;
                health = maxHealth;
                dead = false;
                Animate(new AnimationEventArgs("startAlly"));
                if (hitBox != null)
                {
                    hitBox.allyHitBox = true;
                    hitBox.SetEnemies(enemies);
                }
                beginPosition = new ETarget(SpecialFunctions.Player.transform);//Союзники стремятся находится рядом с главным героем
                if (areaTrigger != null)
                {
                    areaTrigger.triggerFunctionOut += AreaTriggerExitDeath;
                }
                BecomeCalm();
                MainTarget = ETarget.zero;
            }
            else
            {
                gameObject.layer = LayerMask.NameToLayer("neutralCharacter");
                cLName = "ground";
            }
            OnChangeLoyalty(new LoyaltyEventArgs(value));
        }
    }

    protected bool waiting = false;//Находится ли персонаж в тактическом ожидании?
    public virtual bool Waiting { get { return waiting; } set { waiting = value; } }
    protected virtual float waitingNearDistance { get { return 1f; } }//Если цель ближе этого расстояния, то в режиме ожидания персонаж будет убегать от неё
    protected virtual float waitingFarDistance { get { return 1.6f; } }//Если цель дальше этого расстояния, то в режиме ожидания персонаж будет стремиться к ней

    [SerializeField]protected float acceleration = 1f;

    [SerializeField] [HideInInspector]protected byte vulnerability = 0;
    public byte Vulnerability { get { return vulnerability; }  set { vulnerability = value; } }

    [SerializeField]protected float jumpForce = 60f;//Сила, с которой ИИ совершает прыжок
    protected bool jumping = false;
    protected bool avoid = false;//Обходим ли препятствие в данный момент?
    protected float optSpeed;//Скорость оптимизированной версии персонажа

    protected EVector3 prevTargetPosition = new EVector3(Vector3.zero);//Предыдущее местоположение цели
    protected EVector3 prevPosition = EVector3.zero;//Собственное предыдущее местоположение

    protected float navCellSize, minCellSqrMagnitude;
    protected virtual float NavCellSize { get { return navCellSize; } set { navCellSize = value; minCellSqrMagnitude = navCellSize * navCellSize / 4f; } }

    protected ETarget beginPosition;//С какой точки персонаж начинает производить свою деятельность
    protected OrientationEnum beginOrientation;//С какой ориентацией персонаж начинает игру

    protected bool optimized = false;//Находится ли персонаж в своей оптимизированной версии?
    protected bool Optimized { get { return optimized; } set { optimized = value; if (optimized) analyseActions = AnalyseOpt; else analyseActions = Analyse; } }
    protected bool followOptPath = false;//Следует ли персонаж в оптимизированной версии маршруту?

    protected override bool Underwater{get{return base.Underwater;}set{base.Underwater = value;Animate(new AnimationEventArgs("waterSplash"));}}

    #endregion //parametres

    protected virtual void FixedUpdate()
    {
        if (!immobile)
            behaviorActions.Invoke();
    }

    protected virtual void Update()
    {
        analyseActions.Invoke();
    }

    protected override void Initialize()
    {
        base.Initialize();
        analyseActions = Analyse;
        beginPosition = new ETarget(transform.position);
        beginOrientation = orientation;
        indicators = transform.FindChild("Indicators");
        Transform areaTransform = indicators.FindChild("AreaTrigger");
        if (areaTransform != null ? areaTransform.gameObject.activeSelf : false)
            areaTrigger = areaTransform.GetComponent<AreaTrigger>();
        if (areaTrigger != null)
        {
            areaTrigger.TriggerHolder = gameObject;
            areaTrigger.triggerFunctionIn += ChangeBehaviorToActive;
            areaTrigger.triggerFunctionOut += ChangeBehaviorToOptimized;
            areaTrigger.triggerFunctionIn += EnableColliders;
            areaTrigger.triggerFunctionOut += DisableColliders;
            if (hitBox != null)
            {
                areaTrigger.triggerFunctionIn += EnableHitBox;
                areaTrigger.triggerFunctionOut += DisableHitBox;
            }
            if (rigid != null)
            {
                areaTrigger.triggerFunctionIn += EnableRigidbody;
                areaTrigger.triggerFunctionOut += DisableRigidbody;
            }
            if (indicators != null)
            {
                areaTrigger.triggerFunctionIn += EnableIndicators;
                areaTrigger.triggerFunctionOut += DisableIndicators;
            }
            if (anim != null)
            {
                areaTrigger.triggerFunctionIn += EnableVisual;
                areaTrigger.triggerFunctionOut += DisableVisual;
            }
        }
        GetMap();
        BecomeCalm();
        optSpeed = speed * optTimeStep;
        usualBalance = balance;
    }

    protected virtual void Start()
    {
        Loyalty = loyalty;
    }

    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        storyActionBase.Add("changeLoyalty", StoryLoyaltyChange);
        storyActionBase.Add("goToThePoint", StoryGoToThePoint);
    }

    /// <summary>
    /// Двинуться прочь от текущей цели (используется неходящими по земле персонажами)
    /// </summary>
    protected virtual void MoveAway(OrientationEnum _orientation)
    { }

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
        Animate(new AnimationEventArgs("attack", "", Mathf.RoundToInt(100f * attackParametres.wholeAttackTime)));
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.preAttackTime);
        hitBox.SetHitBox(new HitParametres(attackParametres));
        hitBox.AttackDirection = Vector2.right * (int)orientation;
        yield return new WaitForSeconds(attackParametres.actTime + attackParametres.endAttackTime);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Прекратить атаку
    /// </summary>
    protected override void StopAttack()
    {
        base.StopAttack();
        Animate(new AnimationEventArgs("stop"));
        if (hitBox!=null)
            hitBox.ResetHitBox();
    }

    /// <summary>
    /// Функция, ответственная за анализ окружающей обстановки
    /// </summary>
    protected override void Analyse()
    {
        Vector2 pos = transform.position;
        if (Physics2D.OverlapCircle(pos + Vector2.up * underwaterCheckOffset, minDistance / 2f, LayerMask.GetMask(wLName)))
        {
            if (!underWater)
                Underwater = true;
        }
        else if (!Physics2D.OverlapCircle(pos + Vector2.up * underwaterCheckOffset, minDistance*4f, LayerMask.GetMask(wLName)))
        {
            if (underWater)
                Underwater = false;
        }
    }

    /// <summary>
    /// Процесс обхода препятствия
    /// </summary>
    protected virtual IEnumerator AvoidProcess()
    {
        avoid = true;
        EVector3 _prevPos = prevPosition;
        yield return new WaitForSeconds(avoidTime);
        Vector3 pos = transform.position;
        if (currentTarget.exists && (currentTarget != mainTarget) && (pos - _prevPos).sqrMagnitude < speed*speedCoof * Time.fixedDeltaTime / 10f)
        {
            transform.position += (currentTarget - transform.position).normalized * navCellSize;
        }
        avoid = false;
    }

    protected virtual void StopAvoid()
    {
        StopCoroutine("AvoidProcess");
        avoid = false;
    }

    /// <summary>
    /// Специальный процесс, учитывающий то, что персонаж долгое время не может достичь какой-то позиции. Тогда, считаем, что опорной позицией персонажа станет текущая позиция (если уж он не может вернуться домой)
    /// </summary>
    /// <param name="prevPosition">Предыдущая позиция персонажа. Если спустя какое-то время ИИ никак не сдвинулся относительно неё, значит нужно привести в действие данный процесс</param>
    /// <returns></returns>
    protected virtual IEnumerator ResetStartPositionProcess(Vector2 prevPosition)
    {
        yield return new WaitForSeconds(avoidTime);
        if (Vector2.SqrMagnitude(prevPosition - (Vector2)transform.position) < minCellSqrMagnitude && behavior == BehaviorEnum.patrol)
        {
            beginPosition = new ETarget(transform.position);
            beginOrientation = orientation;
            BecomeCalm();
        }
    }

    /// <summary>
    /// Обновить информацию, важную для моделей поведения
    /// </summary>
    protected virtual void RefreshTargets()
    {
        employment = maxEmployment;
        currentTarget.Exists = false;
        StopFollowOptPath();
        Waiting = false;
        if (!immobile)
            StopAttack();
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected virtual void BecomeAgressive()
    {
        if (!mainTarget.Exists)
            return;
        RefreshTargets();
        behavior = BehaviorEnum.agressive;
        waypoints = null;
        currentTarget = mainTarget;
        if (optimized)
            behaviorActions = AgressiveOptBehavior;
        else
            behaviorActions = AgressiveBehavior;
        StopCoroutine("BecomeCalmProcess");
        OnChangeBehavior(new BehaviorEventArgs(BehaviorEnum.agressive));
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected virtual void BecomeCalm()
    {
        RefreshTargets();
        behavior = BehaviorEnum.calm;
        mainTarget.Exists = false;
        TargetCharacter = null;
        waypoints = null;
        if (optimized)
            behaviorActions = CalmOptBehavior;
        else
            behaviorActions = CalmBehavior;
        OnChangeBehavior(new BehaviorEventArgs(BehaviorEnum.calm));
    }

    /// <summary>
    /// Перейти к патрулированию
    /// </summary>
    protected virtual void BecomePatrolling()
    {
        RefreshTargets();
        behavior = BehaviorEnum.patrol;
        //mainTarget.Exists = false;
        TargetCharacter = null;
        if (optimized)
            behaviorActions = PatrolOptBehavior;
        else
            behaviorActions = PatrolBehavior;
        OnChangeBehavior(new BehaviorEventArgs(BehaviorEnum.patrol));
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
        else if (behavior != BehaviorEnum.calm)
            BecomeCalm();
    }

    /// <summary>
    /// Функция, заставляющая ИИ выдвигаться к заданной точке
    /// </summary>
    /// <param name="targetPosition">Точка, достичь которую стремится ИИ</param>
    protected virtual void GoToThePoint(Vector2 targetPosition)
    {
        if (navMap == null)
            return;
        waypoints = navMap.GetPath(transform.position, targetPosition, true);
        if (waypoints == null)
            return;
        BecomePatrolling();
    }

    /// <summary>
    /// Выдвинуться в изначальную позицию
    /// </summary>
    public virtual void GoHome()
    {
        //StopMoving();
        MainTarget = ETarget.zero;
        GoToThePoint(beginPosition);
        if (behavior == BehaviorEnum.agressive)
        {
            BecomePatrolling();
        }
    }

    /// <summary>
    /// Среагировать на услышанный боевой клич
    /// </summary>
    /// <param name="cryPosition">Место, откуда издался клич</param>
    public virtual void HearBattleCry(Vector2 cryPosition)
    {
        if (behavior!=BehaviorEnum.agressive && loyalty==LoyaltyEnum.enemy)
        {
            GoToThePoint(cryPosition);
        }
    }

    /// <summary>
    /// Сбросить все точки пути следования
    /// </summary>
    protected virtual void ResetWaypoints()
    {
        waypoints = new List<NavigationCell>();
    }

    /// <summary>
    /// Возвращает навигационный маршрут персонажа
    /// </summary>
    public List<NavigationCell> GetWaypoints()
    {
        return waypoints;
    }

    /// <summary>
    /// Функция, вызываемая при получении урона, оповещающая о субъекте нападения
    /// </summary>
    /// <param name="attackerInfo">Кто атаковал персонажа</param>
    public override void TakeAttackerInformation(AttackerClass attackerInfo)
    {
        if (attackerInfo != null)
        {
            if (mainTarget.transform != attackerInfo.attacker.transform)
                MainTarget = new ETarget(attackerInfo.attacker.transform);
            if (behavior!=BehaviorEnum.agressive)
                BecomeAgressive();
        }
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage( HitParametres hitData)
    {
        if (hitData.damageType != DamageType.Physical)
        {
            if (((DamageType)vulnerability & hitData.damageType) == hitData.damageType)
            {
                hitData.damage *= 1.25f;
                if (health - hitData.damage <= 0f && attackParametres.damageType != DamageType.Physical)
                    SpecialFunctions.gameController.AddVulnerableKilledEnemy(monsterName);
            }
            else if (hitData.damageType == attackParametres.damageType)
                hitData.damage *= .9f;//Если урон совпадает с типом атаки персонажа, то он ослабевается (бить огонь огнём - не самая гениальная затея)
        }
        base.TakeDamage(hitData);
        OnHealthChanged(new HealthEventArgs(health));
        bool stunned = GetBuff("StunnedProcess") != null;
        bool frozen = GetBuff("FrozenProcess") != null;
        if (hitData.attackPower >balance || frozen || stunned)
        {
            StopMoving();
            balance = usualBalance;
            if (!frozen && !stunned)
            {
                StopCoroutine("Microstun");
                StartCoroutine("Microstun");
            }
            StopAttack();
            employment = maxEmployment;
        }
    }

    /// <summary>
    /// Функция получения урона (в данной функции параметр ignoreInvul показывает, вводится ли персонаж в микростан при ударе или нет).
    /// </summary>
    /// <param name="damage">величина урона</param>
    /// <param name="hitData.damageType">тип урона</param>
    /// <param name="ignoreInvul">показывает, вводится ли персонаж в микростан при ударе или нет</param>
    public override void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
        if (hitData.damageType != DamageType.Physical)
        {
            if (((DamageType)vulnerability & hitData.damageType) == hitData.damageType)
            {
                hitData.damage *= 1.25f;
                if (health - hitData.damage <= 0f && attackParametres.damageType != DamageType.Physical)
                    SpecialFunctions.gameController.AddVulnerableKilledEnemy(monsterName);
            }
            else if (hitData.damageType == attackParametres.damageType)
                hitData.damage *= .9f;//Если урон совпадает с типом атаки персонажа, то он ослабевается (бить огонь огнём - не самая гениальная затея)
        }
        base.TakeDamage(hitData, ignoreInvul);
        OnHealthChanged(new HealthEventArgs(health));
        bool stunned = GetBuff("StunnedProcess") != null;
        bool frozen = GetBuff("FrozenProcess") != null;
        if (hitData.attackPower >balance || stunned || frozen)
        {
            StopMoving();
            if (!stunned && !frozen)
            {
                StopCoroutine("Microstun");
                StartCoroutine("Microstun");
            }
            StopAttack();
            employment = maxEmployment;
        }
    }

    /// <summary>
    /// Функция смерти
    /// </summary>
    protected override void Death()
    {
        if (!dead)
        {
            base.Death();
            if (!dead)
                return;

            if (UnityEngine.Random.Range(0f,1f)<dropProbability)
                foreach (GameObject drop1 in drop)
                {
                    GameObject _drop = Instantiate(drop1, transform.position, Quaternion.identity) as GameObject;
                }

            SpecialFunctions.statistics.ConsiderStatistics(this);
            string _id = "";
            if (GetBuff("FrozenProcess") != null)
                _id = "ice";
            else if (GetBuff("StunnedProcess") != null && GetBuff("BurningProcess") != null)
                _id = "fire";
            Animate(new AnimationEventArgs("death", _id, 0));
            if (targetCharacter != null)
                targetCharacter.CharacterDeathEvent -= HandleTargetDeathEvent;
            Destroy(gameObject);
        }
    }

    protected virtual IEnumerator Microstun()
    {
        immobile = true;
        yield return new WaitForSeconds(microStun);
        if (GetBuff("StunnedProcess")==null)
            immobile = false;
    }

    /// <summary>
    /// Процесс, обеспечивающий частоту учёта смены положения союзника, за которым следует персонаж
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator ConsiderAllyPathProcess()
    {
        followAlly = false;
        yield return new WaitForSeconds(allyTime);
        followAlly = true;
    }

    #region behaviorActions

    /// <summary>
    /// Пустая функция, которая используется, чтобы показать, что персонаж ничего не совершает
    /// </summary>
    protected void NullBehavior()
    {
    }

    /// <summary>
    /// Функция, реализующая спокойное состояние ИИ
    /// </summary>
    protected virtual void CalmBehavior()
    {
    }

    //Функция, реализующая агрессивное состояние ИИ
    protected virtual void AgressiveBehavior()
    {
    }

    /// <summary>
    /// Функция, реализующая состояние ИИ, при котором то перемещается между текущими точками следования
    /// </summary>
    protected virtual void PatrolBehavior()
    {
    }

    /// <summary>
    /// Функция, которая строит маршрут
    /// </summary>
    /// <param name="endPoint">точка назначения</param>
    ///<param name="maxDepth">Максимальная сложность маршрута</param>
    /// <returns>Навигационные ячейки, составляющие маршрут</returns>
    protected virtual List<NavigationCell> FindPath(Vector2 endPoint, int _maxDepth)
    {
        if (navMap == null || !(navMap is NavigationBunchedMap))
            return null;

        NavigationBunchedMap _map = (NavigationBunchedMap)navMap;
        List<NavigationCell> _path = new List<NavigationCell>();
        ComplexNavigationCell beginCell = (ComplexNavigationCell)_map.GetCurrentCell(transform.position), endCell = (ComplexNavigationCell)_map.GetCurrentCell(endPoint);

        if (beginCell == null || endCell == null)
            return null;
        prevTargetPosition = new EVector3(endPoint, true);

        int depthOrder = 0, currentDepthCount = 1, nextDepthCount = 0;
        //navMap.ClearMap();
        List<NavigationGroup> clearedGroups = new List<NavigationGroup>();//Список "очищенных групп" (со стёртой информации о посещённости ячеек)
        List<NavigationGroup> cellGroups = _map.cellGroups;
        NavigationGroup clearedGroup = cellGroups[beginCell.groupNumb];
        clearedGroup.ClearCells();
        clearedGroups.Add(clearedGroup);
        clearedGroup = cellGroups[endCell.groupNumb];
        if (!clearedGroups.Contains(clearedGroup))
        {
            clearedGroup.ClearCells();
            clearedGroups.Add(clearedGroup);
        }

        Queue<ComplexNavigationCell> cellsQueue = new Queue<ComplexNavigationCell>();
        cellsQueue.Enqueue(beginCell);
        beginCell.visited = true;
        while (cellsQueue.Count > 0 && endCell.fromCell == null)
        {
            ComplexNavigationCell currentCell = cellsQueue.Dequeue();
            if (currentCell == null)
                return null;
            List<ComplexNavigationCell> neighbourCells = currentCell.neighbors.ConvertAll<ComplexNavigationCell>(x => _map.GetCell(x.groupNumb, x.cellNumb));
            foreach (ComplexNavigationCell cell in neighbourCells)
            {
                if (cell.groupNumb != currentCell.groupNumb)
                {
                    clearedGroup = cellGroups[cell.groupNumb];
                    if (!clearedGroups.Contains(clearedGroup))
                    {
                        clearedGroup.ClearCells();
                        clearedGroups.Add(clearedGroup);
                    }
                }

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
            ComplexNavigationCell checkPoint1 = (ComplexNavigationCell)_path[i], checkPoint2 = (ComplexNavigationCell)_path[i + 1];
            NeighborCellStruct neighbConnection = checkPoint1.GetNeighbor(checkPoint2.groupNumb, checkPoint2.cellNumb),prevNeighbConnection;
            if (neighbConnection.connectionType == NavCellTypeEnum.jump || checkPoint1.cellType == NavCellTypeEnum.movPlatform)
                continue;
            if (checkPoint1.cellType != checkPoint2.cellType)
                continue;
            Vector2 movDirection1 = (checkPoint2.cellPosition - checkPoint1.cellPosition).normalized;
            Vector2 movDirection2 = Vector2.zero;
            int index = i + 2;
            ComplexNavigationCell checkPoint3 = (ComplexNavigationCell)_path[index];
            prevNeighbConnection = neighbConnection;
            neighbConnection = checkPoint2.GetNeighbor(checkPoint3.groupNumb, checkPoint3.cellNumb);
            while (Vector2.SqrMagnitude(movDirection1 - (checkPoint3.cellPosition - checkPoint2.cellPosition).normalized) < .01f &&
                   (checkPoint1.cellType == checkPoint3.cellType? true : prevNeighbConnection.connectionType==neighbConnection.connectionType) &&
                   index < _path.Count)
            {
                index++;
                if (index < _path.Count)
                {
                    checkPoint2 = checkPoint3;
                    checkPoint3 = (ComplexNavigationCell)_path[index];
                    prevNeighbConnection = neighbConnection;
                    neighbConnection = checkPoint2.GetNeighbor(checkPoint3.groupNumb, checkPoint3.cellNumb);
                }
            }
            if (i == 0 && index > 1 && (OnLadder || checkPoint1.cellType==NavCellTypeEnum.ladder))//Уберём самую первую точку, если она не имеет большого значения в маршруте
            {
                _path.RemoveAt(0);
                index--;
                if (i == 0 && index > 1)//Уберём и вторую точку тогда
                {
                    _path.RemoveAt(0);
                    index--;
                    if (i == 0 && index > 1)//А что... третья тоже мешает
                    {
                        _path.RemoveAt(0);
                        index--;
                    }
                }
            }
            for (int j = i + 1; j < index - 1; j++)
                _path.RemoveAt(i + 1);
        }

        /*
        //Рассмотрим первые 2 точки. Чтобы персонаж не возвращался назад к первой точке, когда вторая точка в другой стороне движения - удалим первую точку (если она не имеет значения)
        if (_path.Count >= 2)
        {
            ComplexNavigationCell checkPoint1 = (ComplexNavigationCell)_path[0], checkPoint2 = (ComplexNavigationCell)_path[1];
            NeighborCellStruct neighbInfo = checkPoint1.GetNeighbor(checkPoint2.groupNumb, checkPoint2.cellNumb);
            if (neighbInfo.groupNumb == -1)
            {
                if (OnLadder || checkPoint1.cellType != NavCellTypeEnum.ladder)
                    _path.RemoveAt(0);
            }
            else
            {
                if (neighbInfo.connectionType == NavCellTypeEnum.usual || (neighbInfo.connectionType == NavCellTypeEnum.ladder && !OnLadder))
                    _path.RemoveAt(0);
            }
        }*/

        #endregion //optimize

        return _path;
    }

    /// <summary>
    /// Сменить главную цель
    /// </summary>
    protected virtual void ChangeMainTarget()
    {
        GameObject prevObj = mainTarget.transform != null ? mainTarget.transform.gameObject : null;
        MainTarget = ETarget.zero;
        Transform obj = SpecialFunctions.battleField.GetNearestCharacter(transform.position, loyalty == LoyaltyEnum.ally);
        if (obj != null && obj!=prevObj)
        {
            MainTarget = new ETarget(obj);
            GoToThePoint(mainTarget);
        }
        else
        {
            GoHome();
        }
    }

    #endregion //behaviorActions

    /// <summary>
    /// Возвращает тот тип навигацинной карты, которой пользуется данный бот
    /// </summary>
    public virtual NavMapTypeEnum GetMapType()
    {
        return NavMapTypeEnum.usual;
    }

    /// <summary>
    /// Определить, какая карта соответствует персонажу
    /// </summary>
    protected virtual void GetMap()
    {
        GameStatistics statistics = SpecialFunctions.statistics;
        if (statistics == null? true: statistics.navSystem== null)
            navMap = null;
        else
        {
            navMap = statistics.navSystem.GetMap(GetMapType());
            NavCellSize = statistics.navSystem.cellSize.magnitude;
        }
    }

    #region optimization

    /// <summary>
    /// Сменить модель поведения в связи с выходом из триггера
    /// </summary>
    protected virtual void AreaTriggerExitChangeBehavior()
    {
        if (behavior == BehaviorEnum.agressive)
        {
            GoToThePoint(mainTarget);
            if (behavior == BehaviorEnum.agressive)
                GoHome();
            else
                StartCoroutine("BecomeCalmProcess");
        }
    }

    /// <summary>
    /// Смерть персонажа в связи с его выходом из триггера поля боя
    /// </summary>
    protected virtual void AreaTriggerExitDeath()
    {
        Death();
    }

    /// <summary>
    /// Включить хитбокс
    /// </summary>
    protected virtual void EnableHitBox()
    {
        hitBox.gameObject.SetActive(true);
    }

    /// <summary>
    /// Выключить хитбокс
    /// </summary>
    protected virtual void DisableHitBox()
    {
        hitBox.gameObject.SetActive(false);
    }

    /// <summary>
    /// Включить собственный хитбокс
    /// </summary>
    protected virtual void EnableSelfHitBox()
    {
    }

    /// <summary>
    /// Выключить собственный хитбокс
    /// </summary>
    protected virtual void DisableSelfHitBox()
    {
    }

    /// <summary>
    /// Включить риджидбоди
    /// </summary>
    protected virtual void EnableRigidbody()
    {
        rigid.isKinematic = false;
    }

    /// <summary>
    /// Выключить риджидбоди
    /// </summary>
    protected virtual void DisableRigidbody()
    {
        rigid.velocity = Vector2.zero;
        rigid.isKinematic = true;
    }

    /// <summary>
    /// Включить все коллайдеры в персонаже
    /// </summary>
    protected virtual void EnableColliders()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (Collider2D col in cols)
            col.enabled = true;
    }

    /// <summary>
    /// Выключить все коллайдеры в персонаже
    /// </summary>
    protected virtual void DisableColliders()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (Collider2D col in cols)
            col.enabled = false;
    }

    /// <summary>
    /// Включить визуальное отображение персонажа
    /// </summary>
    protected virtual void EnableVisual()
    {
        anim.gameObject.SetActive(true);
    }

    /// <summary>
    /// Выключить визуальное отображение персонажа
    /// </summary>
    protected virtual void DisableVisual()
    {
        anim.gameObject.SetActive(false);
    }

    /// <summary>
    /// Включить индикаторы
    /// </summary>
    protected virtual void EnableIndicators()
    {
        for (int i = 0; i < indicators.childCount; i++)
        {
            GameObject indicator = indicators.GetChild(i).gameObject;
            WallChecker wChecker = null;
            if ((wChecker = indicator.GetComponent<WallChecker>()) != null)
                wChecker.WallInFront=false;
            indicators.GetChild(i).gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Выключить индикаторы
    /// </summary>
    protected virtual void DisableIndicators()
    {
        for (int i = 0; i < indicators.childCount; i++)
        {
            GameObject indicator = indicators.GetChild(i).gameObject;
            if (indicator.GetComponent<AreaTrigger>() == null)
                indicator.SetActive(false);
        }
    }

    /// <summary>
    /// Функция - пустышка
    /// </summary>
    protected virtual void NullAreaFunction()
    { }

    /// <summary>
    /// Функция реализующая анализ окружающей персонажа обстановки, когда тот находится в оптимизированном состоянии
    /// </summary>
    protected virtual void AnalyseOpt()
    {
        if (!followOptPath)
            StartCoroutine("PathPassOptProcess");
    }

    /// <summary>
    /// Функция, реализующая спокойное состояние ИИ (оптимизированная версия)
    /// </summary>
    protected virtual void CalmOptBehavior()
    {
    }

    //Функция, реализующая агрессивное состояние ИИ (оптимизированная версия)
    protected virtual void AgressiveOptBehavior()
    {
    }

    /// <summary>
    /// Функция, реализующая состояние ИИ, при котором то перемещается между текущими точками следования (оптимизированная версия)
    /// </summary>
    protected virtual void PatrolOptBehavior()
    {
    }

    /// <summary>
    /// Сменить оптимизированную версию на активную
    /// </summary>
    protected virtual void ChangeBehaviorToActive()
    {
        Optimized = false;
        StopFollowOptPath();
        switch (behavior)
        {
            case BehaviorEnum.calm:
                {
                    behaviorActions = CalmBehavior;
                    break;
                }
            case BehaviorEnum.agressive:
                {
                    behaviorActions = AgressiveBehavior;
                    break;
                }
            case BehaviorEnum.patrol:
                {
                    behaviorActions = PatrolBehavior;
                    break;
                }
            default:
                break;
        }
        RestoreActivePosition();
    }

    /// <summary>
    /// Сменить оптимизированную версию на активную
    /// </summary>
    protected virtual void ChangeBehaviorToOptimized()
    {
        GetOptimizedPosition();
        Optimized = true;
        switch (behavior)
        {
            case BehaviorEnum.calm:
                {
                    behaviorActions = CalmOptBehavior;
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
    /// Функция, которая восстанавливает положение и состояние персонажа, пользуясь данными, полученными в оптимизированном режиме
    /// </summary>
    protected virtual void RestoreActivePosition()
    {
    }

    /// <summary>
    /// Функция, которая переносит персонажа в ту позицию, в которой он может нормально функционировать для ведения оптимизированной версии 
    /// </summary>
    protected virtual void GetOptimizedPosition()
    {
    }

    /// <summary>
    /// Процесс оптимизированного прохождения пути. Заключается в том, что персонаж, зная свой маршрут, появляется в его различиных позициях, не используя 
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator PathPassOptProcess()
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
                    //Если не получается добраться до начальной позиции (И если эта начальная позиция - не главный герой, за которым следует союзник), то считаем, что текущая позиция становится начальной
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
                        ComplexNavigationCell currentCell = (ComplexNavigationCell)waypoints[0];
                        waypoints.RemoveAt(0);
                        if (waypoints.Count <= 0)
                            break;
                        ComplexNavigationCell nextCell = (ComplexNavigationCell)waypoints[0];
                        currentTarget = new ETarget(nextCell.cellPosition);
                        if (currentCell.GetNeighbor(nextCell.groupNumb, nextCell.cellNumb).groupNumb != -1)
                        {
                            yield return new WaitForSeconds(optTimeStep);
                            transform.position = nextCell.cellPosition;
                            continue;
                        }
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

    /// <summary>
    /// Прекратить следование маршруту в оптимизированном режиме
    /// </summary>
    protected virtual void StopFollowOptPath()
    {
        followOptPath = false;
        StopCoroutine("PathPassOptProcess");
    }

    #endregion //optimization

    #region events

    /// <summary>
    /// Вызвать событие, связанное со сменой модели поведения
    /// </summary>
    /// <param name="e">Данные события</param>
    protected virtual void OnChangeBehavior(BehaviorEventArgs e)
    {
        EventHandler<BehaviorEventArgs> handler = BehaviorChangeEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    /// <summary>
    /// Вызвать событие, связанное со сменой стороны конфликта
    /// </summary>
    /// <param name="e">Данные события</param>
    protected virtual void OnChangeLoyalty(LoyaltyEventArgs e)
    {
        EventHandler<LoyaltyEventArgs> handler = LoyaltyChangeEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    /// <summary>
    /// Событие "уровень здоровья изменился"
    /// </summary>
    protected virtual void OnHealthChanged(HealthEventArgs e)
    {
        EventHandler<HealthEventArgs> handler = healthChangedEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

    #region eventHandlers

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected virtual void HandleAttackProcess(object sender, HitEventArgs e)
    {
        //Если игрок случайно наткнулся на монстра и получил урон, то персонаж автоматически становится агрессивным
        if (behavior != BehaviorEnum.agressive)
        {
            if (e.Target!=null)
                MainTarget = new ETarget(e.Target.transform);
            BecomeAgressive();
        }
    }

    /// <summary>
    /// Обработка события "Услышал врага"
    /// </summary>
    protected virtual void HandleHearingEvent(object sender, HearingEventArgs e)
    {
        if (behavior != BehaviorEnum.agressive)
        {
            MainTarget = new ETarget(e.Target.transform);
            BecomeAgressive();
        }
    }

    /// <summary>
    /// Узнать о смерти текущей цели и переключится на следующую
    /// </summary>
    /// <param name="sender">Что вызвало событие</param>
    /// <param name="e">Данные о событии</param>
    protected virtual void HandleTargetDeathEvent(object sender, StoryEventArgs e)
    {
        if (dead)
            return;
        ChangeMainTarget();
    }

    #endregion //eventHandlers

    #region id

    /// <summary>
    /// Получить данные о враге с целью сохранить их
    /// </summary>
    public virtual EnemyData GetAIData()
    {
        EnemyData eData = new EnemyData(this);
        return eData;
    }

    /// <summary>
    /// Настроить персонажа в соответствии с загруженными данными
    /// </summary>
    public virtual void SetAIData(EnemyData eData)
    {
        if (eData != null)
        {
            transform.position = eData.position;
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
                        break;
                    }
                case "patrol":
                    {
                        BecomePatrolling();
                        if (eData.waypoints.Count > 0)
                        {
                            NavigationSystem navSystem = SpecialFunctions.statistics.navSystem;
                            if (navSystem != null)
                            {
                                waypoints = new List<NavigationCell>();
                                for (int i = 0; i < eData.waypoints.Count; i++)
                                    waypoints.Add(navMap.GetCurrentCell(eData.waypoints[i]));
                                transform.position = waypoints[0].cellPosition;
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

            TargetData currentTargetData = eData.currentTargetData;
            TargetData mainTargetData = eData.mainTargetData;

            if (currentTargetData.targetName != string.Empty)
                currentTarget = new ETarget(GameObject.Find(currentTargetData.targetName).transform);
            else
                currentTarget = new ETarget(currentTargetData.position);

            if (mainTargetData.targetName != string.Empty)
                MainTarget = new ETarget(GameObject.Find(mainTargetData.targetName).transform);
            else
                MainTarget = new ETarget(mainTargetData.position);

            SetBuffs(eData.bListData);

            Health = eData.health;
        }
    }

    #endregion //id

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public override List<string> actionNames()
    {
        List<string> _actionNames = base.actionNames();
        _actionNames.Add("changeLoyalty");
        _actionNames.Add("goToThePoint");
        return _actionNames;
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs1()
    {
        Dictionary<string, List<string>> _actionIDs1 = base.actionIDs1();
        _actionIDs1.Add("changeLoyalty", new List<string>() {"enemy", "ally", "neutral"});
        _actionIDs1.Add("goToThePoint", new List<string>() { "hero" });
        return _actionIDs1;
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs2()
    {
        Dictionary<string, List<string>> _actionIDs2 = base.actionIDs2();
        _actionIDs2.Add("changeLoyalty", new List<string>() { });
        _actionIDs2.Add("goToThePoint", new List<string>() { });
        return _actionIDs2;
    }

    #endregion //IHaveStory

    #region storyActions

    /// <summary>
    /// Сменить отношение к игроку в результате исторического действия
    /// </summary>
    public void StoryLoyaltyChange(StoryAction _action)
    {
        Loyalty = _action.id1 == "enemy" ? LoyaltyEnum.enemy : (_action.id1 == "ally" ? LoyaltyEnum.ally : LoyaltyEnum.neutral);
    }

    /// <summary>
    /// Выдвинуться к позиции в результате исторического действия
    /// </summary>
    public void StoryGoToThePoint(StoryAction _action)
    {
        if (_action.id1 == "hero")
            GoToThePoint(SpecialFunctions.player.transform.position);
        else
        {
            GameObject nextTargetObject = GameObject.Find(_action.id1);
            if (nextTargetObject!=null)
                GoToThePoint(nextTargetObject.transform.position);
        }
    }

    #endregion //storyActions

    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (currentTarget.exists && UnityEditor.Selection.activeObject==gameObject)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawIcon(currentTarget, "target");
        }
#endif //UNITY_EDITOR
    }

}

/// <summary>
/// Редактор персонажей с ИИ
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(AIController),true)]
public class AIControllerEditor : Editor
{

    #region fields

    protected HitParametres hitData;

    #endregion //fields

    #region parametres

    protected DamageType prevDamageType;

    #endregion //parametres

    public virtual void OnEnable()
    {
        AIController ai = (AIController)target;
        hitData = ai.AttackParametres;
        prevDamageType = hitData.damageType;
    }

    public override void OnInspectorGUI()
    {
        AIController ai = (AIController)target;
        hitData = ai.AttackParametres;
        base.OnInspectorGUI();
        if (hitData.damageType != prevDamageType)
        {
            prevDamageType = hitData.damageType;
            switch (prevDamageType)
            {
                case DamageType.Physical:
                    {
                        ai.Vulnerability = (byte)(DamageType.Cold | DamageType.Crushing | DamageType.Fire | DamageType.Poison | DamageType.Water);
                        hitData.effectChance=0f;
                        break;
                    }
                case DamageType.Crushing:
                    {
                        ai.Vulnerability = 0;
                        hitData.effectChance = 30f;
                        break;
                    }
                case DamageType.Fire:
                    {
                        ai.Vulnerability = (byte)DamageType.Water;
                        hitData.effectChance = 20f;
                        break;
                    }
                case DamageType.Cold:
                    {
                        ai.Vulnerability = (byte)DamageType.Fire;
                        hitData.effectChance = 20f;
                        break;
                    }
                case DamageType.Water:
                    {
                        ai.Vulnerability = (byte)DamageType.Fire;
                        hitData.effectChance = 50f;
                        break;
                    }
                case DamageType.Poison:
                    {
                        ai.Vulnerability = (byte)DamageType.Crushing;
                        hitData.effectChance = 15f;
                        break;
                    }
                default:
                    break;
            }
            ai.AttackParametres = hitData;
        }
        ai.Vulnerability = (byte)(DamageType)EditorGUILayout.EnumMaskPopup(new GUIContent("vulnerability"),(DamageType)ai.Vulnerability);
        EditorUtility.SetDirty(ai);
    }

}
#endif //UNITY_EDITOR