using UnityEngine;
using System;
using System.Collections;


/// <summary>
/// Базовый класс для персонажей, управляемых ИИ
/// </summary>
public class AIController : CharacterController
{

    #region consts

    protected const float sightOffset = 0.1f;
    protected const float microStun = .1f;

    #endregion //consts

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

    #endregion //fields

    #region parametres

    [SerializeField]protected float sightRadius = 5f;

    protected bool agressive = false;//Находится ли ИИ в агрессивном состоянии (хочет ли он убить игрока?)
    public bool Agressive { get { return agressive; } set { agressive = value; } }

    [SerializeField]protected float acceleration = 1f;

    [SerializeField] protected float damage = 1f;
    [SerializeField] protected float hitForce = 0f;
    [SerializeField] protected Vector2 attackSize = new Vector2(.07f, .07f);
    [SerializeField] protected Vector2 attackPosition = new Vector2(0f, 0f);

    protected bool dead = false;

    #endregion //parametres

    protected override void Initialize()
    {
        base.Initialize();
        agressive = false;
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected virtual void BecomeAgressive()
    {
        agressive = true;
        mainTarget = SpecialFunctions.player;
        currentTarget = mainTarget;
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected virtual void BecomeCalm()
    {
        agressive = false;
        mainTarget = null;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        StopMoving();
        StartCoroutine(Microstun());
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
            if (eData.agressive)
                BecomeAgressive();
            
            Health = eData.health;
        }
    }

}
