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
    protected const float frozenCrushChance = .05f;//Шанс разбить персонажа, когда он находится в замороженном состоянии

    #endregion //consts

    #region fields

    [SerializeField]
    protected List<string> enemies = new List<string>();//Список тегов игровых объектов, которых данный персонаж считает за врагов и может атаковать

    protected Rigidbody2D rigid;
    protected HitBoxController hitBox;//То, чем атакует персонаж
    
    protected CharacterVisual anim;//Визуальное представление персонажа

    protected Transform indicators;//Игровой объект, в котором находятся индикаторы

    protected List<BuffClass> buffs = new List<BuffClass>();//Список эффектов, баффов, дебаффов, действующих на персонажа
    public List<BuffClass> Buffs { get { return buffs; } }

    #endregion //fields

    #region parametres

    [SerializeField] protected float maxHealth=100f;
    [SerializeField] protected float health = 100f;
    public virtual float Health { get { return health; } set { health = value; } }
    public virtual float MaxHealth { get { return maxHealth; } set { maxHealth = value; } }

    [SerializeField]protected int balance = 0;//Баланс персонажа. Если при получаемый урон имеет параметр силы атаки больше баланса, то персонаж сбивается, вводится в микростан
    public virtual int Balance { get { return balance; } set { balance = value; } }

    [SerializeField] protected float speed = 1f;
    protected float speedCoof = 1f;//Коэффициент скорости

    public virtual bool OnLadder { get { return false; } }//Находится ли персонаж на лестнице

    protected OrientationEnum orientation; //В какую сторону повёрнут персонаж

    protected int employment = maxEmployment;
    protected bool dead = false;//Мёртв ли персонаж
    public bool Dead { get { return dead; } }

    [SerializeField]
    protected bool immobile;//Можно ли управлять персонажем

    protected bool underWater;//Находится ли персонаж под водой?
    protected virtual bool Underwater//Свойство, которое описывает погружение и выход из воды
    {
        get
        {
            return underWater;
        }
        set
        {
            underWater = value;
            if (value)
            {
                if (GetBuff("FrozenProcess") == null)
                {
                    BecomeWet(0f);
                    StopCoroutine("WetProcess");
                    Animate(new AnimationEventArgs("stopWet"));
                }
            }
            else
            {
                if (GetBuff("FrozenProcess") == null)
                {
                    RemoveBuff("WetProcess");
                    BecomeWet(0f);
                }
            }
        }
    }

    #region effect

    protected virtual float stunTime { get { return 2f; } }

    protected virtual float burningTime { get { return 2f; } }
    protected virtual float burnFrequency { get { return 1f; } }
    protected virtual float burnDamage { get { return 10f; } }

    protected virtual float poisonTime { get { return 3f; } }
    protected virtual float poisonFrequency { get { return 1f; } }
    protected virtual float poisonDamage { get { return 8f; } }

    protected virtual float coldTime { get { return 2f; } }
    protected virtual float coldSpeedCoof { get { return .7f; } }//Коэффициент, на который домножается скорость персонажа при заморозке

    protected virtual float wetTime { get { return 2f; } }
    protected virtual float frozenTime { get { return 3.5f; } }

    #endregion //effectTimes

    [SerializeField]
    protected bool questCharacter = false;//Является и персонаж квестовым (его смерть не вызывает игровых эффектов)

    protected List<Timer> timers = new List<Timer>();
    protected virtual List<Timer> Timers { get { return new List<Timer>(); } }
    protected List<TimerInfo> timersInfo = new List<TimerInfo>();

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
            hitBox = transform.FindChild("HitBox").GetComponent<HitBoxController>();

        if (hitBox != null)
        {
            hitBox.AttackerInfo = new AttackerClass(gameObject,AttackTypeEnum.melee);
            hitBox.SetEnemies(enemies);
        }

        orientation = (OrientationEnum)Mathf.RoundToInt(Mathf.Sign(transform.localScale.x));

        anim = GetComponentInChildren<CharacterVisual>();
        if (anim != null)
        {
            AnimationEventHandler += anim.AnimateIt;
        }

        employment = maxEmployment;
        speedCoof = 1f;

        FormDictionaries();
        SetTimers();

        buffs = new List<BuffClass>();
    }

    /// <summary>
    /// Сформировать словари стори-действий
    /// </summary>
    protected virtual void FormDictionaries()
    {
        storyActionBase = new Dictionary<string, storyActionDelegate>();
        storyActionBase.Add("destroy", StoryDestroy);
        storyActionBase.Add("death", StoryDeath);
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
    public virtual void Turn(OrientationEnum _orientation)
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
    /// Прекратить атаку
    /// </summary>
    protected virtual void StopAttack()
    {
        StopCoroutine("AttackProcess");
    }

    /// <summary>
    /// Функция, вызываемая при получении урона, оповещающая о субъекте нападения
    /// </summary>
    /// <param name="attackerInfo">Кто атаковал персонажа</param>
    public virtual void TakeAttackerInformation(AttackerClass attackerInfo)
    {}

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public virtual void TakeDamage(HitParametres hitData)
    {
        Health = Mathf.Clamp(Health - hitData.damage, 0f, maxHealth);
        if (health <= 0f)
        {
            Death();
            return;
        }
        else
        {
            if ((hitData.damageType != DamageType.Physical) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
                TakeDamageEffect(hitData.damageType);
            if (hitData.damageType == DamageType.Physical || hitData.damageType == DamageType.Crushing)
                if (UnityEngine.Random.Range(0f, 1f) < frozenCrushChance)
                    if (GetBuff("FrozenProcess") != null)
                    {
                        Death();//Персонаж, находясь в замороженном состоянии, рискует быть мгновенно убит
                        return;
                    }
            Animate(new AnimationEventArgs("hitted","", hitData.attackPower>balance ? 0 :1));
        }
    }

    /// <summary>
    /// Ещё одна функция получения урона, что не обращает на неуязвимость поражённого персонажа, если таковая имеется
    /// </summary>
    public virtual void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
        Health = Mathf.Clamp(Health - hitData.damage, 0f,maxHealth);
        if (health <= 0f)
            Death();
        else
        {
            if ((hitData.damageType != DamageType.Physical) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
                TakeDamageEffect(hitData.damageType);
            if (hitData.damageType == DamageType.Physical || hitData.damageType == DamageType.Crushing)
                if (UnityEngine.Random.Range(0f, 1f) < frozenCrushChance)
                    if (GetBuff("FrozenProcess") != null)
                        Death();//Персонаж, находясь в замороженном состоянии, рискует быть мгновенно убит
            Animate(new AnimationEventArgs("hitted","",hitData.attackPower>balance?0:1));
        }
    }

    /// <summary>
    /// Функция получения специального эффекта, зависящего от
    /// </summary>
    /// <param name="_dType">тип урона, за которым следует эффект</param>
    public virtual void TakeDamageEffect(DamageType _dType)
    {
        switch (_dType)
        {
            case DamageType.Crushing:
                {
                    BecomeStunned(0f);
                    break;
                }
            case DamageType.Fire:
                {
                    BecomeBurning(0f);
                    break;
                }
            case DamageType.Cold:
                {
                    BecomeCold(0f);
                    break;
                }
            case DamageType.Water:
                {
                    BecomeWet(0f);
                    break;
                }
            case DamageType.Poison:
                {
                    BecomePoisoned(0f);
                    break;
                }
        }
    }

    /// <summary>
    /// Функция смерти персонажа
    /// </summary>
    protected virtual void Death()
    {
        dead = true;
        if (this is AIController)
        {
            AIController ai = (AIController)this;
            if (ai.Loyalty != LoyaltyEnum.ally && !(this is BossController) && !questCharacter)
                SpecialFunctions.gameController.AddRandomDeathGameEffect(this);
        }
        if (!dead)
            return;
        SpecialFunctions.StartStoryEvent(this, CharacterDeathEvent, new StoryEventArgs());
        for (int i = buffs.Count-1; i >=0; i--)
        {
            BuffClass buff = buffs[i]; 
            StopCustomBuff(new BuffData(buff));
        }
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

    /// <summary>
    /// Установить таймеры
    /// </summary>
    protected void SetTimers()
    {
        timers = Timers;
    }

    /// <summary>
    /// Сбросить все таймеры
    /// </summary>
    protected void ResetTimers()
    {
        for (int i = timersInfo.Count - 1; i > -0; i--)
            StopTimer(timersInfo[i].timerName, true);
    }

    /// <summary>
    /// Запустить таймер с данным именем
    /// </summary>
    /// <param name="_timerName">Название таймера</param>
    protected void StartTimer(string _timerName)
    {
        Timer _timer = timers.Find(x => x.timerName == _timerName);
        TimerInfo _info = timersInfo.Find(x => x.timerName == _timerName);
        if (_timer != null && _info == null)
        {
            IEnumerator _timerProcess = _timer.TimerProcess(this);
            StartCoroutine(_timerProcess);
            _timer.active = true;
            timersInfo.Add(new TimerInfo(_timerName, _timerProcess));            
        }
    }

    /// <summary>
    /// Остановить таймер с данным названием
    /// </summary>
    /// <param name="_timerName">название таймера</param>
    /// <param name="resetProcess"></param>
    public void StopTimer(string _timerName, bool resetProcess)
    {
        TimerInfo _info = timersInfo.Find(x => x.timerName == _timerName);
        if (resetProcess)
        {
            Timer _timer = timers.Find(x => x.timerName == _timerName);
            if (_timer != null)
                _timer.active = false;
        }
        if (_info != null)
        {
            if (resetProcess)
                StopCoroutine(_info.process);
            timersInfo.Remove(_info);
        }
    }

    /// <summary>
    /// Узнать, активен ли таймер с заданным именем
    /// </summary>
    /// <param name="_timerName">Название таймера</param>
    protected bool IsTimerActive(string _timerName)
    {
        Timer _timer = timers.Find(x => x.timerName == _timerName);
        if (_timer == null)
            return false;
        else
            return _timer.active;
    }

    #region buffs

    /// <summary>
    /// Вернуть бафф с заданным именем из списка баффов (если его нет, то вернуть null)
    /// </summary>
    protected virtual BuffClass GetBuff(string _bName)
    {
        BuffClass _buff = buffs.Find(x => x.buffName == _bName);
        return _buff;
    }

    /// <summary>
    ///Добавить новый бафф в список активных эффектов 
    /// </summary>
    /// <param name="_buff">новый бафф</param>
    public virtual void AddBuff(BuffClass _buff)
    {
        buffs.Add(_buff);
    }

    /// <summary>
    /// Убрать бафф с указанным именем из списка баффов (если он существует)
    /// </summary>
    public virtual void RemoveBuff(string _bName)
    {
        BuffClass _buff = GetBuff(_bName);
        if (_buff != null)
            buffs.Remove(_buff);
    }

    /// <summary>
    /// Добавить бафф в соответствии с данными этого баффа
    /// </summary>
    /// <param name="_bData">данные баффа</param>
    protected virtual void AddCustomBuff(BuffData _bData)
    {
        switch (_bData.buffName)
        {
            case "StunnedProcess":
                {
                    BecomeStunned(_bData.buffDuration);
                    break;
                }
            case "BurningProcess":
                {
                    BecomeBurning(_bData.buffDuration);
                    break;
                }
            case "PoisonProcess":
                {
                    BecomePoisoned(_bData.buffDuration);
                    break;
                }
            case "WetProcess":
                {
                    BecomeWet(_bData.buffDuration);
                    break;
                }
            case "ColdProcess":
                {
                    BecomeCold(_bData.buffDuration);
                    break;
                }
            case "FrozenProcess":
                {
                    BecomeFrozen(_bData.buffDuration);
                    break;
                }
            default:
                {
                    //SpecialFunctions.gameController.SetBuffData(_bData, this);
                    break;
                }
        }
    }

    /// <summary>
    /// Снять бафф с указанным именем
    /// </summary>
    protected virtual void StopCustomBuff (BuffData _bData)
    {
        switch (_bData.buffName)
        {
            case "StunnedProcess":
                {
                    StopStun();
                    break;
                }
            case "BurningProcess":
                {
                    StopBurning();
                    break;
                }
            case "PoisonProcess":
                {
                    StopPoison();
                    break;
                }
            case "WetProcess":
                {
                    StopWet();
                    break;
                }
            case "ColdProcess":
                {
                    StopCold();
                    break;
                }
            case "FrozenProcess":
                {
                    StopFrozen();
                    break;
                }
            default:
                {
                    SpecialFunctions.gameController.StopBuff(_bData);
                    break;
                }
        }
    }

    /// <summary>
    /// Получить стан
    /// </summary>
    protected virtual void BecomeStunned(float _time)
    {
        if (GetBuff("StunnedProcess") != null)//Если на персонаже уже висит стан, то нельзя навесить ещё один
            return;
        if (GetBuff("FrozenProcess") != null)
        {
            Death();//Если персонаж находится в замороженном состоянии, то он будет разбит сокрушающим ударом
            return;
        }
        else if (health < maxHealth/2f)
        {
            if (GetBuff("BurningProcess") != null)
            {
                AddBuff(new BuffClass("StunnedProcess", Time.fixedTime, _time));
                Death();//Если персонаж находится в подожённом состоянии, то при стане он должен осыпаться пеплом
                return;
            }
        }
        StartCoroutine("StunnedProcess", _time == 0 ? stunTime : _time);
    }

    /// <summary>
    /// Процесс стана
    /// </summary>
    /// <param name="_time">Время действия стана</param>
    protected virtual IEnumerator StunnedProcess(float _time)
    {
        AddBuff(new BuffClass("StunnedProcess", Time.fixedTime, _time));
        immobile = true;//Запретить двигаться во время стана
        Animate(new AnimationEventArgs("startStun"));
        yield return new WaitForSeconds(_time);
        Animate(new AnimationEventArgs("stopStun"));
        RemoveBuff("StunnedProcess");
        immobile = false;
    }

    /// <summary>
    /// Прервать стан
    /// </summary>
    protected virtual void StopStun()
    {
        if (GetBuff("StunnedProcess") == null)
            return;
        StopCoroutine("StunnedProcess");
        Animate(new AnimationEventArgs("stopStun"));
        RemoveBuff("StunnedProcess");
        immobile = false;
    }

    /// <summary>
    /// Получить поджог
    /// </summary>
    protected virtual void BecomeBurning(float _time)
    {
        if (GetBuff("BurningProcess") != null)
            return;
        if (GetBuff("WetProcess") != null)
            return;//Нельзя мокрого персонажа
        if (health<maxHealth/2f)
        {
            if (GetBuff("StunnedProcess") != null)
            {
                AddBuff(new BuffClass("BurningProcess", Time.fixedTime, _time));
                Death();//Если персонажа подожгли, когда он находился в стане, то он осыпется пеплом
            }
        }
        StopCold();//Согреться
        if (GetBuff("FrozenProcess")!=null)
        {
            StopFrozen();//Если персонажа подожгли, когда он был заморожен, то он отмараживается и не получает никакого урона от огня, так как считаем, что всё тепло ушло на разморозку
            return;
        }
        StartCoroutine("BurningProcess",_time == 0 ? burningTime : _time);
    }

    /// <summary>
    /// Процесс горения
    /// </summary>
    /// <param name="_time">Время горения</param>
    protected virtual IEnumerator BurningProcess(float _time)
    {
        AddBuff(new BuffClass("BurningProcess", Time.fixedTime, _time));
        Animate(new AnimationEventArgs("startBurning"));
        StartCoroutine("BurningDamageProcess");
        yield return new WaitForSeconds(_time);
        RemoveBuff("BurningProcess");
        StopCoroutine("BurningDamageProcess");
        Animate(new AnimationEventArgs("stopBurning"));
    }

    /// <summary>
    /// Процесс периодического получения урона при поджоге
    /// </summary>
    protected virtual IEnumerator BurningDamageProcess()
    {
        while (true)
        {
            yield return new WaitForSeconds(burnFrequency);
            TakeDamage(new HitParametres(burnDamage, DamageType.Fire), true);
        }
    }

    /// <summary>
    /// Прекратить горение
    /// </summary>
    protected virtual void StopBurning()
    {
        if (GetBuff("BurningProcess") == null)
            return;
        StopCoroutine("BurningProcess");
        RemoveBuff("BurningProcess");
        StopCoroutine("BurningDamageProcess");
        Animate(new AnimationEventArgs("stopBurning"));
    }

    /// <summary>
    /// Промокнуть
    /// </summary>
    protected virtual void BecomeWet(float _time)
    {
        if (GetBuff("WetProcess") != null)
            return;
        StopBurning();
        StartCoroutine("WetProcess",_time == 0 ? wetTime : _time);
        if (GetBuff("ColdProcess") != null)
        {   
            BecomeFrozen(0f);//Если холодного персонажа облили водой, то он охлаждается
        }
    }

    /// <summary>
    /// Процесс мокрого состояния
    /// </summary>
    /// <param name="_time">Продолжительность процесса</param>
    protected virtual IEnumerator WetProcess(float _time)
    {
        AddBuff(new BuffClass("WetProcess", Time.fixedTime, _time));
        Animate(new AnimationEventArgs("startWet"));
        yield return new WaitForSeconds(_time);
        RemoveBuff("WetProcess");
        Animate(new AnimationEventArgs("stopWet"));
        //underWater = false;
    }

    /// <summary>
    /// Высохнуть
    /// </summary>
    protected virtual void StopWet()
    {
        if (GetBuff("WetProcess") == null)
            return;
        StopCoroutine("WetProcess");
        RemoveBuff("WetProcess");
        Animate(new AnimationEventArgs("stopWet"));
        //underWater = false;
    }

    /// <summary>
    /// Замёрзнуть 
    /// </summary>
    protected virtual void BecomeCold(float _time)
    {
        if (GetBuff("ColdProcess") != null)
            return;//Нельзя во второй раз замёрзнуть
        if (GetBuff("BurningProcess") != null)
            return;//Нельзя замёрзнуть, когда горишь
        StartCoroutine("ColdProcess",_time == 0f ? coldTime : _time);
        if (GetBuff("WetProcess") != null)
            BecomeFrozen(0f);//Если мокрого персонажа охладить, то он заморозится
    }

    /// <summary>
    /// Процесс замерзания
    /// </summary>
    /// <param name="_time">Длительность процесса</param>
    /// <returns></returns>
    protected virtual IEnumerator ColdProcess(float _time)
    {
        AddBuff(new BuffClass("ColdProcess", Time.fixedTime, _time));
        speedCoof *= coldSpeedCoof;
        Animate(new AnimationEventArgs("startCold"));
        yield return new WaitForSeconds(_time);
        speedCoof /= coldSpeedCoof;
        Animate(new AnimationEventArgs("stopCold"));
        RemoveBuff("ColdProcess");
    }

    /// <summary>
    /// Согреться
    /// </summary>
    protected virtual void StopCold()
    {
        if (GetBuff("ColdProcess") == null)
            return;
        StopCoroutine("ColdProcess");
        speedCoof /= coldSpeedCoof;
        RemoveBuff("ColdProcess");
        Animate(new AnimationEventArgs("stopCold"));
    }

    /// <summary>
    /// Отравиться
    /// </summary>
    protected virtual void BecomePoisoned(float _time)
    {
        if (GetBuff("PoisonProcess") != null)
            return;//Нельзя отравиться, будучи отравленным
        StartCoroutine("PoisonProcess",_time == 0f ? poisonTime : _time);
    }

    /// <summary>
    /// Процесс отравления
    /// </summary>
    /// <param name="_time">Длительность процесса</param>
    protected virtual IEnumerator PoisonProcess(float _time)
    {
        AddBuff(new BuffClass("PoisonProcess", Time.fixedTime, _time));
        StartCoroutine("PoisonDamageProcess");
        Animate(new AnimationEventArgs("startPoison"));
        yield return new WaitForSeconds(_time);
        RemoveBuff("PoisonProcess");
        StopCoroutine("PoisonDamageProcess");
        Animate(new AnimationEventArgs("stopPoison"));
    }

    /// <summary>
    /// Процесс регулярного получения ядовитого урона
    /// </summary>
    protected virtual IEnumerator PoisonDamageProcess()
    {
        while (true)
        {
            yield return new WaitForSeconds(poisonFrequency);
            TakeDamage(new HitParametres(poisonDamage, DamageType.Poison, 0),true);
        }
    }

    /// <summary>
    /// Оправиться от отравления
    /// </summary>
    protected virtual void StopPoison()
    {
        if (GetBuff("PoisonProcess") == null)
            return;
        RemoveBuff("PoisonProcess");
        StopCoroutine("PoisonDamageProcess");
        StopCoroutine("PoisonProcess");
        Animate(new AnimationEventArgs("stopPoison"));
    }

    /// <summary>
    /// Заморозиться
    /// </summary>
    protected virtual void BecomeFrozen(float _time)
    {
        StopWet();
        StopCold();
        StopStun();
        StartCoroutine("FrozenProcess",_time == 0f ? frozenTime : _time);
    }

    /// <summary>
    /// Процесс нахождения в замороженном состоянии
    /// </summary>
    /// <param name="_time">Длительность процесса</param>
    protected virtual IEnumerator FrozenProcess(float _time)
    {
        AddBuff(new BuffClass("FrozenProcess", Time.fixedTime, _time));
        immobile = true;//Потерять способность двигаться
        Animate(new AnimationEventArgs("startFrozen"));
        Underwater = false;//Считаем, что в замороженном состоянии персонажу не требуется энергия, поэтому он не будет задыхаться
        yield return new WaitForSeconds(_time);
        RemoveBuff("FrozenProcess");
        immobile = false;
        Animate(new AnimationEventArgs("stopFrozen"));
    }

    /// <summary>
    /// Отморозиться
    /// </summary>
    protected virtual void StopFrozen()
    {
        if (GetBuff("FrozenProcess") == null)
            return;
        RemoveBuff("FrozenProcess");
        immobile = false;
        Animate(new AnimationEventArgs("stopFrozen"));
        StopCoroutine("FrozenProcess");
    }

    #endregion //buffs

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

    #region IHaveID

    /// <summary>
    /// Вернуть id персонажа
    /// </summary>
    public virtual int GetID()
    {
        return -1;
    }

    /// <summary>
    /// Установить заданное id
    /// </summary>
    public virtual void SetID(int _id)
    {
        
    }

    /// <summary>
    /// Настроить персонажа в соответствии с сохранёнными данными
    /// </summary>
    public virtual void SetData(InterObjData _intObjData)
    {
    }

    /// <summary>
    /// Вернуть сохраняемые данные персонажа
    /// </summary>
    public virtual InterObjData GetData()
    {
        return null;
    }

    public void SetBuffs(BuffListData _bListData)
    {
        foreach (BuffData _bData in _bListData.buffs)
            AddCustomBuff(_bData);
    }

    #endregion //IHaveID

    #region storyActions

    /// <summary>
    /// Уничтожение объекта в результате скриптового события
    /// </summary>
    public void StoryDestroy(StoryAction _action)
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Смерть персонажа в результате скриптового действия
    /// </summary>
    protected virtual void StoryDeath(StoryAction _action)
    {
        StartCoroutine(StoryDeathProcess(_action.argument / 10f));
    }

    protected virtual IEnumerator StoryDeathProcess(float deathTime)
    {
        yield return new WaitForSeconds(deathTime);
        Death();
    }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public virtual List<string> actionNames()
    {
        return new List<string>() {"destroy","death"};
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() { { "destroy", new List<string>() { } },
                                                        { "death", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() { { "destroy", new List<string>() { } },
                                                        { "death", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь id-шников, связанных с конкретной функцией проверки условия сюжетного события
    /// </summary>
    public virtual Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>() {
                                                        {"", new List<string>() },
                                                        {"compare",new List<string>()},
                                                        { "compareHistoryProgress",SpecialFunctions.statistics.HistoryBase.stories.ConvertAll(x=>x.storyName)}};
    }

    /// <summary>
    /// Возвращает ссылку на сюжетное действие, соответствующее данному имени
    /// </summary>
    public StoryAction.StoryActionDelegate GetStoryAction(string s)
    {
        if (storyActionBase.ContainsKey(s))
            return storyActionBase[s].Invoke;
        else
            return null;
    }

    #endregion //IHaveStory

}

/// <summary>
/// Структура, представляющий собой таймер
/// </summary>
public class Timer
{

    public string timerName = "timer";
    public bool active = false;
    public float duration = 0f;

    public Timer(string _timerName, float _duration)
    {
        timerName = _timerName;
        active = false;
        duration = _duration;
    }

    public IEnumerator TimerProcess(CharacterController _char)
    {
        active = true;
        yield return new WaitForSeconds(duration);
        active = false;
        _char.StopTimer(timerName, false);
    }

}

/// <summary>
/// Информация об активном таймере
/// </summary>
public class TimerInfo
{

    public string timerName = "timer";
    public IEnumerator process;

    public TimerInfo(string _timerName, IEnumerator _process)
    {
        timerName = _timerName;
        process = _process;
    }

}