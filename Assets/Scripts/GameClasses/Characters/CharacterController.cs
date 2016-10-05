using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Базовый класс для персонажей, имеющих некоторую самостоятельность (искусственный интеллект или управляемый игрок)
/// </summary>
public class CharacterController : MonoBehaviour, IDamageable
{

    #region consts

    protected const int maxEmployment = 10;

    #endregion //consts

    #region fields

    [SerializeField]
    protected List<string> enemies = new List<string>();//Список тегов игровых объектов, которых данный персонаж считает за врагов и может атаковать

    protected Rigidbody2D rigid;
    protected HitBox hitBox;//То, чем атакует персонаж

    protected CharacterVisual anim;//Визуальное представление персонажа

    #endregion //fields

    #region parametres

    [SerializeField] protected float maxHealth=100f;
    [SerializeField] protected float health = 100f;
    public virtual float Health { get { return health; } set { health = value; } }

    [SerializeField] protected float speed = 1f;

    protected OrientationEnum orientation; //В какую сторону повёрнут персонаж

    protected int employment = maxEmployment;

    #endregion //parametres

    #region eventHandlers

    public EventHandler<AnimationEventArgs> AnimationEventHandler;//Хэндлер события о визуализации действия

    #endregion //eventHandlers

    protected virtual void Awake ()
    {
        Initialize();
	}

    /// <summary>
    /// Функция инициализации
    /// </summary>
    protected virtual void Initialize()
    {
        rigid = GetComponent<Rigidbody2D>();

        hitBox = GetComponentInChildren<HitBox>();
        hitBox.SetEnemies(enemies);

        orientation = (OrientationEnum)Mathf.RoundToInt(Mathf.Sign(transform.localScale.x));

        anim = GetComponentInChildren<CharacterVisual>();
        if (anim != null)
        {
            AnimationEventHandler += anim.AnimateIt;
        }

        employment = maxEmployment;

    }


    /// <summary>
    /// Функция, ответственная за анализ окружающей обстановки
    /// </summary>
    protected virtual void Analyse()
    {}

    /// <summary>
    /// Функция, ответственная за перемещения персонажа
    /// </summary>
    protected virtual void Move(OrientationEnum _orientation)
    {}

    /// <summary>
    /// Прекратить перемещение
    /// </summary>
    protected virtual void StopMoving()
    {}

    /// <summary>
    /// Поворот
    /// </summary>
    protected virtual void Turn(OrientationEnum _orientation)
    {
        Vector3 vect = transform.localScale;
        orientation = _orientation;
        transform.localScale = new Vector3(-1 * vect.x, vect.y, vect.z);
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected virtual void Attack()
    {}

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected virtual IEnumerator AttackProcess()
    {
        yield return 0;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        Health = Mathf.Clamp(Health - damage, 0f, 100f);
        if (health <= 0f)
            Death();
        else
            Animate(new AnimationEventArgs("hitted"));
    }

    /// <summary>
    /// Функция смерти персонажа
    /// </summary>
    protected virtual void Death()
    {
    }

    #region events

    /// <summary>
    /// Событие, вызываемое при запросе новой анимации у аниматора персонажа
    /// </summary>
    public void Animate(AnimationEventArgs e)
    {
        EventHandler<AnimationEventArgs> handler = AnimationEventHandler;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

}
