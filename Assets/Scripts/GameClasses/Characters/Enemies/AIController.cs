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
    protected const string cLName = "character";//Название слоя земли

    #endregion //consts

    #region delegates

    protected delegate void ActionDelegate();

    #endregion //delegates

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

    protected GameObject mainTarget;//Что является целью ИИ
    protected GameObject currentTarget;//Что является текущей целью ИИ
    protected List<NavigationCell> waypoints;//Маршрут следования
    protected virtual List<NavigationCell> Waypoints { get { return waypoints; } set { waypoints = value;} }

    protected ActionDelegate behaviourActions;

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

    protected BehaviourEnum behaviour = BehaviourEnum.calm;
    public BehaviourEnum Behaviour { get { return behaviour; } set { behaviour = value; } }

    [SerializeField]protected float acceleration = 1f;

    [SerializeField] protected float damage = 1f;
    [SerializeField] protected float hitForce = 0f;
    [SerializeField] protected Vector2 attackSize = new Vector2(.07f, .07f);
    [SerializeField] protected Vector2 attackPosition = new Vector2(0f, 0f);

    [SerializeField]
    protected float jumpForce = 60f;//Сила, с которой паук совершает прыжок
    protected bool jumping = false;
    protected bool avoid = false;//Обходим ли препятствие в данный момент?

    protected Vector2 waypoint;//Пункт назначения, к которому стремится ИИ
    protected EVector3 prevTargetPosition = new EVector3(Vector3.zero);//Предыдущее местоположение цели
    protected EVector3 prevPosition = EVector3.zero;//Собственное предыдущее местоположение

    protected float navCellSize, minCellSqrMagnitude;
    protected virtual float NavCellSize { get { return navCellSize; } set { navCellSize = value; minCellSqrMagnitude = navCellSize * navCellSize / 4f; } }

    protected bool dead = false;

    protected bool canStayOnPlatform = true;//Если true, то персонаж может использовать движущуюся платформу
    public bool CanStayOnPlatform { get { return canStayOnPlatform; } }

    protected Vector2 beginPosition;//С какой точки персонаж начинает игру
    protected OrientationEnum beginOrientation;//С какой ориентацией персонаж начинает игру

    #endregion //parametres

    protected virtual void FixedUpdate()
    {
        behaviourActions.Invoke();
    }

    protected override void Initialize()
    {
        base.Initialize();
        BecomeCalm();
        beginPosition = transform.position;
        beginOrientation = orientation;
        Transform indicators = transform.FindChild("Indicators");
        Transform areaTransform = indicators.FindChild("AreaTrigger");
        if (areaTransform != null)
            areaTrigger = areaTransform.GetComponent<AreaTrigger>();
        if (areaTrigger != null)
        {
            if (hitBox != null)
            {
                areaTrigger.triggerFunctionIn += EnableHitBox;
                areaTrigger.triggerFunctionOut += DisableHitBox;
            }
        }
        GetMap();
    }

    /// <summary>
    /// Процесс обхода препятствия
    /// </summary>
    protected virtual IEnumerator AvoidProcess()
    {
        avoid = true;
        EVector3 _prevPos = prevPosition;
        yield return new WaitForSeconds(avoidTime);
        if (currentTarget != null && currentTarget != mainTarget && (transform.position - _prevPos).sqrMagnitude < speed * Time.fixedDeltaTime / 10f)
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
    /// Специальный процесс, учитывающий то, что персонаж долгое время не может достичь какой-то позиции. Тогда, считаем, что опорной позицией персонажа станет текущая позиция (если уж он не может вернуться домой)
    /// </summary>
    /// <param name="prevPosition">Предыдущая позиция персонажа. Если спустя какое-то время ИИ никак не сдвинулся относительно неё, значит нужно привести в действие данный процесс</param>
    /// <returns></returns>
    protected virtual IEnumerator ResetStartPositionProcess(Vector2 prevPosition)
    {
        yield return new WaitForSeconds(avoidTime);
        if (Vector2.SqrMagnitude(prevPosition - (Vector2)transform.position) < minCellSqrMagnitude && behaviour==BehaviourEnum.patrol)
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
        if (currentTarget != null && currentTarget != mainTarget)
            Destroy(currentTarget);
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected virtual void BecomeAgressive()
    {
        RefreshTargets();
        behaviour = BehaviourEnum.agressive;
        mainTarget = SpecialFunctions.player;
        waypoints = null;
        currentTarget = mainTarget;
        behaviourActions = AgressiveBehaviour;
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected virtual void BecomeCalm()
    {
        RefreshTargets();
        behaviour = BehaviourEnum.calm;
        mainTarget = null;
        currentTarget = null;
        waypoints = null;
        behaviourActions = CalmBehaviour;
    }

    /// <summary>
    /// Перейти к патрулированию
    /// </summary>
    protected virtual void BecomePatrolling()
    {
        RefreshTargets();
        behaviour = BehaviourEnum.patrol;
        mainTarget = null;
        currentTarget = null;
        behaviourActions = PatrolBehaviour;
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
        else if (behaviour != BehaviourEnum.calm)
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
    protected virtual void GoHome()
    {
        GoToThePoint(beginPosition);
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
        StopMoving();
        StartCoroutine(Microstun());
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

    #region behaviourActions

    /// <summary>
    /// Пустая функция, которая используется, чтобы показать, что персонаж ничего не совершает
    /// </summary>
    protected void NullBehaviour()
    {
    }

    /// <summary>
    /// Функция, реализующая спокойное состояние ИИ
    /// </summary>
    protected virtual void CalmBehaviour()
    {
    }

    //Функция, реализующая агрессивное состояние ИИ
    protected virtual void AgressiveBehaviour()
    {
    }

    /// <summary>
    /// Функция, реализующая состояние ИИ, при котором то перемещается между текущими точками следования
    /// </summary>
    protected virtual void PatrolBehaviour()
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
        if (navMap == null)
            return null;

        List<NavigationCell> _path = new List<NavigationCell>();
        NavigationCell beginCell = navMap.GetCurrentCell(transform.position), endCell = navMap.GetCurrentCell(endPoint);

        if (beginCell == null || endCell == null)
            return null;
        prevTargetPosition = new EVector3(endPoint, true);

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

    #endregion //behaviourActions

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
        if (statistics == null)
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
        if (behaviour == BehaviourEnum.agressive)
        {
            GoToThePoint(mainTarget.transform.position);
            if (behaviour == BehaviourEnum.agressive)
                GoHome();
            else
                StartCoroutine(BecomeCalmProcess());
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
    /// Включить слух
    /// </summary>
    protected virtual void EnableHearing()
    { }

    /// <summary>
    /// Выключить слух
    /// </summary>
    protected virtual void DisableHearing()
    { }

    /// <summary>
    /// Функция - пустышка
    /// </summary>
    protected virtual void NullAreaFunction()
    { }

    #endregion //optimization

    #region eventHandlers

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected virtual void HandleAttackProcess(object sender, HitEventArgs e)
    {
        //Если игрок случайно наткнулся на монстра и получил урон, то паук автоматически становится агрессивным
        if (behaviour != BehaviourEnum.agressive)
            BecomeAgressive();
    }

    /// <summary>
    /// Обработка события "Услышал врага"
    /// </summary>
    protected virtual void HandleHearingEvent(object sender, EventArgs e)
    {
        if (behaviour != BehaviourEnum.agressive)
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

            string behaviourName = eData.behaviour;
            switch (behaviourName)
            {
                case "calm":
                    {
                        behaviour = BehaviourEnum.calm;
                        break;
                    }
                case "agressive":
                    {
                        behaviour = BehaviourEnum.agressive;
                        break;
                    }
                case "patrol":
                    {
                        behaviour = BehaviourEnum.patrol;
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
                        behaviour = BehaviourEnum.calm;
                        BecomeCalm();
                        break;
                    }
            }
            
            Health = eData.health;
        }
    }

    #endregion //id

}
