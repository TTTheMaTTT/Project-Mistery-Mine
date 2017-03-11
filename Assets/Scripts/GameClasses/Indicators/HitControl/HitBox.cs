using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Хитбокс - это объект, область, посредством которого наносят урон
/// </summary>
public class HitBox : HitBoxController
{

    #region consts 

    protected const float eps=.0001f;

    #endregion //consts

    #region fields

    protected BoxCollider2D col;//Область удара
    protected BoxCollider2D Col { get { if (col == null) col = GetComponent<BoxCollider2D>(); return col; } }
    protected List<string> enemies;//По каким тегам искать врагов?

    protected List<GameObject> list=new List<GameObject>();//Список всех атакованных противников. (чтобы один удар не отнимал hp дважды)

    #endregion //fields

    #region parametres

    protected bool activated;

    private int k=1;

    public override bool allyHitBox
    {
        set
        {
            if (value)
            {
                enemies.Remove("player");
                enemies.Add("enemy");
                gameObject.layer = LayerMask.NameToLayer("heroHitBox");
            }
            else
            {
                enemies.Remove("enemy");
                enemies.Add("player");
                gameObject.layer = LayerMask.NameToLayer("hitBox");
            }
        }
    }

    #endregion //parametres

    //Инициализация
    public override void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.enabled = activated;
    }

    /// <summary>
    /// Функция, обеспечивающая постоянную работу коллайдера
    /// </summary>
    public virtual void FixedUpdate()
    {
        k *= -1;
        transform.localPosition += new Vector3(k * eps, 0f, 0f);
    }

    /// <summary>
    /// Сбросить настройки хитбокса и перестать атаковать им
    /// </summary>
    public override void ResetHitBox()
    {
        StopAllCoroutines();
        activated = false;
        list.Clear();
        Col.enabled = false;
    }

    /// <summary>
    /// Настройка ХитБокса
    /// </summary>
    public override void SetHitBox(HitParametres _hitData)
    {
        activated = true;
        hitData = _hitData;
        Col.enabled = true;
        if (!immobile)
            transform.localPosition = hitData.hitPosition;
        col.size = hitData.hitSize;
        if (hitData.actTime != -1f)
        {
            StartCoroutine(HitProcess(hitData.actTime));
        }
    }

    /// <summary>
    /// Настройка хитбокса (без изменения положения и размера хитбокса)
    /// </summary>
    /// <param name="_damage">Наносимый урон</param>
    /// <param name="_actTime">Время действи (если -1, то всегда действует)</param>
    /// <param name="_hitForce">Сила отталкивания, действующая на цель</param>
    /// <param name="_dType">Тип наносимого урона</param>
    /// <param name="_eChance">Шанс особого эффекта модификатора урона</param>
    public override void SetHitBox(float _damage, float _actTime, float _hitForce, DamageType _dType=DamageType.Physical, float _eChance=0f)
    {
        activated = true;
        Col.enabled = true;
        hitData = new HitParametres(_damage,_actTime, _hitForce, _dType, _eChance);
        if (hitData.actTime != -1f)
        {
            StartCoroutine(HitProcess(hitData.actTime));
        }
    }

    /// <summary>
    /// Процесс нанесения урона
    /// </summary>
    protected override IEnumerator HitProcess(float hitTime)
    {
        activated = true;
        yield return new WaitForSeconds(hitTime);
        activated = false;
        list.Clear();
        col.enabled = false;
    }

    /// <summary>
    /// Cмотрим, попал ли хитбокс по врагу, и, если попал, то идёт расчёт урона
    /// </summary>
    void OnTriggerStay2D(Collider2D other)
    {
        
        if (activated)
        {
            if (enemies != null ? (enemies.Count == 0 ? false : enemies.Contains(other.gameObject.tag)) : true)
            {
                IDamageable target = other.gameObject.GetComponent<IDamageable>();
                if (target != null)
                {
                    float prevHP = target.GetHealth();
                    if (hitData.actTime == -1f)
                    {
                        
                        Rigidbody2D rigid;
                        if ((rigid = other.GetComponent<Rigidbody2D>()) != null && !target.InInvul())
                        {
                            rigid.AddForce((new Vector2(Mathf.Sign(transform.lossyScale.x), 0f)) * hitData.hitForce);//Атака всегда толкает вперёд
                        }
                        if ((hitData.damageType != DamageType.Physical && !target.InInvul()) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
                            target.TakeDamageEffect(hitData.damageType);
                        AIController ai = null;
                        if ((ai = other.GetComponent<AIController>()) != null)
                            ai.TakeAttackerInformation(attacker);
                        target.TakeDamage(hitData.damage, hitData.damageType, hitData.attackPower);
                        OnAttack(new HitEventArgs(target.GetHealth()-prevHP));
                        return;
                    }
                    if (!list.Contains(other.gameObject))
                    {
                        list.Add(other.gameObject);
                        Rigidbody2D rigid;
                        if ((rigid = other.GetComponent<Rigidbody2D>()) != null && !target.InInvul())
                        {
                            rigid.velocity = Vector2.zero;
                            rigid.AddForce((new Vector2(Mathf.Sign(transform.lossyScale.x), 0f)) * hitData.hitForce);//Атака всегда толкает вперёд
                        }
                        if ((hitData.damageType != DamageType.Physical && !target.InInvul()) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
                            target.TakeDamageEffect(hitData.damageType);
                        AIController ai = null;
                        if ((ai = other.GetComponent<AIController>()) != null)
                            ai.TakeAttackerInformation(attacker);
                        target.TakeDamage(hitData.damage, hitData.damageType,hitData.attackPower);
                        OnAttack(new HitEventArgs(target.GetHealth() - prevHP));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Установить, по каким целям будет попадать хитбокс
    /// </summary>
    public override void SetEnemies(List<string> _enemies)
    {
        enemies = _enemies;
    }

}


/// <summary>
/// Структура, содержащая данные об атаке
/// </summary>
[System.Serializable]
public struct HitParametres
{
    public float damage;
    public float preAttackTime, actTime, endAttackTime;//Соответственно время подготовки к атаки, самой атаки и выхода из атаки
    public Vector2 hitSize;
    public Vector2 hitPosition;
    public float hitForce;//Насколько сильно отталкивает атака
    public DamageType damageType;//Каким способом наносится атака
    [Range(0f, 100f)]public float effectChance;//Какова вероятность срабатывания особого эффекта урона
    public int attackPower;//Насколько эффектно атака сбивает

    public HitParametres(float _damage, float _actTime, Vector2 _size, Vector2 _position, float _hitForce, 
                                        DamageType _dType=DamageType.Physical, float _eChance=0f, int _attackPower = 0, float _preAttackTime=0f, float _endAttackTime=0f)
    {
        damage = _damage;
        preAttackTime = _preAttackTime;
        actTime = _actTime;
        endAttackTime = _endAttackTime;
        hitSize = _size;
        hitPosition = _position;
        hitForce = _hitForce;
        damageType = _dType;
        effectChance = _eChance;
        attackPower = _attackPower;
    }

    public HitParametres(float _damage, float _actTime, float _hitForce, DamageType _dType=DamageType.Physical, float _eChance=0f, int _attackPower = 0, float _preAttackTime = 0f, float _endAttackTime = 0f)
    {
        damage = _damage;
        preAttackTime = _preAttackTime;
        actTime = _actTime;
        endAttackTime = _endAttackTime;
        hitSize = Vector2.zero;
        hitPosition = Vector2.zero;
        hitForce = _hitForce;
        damageType = _dType;
        effectChance = _eChance;
        attackPower = _attackPower;
    }

    public HitParametres(float _damage, float _actTime, float _hitForce, float _preAttackTime = 0f, float _endAttackTime = 0f)
    {
        damage = _damage;
        preAttackTime = _preAttackTime;
        actTime = _actTime;
        endAttackTime = _endAttackTime;
        hitSize = Vector2.zero;
        hitPosition = Vector2.zero;
        hitForce = _hitForce;
        damageType = DamageType.Physical;
        effectChance = 0f;
        attackPower = 0;
    }

    public HitParametres(float _damage, float _actTime)
    {
        damage = _damage;
        preAttackTime = 0f;
        actTime = _actTime;
        endAttackTime = 0f;
        hitSize = Vector2.zero;
        hitPosition = Vector2.zero;
        hitForce = 0f;
        damageType = DamageType.Physical;
        effectChance = 0f;
        attackPower = 0;
    }

    public HitParametres(HitParametres _hit)
    {
        damage = _hit.damage;
        preAttackTime = _hit.preAttackTime;
        actTime = _hit.actTime;
        endAttackTime = _hit.endAttackTime;
        hitSize = _hit.hitSize;
        hitPosition = _hit.hitPosition;
        hitForce = _hit.hitForce;
        damageType = _hit.damageType;
        effectChance = _hit.effectChance;
        attackPower = _hit.attackPower;
    }

    public static HitParametres zero
    {
        get
        {
            return new HitParametres(0f,0f,Vector2.zero,Vector2.zero,0f,DamageType.Physical,0f);
        }
    }
    
    /// <summary>
    /// Возвращает всё время, необходимое для совершения атаки
    /// </summary>
    public float wholeAttackTime { get { return preAttackTime + actTime + endAttackTime; } }

}

/// <summary>
/// Структура, содержащая информацию об атаке, совершаемой с движением по кривой Безье
/// </summary>
[System.Serializable]
public struct SimpleCurveHitParametres
{
    public HitParametres hitParametres;
    public BezierSimpleCurve curve;

    public SimpleCurveHitParametres(HitParametres _hitParametres, BezierSimpleCurve _curve)
    {
        hitParametres = _hitParametres;
        curve = _curve;
    }

    public static SimpleCurveHitParametres zero { get { return new SimpleCurveHitParametres(HitParametres.zero, BezierSimpleCurve.zero);} }

}