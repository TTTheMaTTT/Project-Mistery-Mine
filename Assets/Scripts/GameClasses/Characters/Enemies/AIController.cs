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
    protected const float minDistance = .2f;
    protected const float minAngle = 1f;
    protected const float minSpeed = .1f;

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

    protected ActionDelegate behaviourActions;

    protected AreaTrigger areaTrigger;//Триггер области вхождения монстра. Если герой находится в ней, то у монстра включаются все функции

    #endregion //fields

    #region parametres

    [SerializeField]protected float sightRadius = 5f;

    protected BehaviourEnum behaviour = BehaviourEnum.calm;
    public BehaviourEnum Behaviour { get { return behaviour; } set { behaviour = value; } }

    [SerializeField]protected float acceleration = 1f;

    [SerializeField] protected float damage = 1f;
    [SerializeField] protected float hitForce = 0f;
    [SerializeField] protected Vector2 attackSize = new Vector2(.07f, .07f);
    [SerializeField] protected Vector2 attackPosition = new Vector2(0f, 0f);

    protected bool dead = false;

    protected bool canStayOnPlatform = true;//Если true, то персонаж может использовать движущуюся платформу
    public bool CanStayOnPlatform { get { return canStayOnPlatform; } }

    protected Vector2 beginPosition;//С какой точки персонаж начинает игру

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


    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected virtual void BecomeAgressive()
    {
        if (currentTarget != null && currentTarget != mainTarget)
            Destroy(currentTarget);
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
        if (currentTarget != null && currentTarget != mainTarget)
            Destroy(currentTarget);
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
        if (currentTarget != null && currentTarget != mainTarget)
            Destroy(currentTarget);
        behaviour = BehaviourEnum.patrol;
        mainTarget = null;
        currentTarget = null;
        behaviourActions = PatrolBehaviour;
    }

    /// <summary>
    /// Функция, заставляющая ИИ выдвигаться к заданной точке
    /// </summary>
    /// <param name="targetPosition">Точка, достичь которую стремится ИИ</param>
    protected virtual void GoToThePoint(Vector2 targetPosition)
    {
        NavigationSystem navSystem = SpecialFunctions.statistics.navSystem;
        if (navSystem == null)
            return;
        NavigationMap navMap = navSystem.GetMap(GetMapType());
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

    #endregion //behaviourActions

    /// <summary>
    /// Возвращает тот тип навигацинной карты, которой пользуется данный бот
    /// </summary>
    public virtual NavMapTypeEnum GetMapType()
    {
        return NavMapTypeEnum.usual;
    }

    #region optimization

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

    #endregion //optimization

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
