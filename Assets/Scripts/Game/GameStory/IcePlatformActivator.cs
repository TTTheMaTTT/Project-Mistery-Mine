using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Объект, при уничтожении которого будет активированы механизмы, которые находятся в заледеневшем состоянии из-за этого объекта
/// Лёд, который не позволяет объектам работать
/// </summary>
public class IcePlatformActivator : MonoBehaviour, IDamageable
{

    #region fields

    [SerializeField]protected List<GameObject> mechanisms;

    #endregion //fields

    #region parametres

    [SerializeField]
    protected float maxHealth = 10f;
    [SerializeField]
    protected float health = 10f;

    [SerializeField]
    [HideInInspector]
    int id;

    #endregion //parametres

    protected virtual void Awake()
    {
        Initialize();
    }

    protected void Initialize()
    {
        health = maxHealth;
    }

    #region IDamageable

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public void TakeDamage(HitParametres hitData)
    {
        if (hitData.damageType != DamageType.Fire)
            return;
        health -= hitData.damage;
        //StopAllCoroutines();
        if (health <= 0f)
            Destroy();
        //else
            //StartCoroutine(HitProcess());
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
        if (hitData.damageType != DamageType.Fire)
            return;
        health -= hitData.damage;
        //StopAllCoroutines();
        if (health <= 0f)
            Destroy();
        //else
            //StartCoroutine(HitProcess());
    }

    /// <summary>
    /// Подвергнуться эффекту, связанному с типом урона
    /// </summary>
    /// <param name="_dType">Тип урона</param>
    public void TakeDamageEffect(DamageType _dType)
    { }

    public bool InInvul()
    {
        return false;
    }

    /// <summary>
    /// Что произойдёт, когда объект будет уничтожен - лёд расстает, заледеневшие механизмы снова заработают
    /// </summary>
    public void Destroy()
    {
        foreach (GameObject mechObj in mechanisms)
        {
            IMechanism mech= mechObj.GetComponent<IMechanism>();
            if (mech != null)
                mech.ActivateMechanism();
        }
        Destroy(gameObject);
        //StartCoroutine(DestroyProcess());
    }

    /// <summary>
    /// Узнать текущее здоровье
    /// </summary>
    public float GetHealth()
    {
        return health;
    }

    /*protected virtual IEnumerator HitProcess()
    {
        if (anim != null)
        {
            anim.Play("TakeDamage");
            yield return new WaitForSeconds(.1f);
            if (anim != null)
                anim.Play("Idle");
        }
    }

    protected virtual IEnumerator DestroyProcess()
    {
        Destroy(anim);
        Destroy(GetComponent<SpriteRenderer>());
        Destroy(GetComponent<Collider2D>());
        content = new List<GameObject>();
        yield return new WaitForSeconds(.6f);
        Destroy(gameObject);
        Destroy(this);
    }*/

    #endregion //IDamageable

    #region IMechanism

    /// <summary>
    /// Активировать механизм коробки, что разрушит её
    /// </summary>
    public virtual void ActivateMechanism()
    {
        Destroy();
    }

    #endregion //IMechanism

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
    }

    /// <summary>
    /// Сохранить данные о коробке
    /// </summary>
    public InterObjData GetData()
    {
        InterObjData _data = new InterObjData(id,gameObject.name, transform.position);
        return _data;
    }

    #endregion //id
}
