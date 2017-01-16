using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Базовый класс для персонажей, управляемых ИИ
/// </summary>
public class AIController : CharacterController
{

    #region consts

    protected const float sightOffset = 0.1f;
    protected const float microStun = .1f;
    protected const float minDistance = .04f;
    protected const float minAngle = 1f;
    protected const float minSpeed = .1f;

    protected const string gLName = "ground";//Название слоя земли
    protected const string cLName = "hero";//Название слоя персонажа

    protected const float optTimeStep = .75f;//Как часто работает оптимизированная версия персонажа

    #endregion //consts

    #region delegates

    protected delegate void ActionDelegate();

    #endregion //delegates

    #region eventHandlers

    public EventHandler<BehaviorEventArgs> BehaviorChangeEvent;//Событие о смене модели поведения
    public EventHandler<HealthEventArgs> healthChangedEvent;

    #endregion //eventHandlers

    #region fields

    [SerializeField][HideInInspector]protected int id;//ID монстра, по которому его можно отличить
    public int ID
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

    protected ETarget mainTarget = ETarget.zero;//Что является целью ИИ
    protected ETarget currentTarget = ETarget.zero;//Что является текущей целью ИИ
    protected List<NavigationCell> waypoints;//Маршрут следования
    protected virtual List<NavigationCell> Waypoints { get { return waypoints; } set { StopFollowOptPath(); waypoints = value; } }

    protected ActionDelegate behaviorActions;
    protected ActionDelegate analyseActions;

    protected AreaTrigger areaTrigger;//Триггер области вхождения монстра. Если герой находится в ней, то у монстра включаются все функции

    protected NavigationMap navMap;

    #endregion //fields

    #region parametres

    protected virtual float attackTime { get { return .6f; } }
    protected virtual float preAttackTime { get { return .3f; } }
    protected virtual float attackDistance { get { return .2f; } }//На каком расстоянии должен стоять ИИ, чтобы решить атаковать
    protected virtual float sightRadius { get { return 1.9f; } }
    protected virtual float beCalmTime { get { return 10f; } }//Время через которое ИИ перестаёт преследовать игрока, если он ушёл из их поля зрения
    protected virtual float avoidTime { get { return 1f; } }//Время, спустя которое можно судить о необходимости обхода препятствия
    protected virtual int maxAgressivePathDepth { get { return 20; } }//Насколько сложен может быть путь ИИ, в агрессивном состоянии 
                                                                      //(этот путь используется в тех случаях, когда невозможно настичь героя прямым путём)

    protected BehaviorEnum behavior = BehaviorEnum.calm;
    public BehaviorEnum Behavior { get { return behavior; } set { behavior = value; } }

    protected bool waiting = false;//Находится ли персонаж в тактическом ожидании?
    public virtual bool Waiting { get { return waiting; } set { waiting = value; } }
    protected virtual float waitingNearDistance { get { return 1f; } }//Если цель ближе этого расстояния, то в режиме ожидания персонаж будет убегать от неё
    protected virtual float waitingFarDistance { get { return 1.6f; } }//Если цель дальше этого расстояния, то в режиме ожидания персонаж будет стремиться к ней

    [SerializeField]protected float acceleration = 1f;

    [SerializeField] protected float damage = 1f;
    [SerializeField] protected float hitForce = 0f;
    [SerializeField] protected Vector2 attackSize = new Vector2(.07f, .07f);
    [SerializeField] protected Vector2 attackPosition = new Vector2(0f, 0f);

    [SerializeField]
    protected float jumpForce = 60f;//Сила, с которой паук совершает прыжок
    protected bool jumping = false;
    protected bool avoid = false;//Обходим ли препятствие в данный момент?
    protected float optSpeed;//Скорость оптимизированной версии персонажа

    protected EVector3 prevTargetPosition = new EVector3(Vector3.zero);//Предыдущее местоположение цели
    protected EVector3 prevPosition = EVector3.zero;//Собственное предыдущее местоположение

    protected float navCellSize, minCellSqrMagnitude;
    protected virtual float NavCellSize { get { return navCellSize; } set { navCellSize = value; minCellSqrMagnitude = navCellSize * navCellSize / 4f; } }

    protected bool dead = false;

    protected Vector2 beginPosition;//С какой точки персонаж начинает игру
    protected OrientationEnum beginOrientation;//С какой ориентацией персонаж начинает игру

    protected bool optimized = false;//Находится ли персонаж в своей оптимизированной версии?
    protected bool Optimized { get { return optimized; } set { optimized = value; if (optimized) analyseActions = AnalyseOpt; else analyseActions = Analyse; } }
    protected bool followOptPath = false;//Следует ли персонаж в оптимизированной версии маршруту?

    #endregion //parametres

    protected virtual void FixedUpdate()
    {
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
        beginPosition = transform.position;
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
        optSpeed = speed * optTimeStep;
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
        if (currentTarget.exists && (currentTarget != mainTarget) && (pos - _prevPos).sqrMagnitude < speed * Time.fixedDeltaTime / 10f)
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
            beginPosition = transform.position;
            beginOrientation = orientation;
            BecomeCalm();
        }
    }

    /// <summary>
    /// Обновить информацию, важную для моделей поведения
    /// </summary>
    protected virtual void RefreshTargets()
    {
        currentTarget.Exists = false;
        StopFollowOptPath();
        Waiting = false;
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected virtual void BecomeAgressive()
    {
        RefreshTargets();
        behavior = BehaviorEnum.agressive;
        mainTarget = new ETarget(SpecialFunctions.Player.transform);
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
        mainTarget.Exists = false;
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
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
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
    protected virtual void GoHome()
    {
        GoToThePoint(beginPosition);
        if (behavior == BehaviorEnum.agressive)
        {
            BecomePatrolling();
        }
    }

    /// <summary>
    /// Сбросить все точки пути следования
    /// </summary>
    protected virtual void ResetWaypoints()
    {
        waypoints = new List<NavigationCell>();
    }

    public List<NavigationCell> GetWaypoints()
    {
        return waypoints;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        OnHealthChanged(new HealthEventArgs(health));
        StopMoving();
        StartCoroutine(Microstun());
        if (behavior != BehaviorEnum.agressive)
            BecomeAgressive();
    }

    /// <summary>
    /// Функция смерти
    /// </summary>
    protected override void Death()
    {
        if (!dead)
        {
            dead = true;
            base.Death();
            SpecialFunctions.statistics.ConsiderStatistics(this);
            Animate(new AnimationEventArgs("death"));
            Destroy(gameObject);
        }
    }

    protected virtual IEnumerator Microstun()
    {
        immobile = true;
        yield return new WaitForSeconds(microStun);
        immobile = false;
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
            if (checkPoint1.cellType == NavCellTypeEnum.jump || checkPoint1.cellType == NavCellTypeEnum.movPlatform)
                continue;
            if (checkPoint1.cellType != checkPoint2.cellType)
                continue;
            Vector2 movDirection1 = (checkPoint2.cellPosition - checkPoint1.cellPosition).normalized;
            Vector2 movDirection2 = Vector2.zero;
            int index = i + 2;
            ComplexNavigationCell checkPoint3 = (ComplexNavigationCell)_path[index];
            while (Vector2.SqrMagnitude(movDirection1 - (checkPoint3.cellPosition - checkPoint2.cellPosition).normalized) < .01f &&
                   checkPoint1.cellType == checkPoint3.cellType &&
                   index < _path.Count)
            {
                index++;
                if (index < _path.Count)
                {
                    checkPoint2 = checkPoint3;
                    checkPoint3 = (ComplexNavigationCell)_path[index];
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
    /// Включить визуальное отображение персонажа
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
                if (waypoints == null)
                {
                    //Если не получается добраться до начальной позиции, то считаем, что текущая позиция становится начальной
                    beginPosition = transform.position;
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
                transform.position = pos + direction.normalized * Mathf.Clamp(speed, 0f, direction.magnitude);
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
    /// Вызвать событие, связанное со сменой моели поведения
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
        //Если игрок случайно наткнулся на монстра и получил урон, то паук автоматически становится агрессивным
        if (behavior != BehaviorEnum.agressive)
            BecomeAgressive();
    }

    /// <summary>
    /// Обработка события "Услышал врага"
    /// </summary>
    protected virtual void HandleHearingEvent(object sender, EventArgs e)
    {
        if (behavior != BehaviorEnum.agressive)
            BecomeAgressive();
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
                        behavior = BehaviorEnum.calm;
                        break;
                    }
                case "agressive":
                    {
                        behavior = BehaviorEnum.agressive;
                        break;
                    }
                case "patrol":
                    {
                        behavior = BehaviorEnum.patrol;
                        if (eData.waypoints.Count > 0)
                        {
                            NavigationSystem navSystem = SpecialFunctions.statistics.navSystem;
                            if (navSystem != null)
                            {
                                NavigationMap navMap = navSystem.GetMap(GetMapType());
                                waypoints = new List<NavigationCell>();
                                for (int i = 0; i < eData.waypoints.Count; i++)
                                    waypoints.Add(navMap.GetCurrentCell(eData.waypoints[i]));
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
            
            Health = eData.health;
        }
    }

    #endregion //id

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
