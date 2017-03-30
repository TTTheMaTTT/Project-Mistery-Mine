using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Коробка с содержимым
/// </summary>
public class BoxController : MonoBehaviour, IDamageable
{

    #region consts

    protected const float dropForceX = 25f, dropForceY = 80f;
    protected const float deathTime = .05f;

    #endregion //consts

    #region eventHandlers

    public EventHandler<EventArgs> BoxDestroyedEvent;//Событие "Коробка была разрушена"

    #endregion //eventHandlers

    #region fields

    [SerializeField]
    protected List<GameObject> content = new List<GameObject>();//Содержимое коробки, что вываливается из неё

    protected Animator anim;
    protected AudioSource aSource;

    #endregion //fields

    #region parametres

    [SerializeField] protected float maxHealth = 10f;
    [SerializeField] protected float health = 10f;

    [SerializeField][HideInInspector]int id;

    #endregion //parametres

    protected virtual void Awake()
    {
        Initialize();
    }

    protected void Initialize()
    {
        health = maxHealth;
        anim = GetComponent<Animator>();
        aSource = GetComponent<AudioSource>();
        if (aSource == null)
            aSource = gameObject.AddComponent<AudioSource>();
    }

    #region IDamageable

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public void TakeDamage(float damage, DamageType _dType, int attackPower=0)
    {
        health -= damage;
        StopAllCoroutines();
        if (health <= 0f)
            Destroy();
        else
            StartCoroutine(HitProcess());
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public void TakeDamage(float damage,DamageType _dType, bool ignoreInvul, int attackPower=0)
    {
        health -= damage;
        StopAllCoroutines();
        if (health <= 0f)
            Destroy();
        else
            StartCoroutine(HitProcess());
    }

    /// <summary>
    /// Подвергнуться эффекту, связанному с типом урона
    /// </summary>
    /// <param name="_dType">Тип урона</param>
    public void TakeDamageEffect(DamageType _dType)
    {}

    public bool InInvul()
    {
        return false;
    }

    /// <summary>
    /// Что произойдёт, когда коробка будет уничтожена
    /// </summary>
    public void Destroy()
    {
        foreach (GameObject drop in content)
        {
            GameObject drop1 = Instantiate(drop, transform.position, transform.rotation) as GameObject;
            Rigidbody2D rigid = drop1.GetComponent<Rigidbody2D>();
            if (rigid != null)
                rigid.AddForce(new Vector2(UnityEngine.Random.Range(-dropForceX, dropForceX), dropForceY));
        }
        SpecialFunctions.PlaySound(aSource);
        OnBoxDestroyed(new EventArgs());
        StartCoroutine(DestroyProcess());
    }

    /// <summary>
    /// Узнать текущее здоровье
    /// </summary>
    public float GetHealth()
    {
        return health;
    }

    protected virtual IEnumerator HitProcess()
    {
        if (anim != null)
        {
            anim.Play("TakeDamage");
            yield return new WaitForSeconds(.1f);
            if (anim!=null)
                anim.Play("Idle");
        }
    }

    protected virtual IEnumerator DestroyProcess()
    {
        Destroy(anim);
        Destroy(GetComponent<SpriteRenderer>());
        Destroy(GetComponent<Collider2D>());
        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }

    #endregion //IDamageable

    #region events

    /// <summary>
    /// Событие "коробка была разрушена"
    /// </summary>
    protected virtual void OnBoxDestroyed(EventArgs e)
    {
        EventHandler<EventArgs> handler = BoxDestroyedEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

    #region id

    /// <summary>
    /// Вернуть id
    /// </summary>
    public int GetID()
    {
        return id;
    }

    /// <summary>
    /// Выставить id объекту
    /// </summary>
    public void SetID(int _id)
    {
        id = _id;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif //UNITY_EDITOR
    }

    /// <summary>
    /// Загрузить данные о коробке 
    /// </summary>
    public void SetData(InterObjData _intObjData)
    {
        BoxData bData = (BoxData)_intObjData;
        if (bData != null)
        {
            health = bData.health;
        }
    }

    /// <summary>
    /// Сохранить данные о коробке
    /// </summary>
    public InterObjData GetData()
    {
        BoxData bData = new BoxData(id, health,gameObject.name);
        return bData;
    }

    #endregion //id

}
