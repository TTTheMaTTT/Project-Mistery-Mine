using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Базовый класс для персонажей, имеющих некоторую самостоятельность (искусственный интеллект или управляемый игрок)
/// </summary>
public class CharacterController : MonoBehaviour, IDamageable, IHaveStory
{

    #region delegates

    public delegate void storyActionDelegate(StoryAction _action);

    #endregion //delegates

    #region dictionaries

    protected Dictionary<string, storyActionDelegate> storyActionBase = new Dictionary<string, storyActionDelegate>(); //Словарь сюжетных действий
    public Dictionary<string, storyActionDelegate> StoryActionBase { get { return storyActionBase; } }

    #endregion //dictionaries

    #region consts

    protected const int maxEmployment = 10;

    #endregion //consts

    #region fields

    [SerializeField]
    protected List<string> enemies = new List<string>();//Список тегов игровых объектов, которых данный персонаж считает за врагов и может атаковать

    protected Rigidbody2D rigid;
    protected HitBox hitBox;//То, чем атакует персонаж
    
    protected CharacterVisual anim;//Визуальное представление персонажа

    protected Transform indicators;//Игровой объект, в котором находятся индикаторы

    #endregion //fields

    #region parametres

    [SerializeField] protected float maxHealth=100f;
    [SerializeField] protected float health = 100f;
    public virtual float Health { get { return health; } set { health = value; } }

    [SerializeField] protected float speed = 1f;

    protected OrientationEnum orientation; //В какую сторону повёрнут персонаж

    protected int employment = maxEmployment;

    [SerializeField]
    protected bool immobile;//Можно ли управлять персонажем

    #endregion //parametres

    #region eventHandlers

    public EventHandler<AnimationEventArgs> AnimationEventHandler;//Хэндлер события о визуализации действия

    public EventHandler<StoryEventArgs> CharacterDeathEvent;//Событие о смерти персонажа

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

        if (transform.FindChild("HitBox")!=null)
            hitBox = transform.FindChild("HitBox").GetComponent<HitBox>();

        if (hitBox!=null)
            hitBox.SetEnemies(enemies);

        orientation = (OrientationEnum)Mathf.RoundToInt(Mathf.Sign(transform.localScale.x));

        anim = GetComponentInChildren<CharacterVisual>();
        if (anim != null)
        {
            AnimationEventHandler += anim.AnimateIt;
        }

        employment = maxEmployment;

        FormDictionaries();

    }

    /// <summary>
    /// Сформировать словари стори-действий
    /// </summary>
    protected virtual void FormDictionaries()
    {
        storyActionBase = new Dictionary<string, storyActionDelegate>();
    }

    /// <summary>
    /// Функция, ответственная за анализ окружающей обстановки
    /// </summary>
    protected virtual void Analyse()
    {}

    /// <summary>
    /// Определить, есть ли необходимость отыскания пути до главной цели
    /// </summary>
    /// <returns>есть ли необходимость</returns>
    protected virtual bool NeedToFindPath()
    {
        return true;
    }

    /// <summary>
    /// Функция, ответственная за перемещения персонажа
    /// </summary>
    protected virtual void Move(OrientationEnum _orientation)
    {}

    /// <summary>
    /// Прекратить перемещение
    /// </summary>
    protected virtual void StopMoving()
    {
        rigid.velocity = new Vector2(0f, rigid.velocity.y);
    }

    /// <summary>
    /// Залезть на лестницу
    /// </summary>
    protected virtual void LadderOn()
    {
        if (orientation == OrientationEnum.left)
        {
            Turn(OrientationEnum.right);
        }
        rigid.velocity = Vector3.zero;
        rigid.gravityScale = 0f;
    }

    /// <summary>
    /// Слезть с лестницы
    /// </summary>
    protected virtual void LadderOff()
    {
        rigid.gravityScale = 1f;
        //rigid.AddForce(new Vector2(0f, jumpForce / 2));
    }

    /// <summary>
    /// Передвижение по лестнице
    /// </summary>
    /// <param name="direction">Число, характеризующее направление движения. Если >0, то вверх, иначе - вниз</param>
    protected virtual void LadderMove(float direction)
    {
    }

    /// <summary>
    /// Остановиться на лестнице
    /// </summary>
    protected virtual void StopLadderMoving()
    {
        rigid.velocity = Vector2.zero;
    }

    /// <summary>
    /// Поворот
    /// </summary>
    protected virtual void Turn(OrientationEnum _orientation)
    {
        if (orientation != _orientation)
        {
            Vector3 vect = transform.localScale;
            orientation = _orientation;
            transform.localScale = new Vector3(-1 * vect.x, vect.y, vect.z);
        }
    }

    /// <summary>
    /// Поворот
    /// </summary>
    protected virtual void Turn()
    {
        Vector3 vect = transform.localScale;
        orientation = (OrientationEnum)(-1*(int)orientation);
        transform.localScale = new Vector3(-1 * vect.x, vect.y, vect.z);
    }

    /// <summary>
    /// Совершить прыжок
    /// </summary>
    protected virtual void Jump()
    {
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
        Health = Mathf.Clamp(Health - damage, 0f, maxHealth);
        if (health <= 0f)
        {
            Death();
            return;
        }
        else
            Animate(new AnimationEventArgs("hitted"));
    }

    /// <summary>
    /// Ещё одна функция получения урона, что не обращает на неуязвимость поражённого персонажа, если таковая имеется
    /// </summary>
    public virtual void TakeDamage(float damage, bool ignoreInvul)
    {
        Health = Mathf.Clamp(Health - damage, 0f,maxHealth);
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
        SpecialFunctions.StartStoryEvent(this, CharacterDeathEvent, new StoryEventArgs());
    }

    /// <summary>
    /// Задать персонажу управляемость
    /// </summary>
    public void SetImmobile(bool _immobile)
    {
        immobile = _immobile;
    }

    public float GetHealth()
    {
        return health;
    }

    public virtual bool InInvul()
    {
        return false;
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

    #region IHasID

    /// <summary>
    /// Вернуть id персонажа
    /// </summary>
    public int GetID()
    {
        return -1;
    }

    /// <summary>
    /// Установить заданное id
    /// </summary>
    public void SetID(int _id)
    {
    }

    /// <summary>
    /// Настроить персонажа в соответствии с сохранёнными данными
    /// </summary>
    public void SetData(InterObjData _intObjData)
    {
    }

    /// <summary>
    /// Вернуть сохраняемые данные персонажа
    /// </summary>
    public InterObjData GetData()
    {
        return null;
    }

    #endregion //IHasID

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public virtual List<string> actionNames()
    {
        return new List<string>() { };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Вернуть словарь id-шников, связанных с конкретной функцией проверки условия сюжетного события
    /// </summary>
    public virtual Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>() {
                                                        {"", new List<string>() },
                                                        {"compare",new List<string>()} };
    }

    #endregion //IHaveStory

}
