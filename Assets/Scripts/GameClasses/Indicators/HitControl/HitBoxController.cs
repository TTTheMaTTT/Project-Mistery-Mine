using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Хитбокс, который работает не посредством коллайдера, а используя специальный компонент HitBoxCollider. Используется монстрами, которые могут атаковать только главного героя.
/// </summary>
public class HitBoxController : MonoBehaviour
{

    #region fields

    private HitBoxCollider hitCol;//Атакующий элемент
    private HitBoxCollider HitCol { get { if (hitCol == null) hitCol=GetComponent<HitBoxCollider>(); return hitCol; } set { hitCol = value; } }
    protected HitParametres hitData = HitParametres.zero;

    protected GameObject attacker = null;//Какой объект атакует данным хитбоксом
    public virtual GameObject Attacker { get { return attacker; } set { attacker = value; } }

    #endregion //fields

    #region parametres

    private string enemyLayer = "hero";//Какой слой игровых оъектов подвергается атаке
    public virtual bool allyHitBox { get { return HitCol.allyHitBox; } set { HitCol.allyHitBox = value; } }

    protected bool immobile;//Запрет на перемещение хитбокса
    public bool Immobile { set { immobile = value; } }

    #endregion //parametres

    #region eventHandlers

    public EventHandler<HitEventArgs> AttackEventHandler;//Хэндлер события "Произошла атака"

    #endregion //eventHandlers

    public virtual void Awake()
    {
        hitCol = GetComponent<HitBoxCollider>();
        hitCol.AttackEventHandler += HandleAttackProcess;
        hitCol.Immobile = immobile;
        if (hitData.actTime!=-1)
            hitCol.Activate(false);
    }

    /// <summary>
    /// Сбросить настройки хитбокса и перестать атаковать им
    /// </summary>
    public virtual void ResetHitBox()
    {
        StopAllCoroutines();
        HitCol.Activate(false);
    }

    /// <summary>
    /// Настройка ХитБокса
    /// </summary>
    public virtual void SetHitBox(HitParametres _hitData)
    {
        hitData = _hitData;
        HitCol.Activate(true);
        if (!immobile)
            hitCol.position = hitData.hitPosition;
        hitCol.size = hitData.hitSize;
        if (hitData.actTime != -1f)
        {
            hitCol.AlwaysAttack = false;
            StartCoroutine(HitProcess(hitData.actTime));
        }
        else
            hitCol.AlwaysAttack = true;
    }

    /// <summary>
    /// Настройка хитбокса (без изменения положения и размера хитбокса)
    /// </summary>
    /// <param name="_damage">Наносимый урон</param>
    /// <param name="_actTime">Время действия (если -1, то всегда действует)</param>
    /// <param name="_hitForce">Сила отталкивания, действующая на цель</param>
    /// <param name="_dType">Тип наносимого урона</param>
    public virtual void SetHitBox(float _damage, float _actTime, float _hitForce, DamageType _dType=DamageType.Physical, float _eChance=0f)
    {
        HitCol.Activate(true);
        hitData = new HitParametres(_damage, _actTime, _hitForce, _dType, _eChance);
        if (hitData.actTime != -1f)
        {
            hitCol.AlwaysAttack = false;
            StartCoroutine(HitProcess(hitData.actTime));
        }
        else
            hitCol.AlwaysAttack = true;
    }

    /// <summary>
    /// Процесс нанесения урона
    /// </summary>
    protected virtual IEnumerator HitProcess(float hitTime)
    {
        yield return new WaitForSeconds(hitTime);
        hitCol.Activate(false);
    }

    /// <summary>
    /// Установить, по каким целям будет попадать хитбокс
    /// </summary>
    public virtual void SetEnemies(List<string> _enemies)
    {
    }

    #region eventHandlers

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    private void HandleAttackProcess(object sender, HitEventArgs e)
    {
        float prevHP = e.HPDif;
        GameObject obj = e.Target;
        if (obj == null)
            return;
        IDamageable target = obj.GetComponent<IDamageable>();
        Rigidbody2D rigid;
        if ((rigid = obj.GetComponent<Rigidbody2D>()) != null && !target.InInvul())
        {
            rigid.AddForce((new Vector2(Mathf.Sign(transform.lossyScale.x), 0f)) * hitData.hitForce);//Атака всегда толкает вперёд
        }
        if ((hitData.damageType != DamageType.Physical && !target.InInvul()) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
            target.TakeDamageEffect(hitData.damageType);
        AIController ai = null;
        if ((ai = obj.GetComponent<AIController>()) != null)
            ai.TakeAttackerInformation(attacker);
        target.TakeDamage(hitData.damage, hitData.damageType, hitData.attackPower);
        OnAttack(new HitEventArgs(target.GetHealth() - prevHP));

    }

    #endregion //eventHandlers


    #region events

    /// <summary>
    /// Событие, вызываемое при совершении атаки
    /// </summary>
    public virtual void OnAttack(HitEventArgs e)
    {
        EventHandler<HitEventArgs> handler = AttackEventHandler;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

}
