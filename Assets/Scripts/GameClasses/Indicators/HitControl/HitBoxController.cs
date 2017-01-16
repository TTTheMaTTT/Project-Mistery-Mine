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
    protected HitClass hitData = new HitClass();

    #endregion //fields

    #region parametres

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
        if (hitData!=null?hitData.actTime!=-1:true)
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
    public virtual void SetHitBox(HitClass _hitData)
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
    public virtual void SetHitBox(float _damage, float _actTime, float _hitForce)
    {
        HitCol.Activate(true);
        hitData = new HitClass(_damage, _actTime, _hitForce);
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
        GameObject hero = SpecialFunctions.Player;
        IDamageable target = hero.GetComponent<IDamageable>();
        Rigidbody2D rigid;
        if ((rigid = hero.GetComponent<Rigidbody2D>()) != null && !target.InInvul())
        {
            rigid.AddForce((new Vector2(Mathf.Sign(transform.lossyScale.x), 0f)) * hitData.hitForce);//Атака всегда толкает вперёд
        }
        target.TakeDamage(hitData.damage);
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
