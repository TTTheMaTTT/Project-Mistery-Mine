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

    protected AttackerClass attackerInfo = null;//Какой объект атакует данным хитбоксом
    public virtual AttackerClass AttackerInfo { get { return attackerInfo; } set { attackerInfo = value; } }

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
    public virtual void SetHitBox(float _damage, float _actTime, float _hitForce, DamageType _dType=DamageType.Physical, float _eChance=0f, int _attackPower=1)
    {
        HitCol.Activate(true);
        hitData = new HitParametres(_damage, _actTime, _hitForce, _dType, _eChance,_attackPower);
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
        CharacterController _char = null;
        if ((_char = obj.GetComponent<CharacterController>()) != null)
            _char.TakeAttackerInformation(attackerInfo);
        target.TakeDamage(hitData);
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

/// <summary>
/// Информация об атакующем
/// </summary>
public class AttackerClass
{
    public GameObject attacker;//Что ялвялось атакующим объектом?
    public AttackTypeEnum attackType;//Какой тип атаки?

    public AttackerClass(GameObject _attacker, AttackTypeEnum _attackType)
    {
        attacker = _attacker;
        attackType = _attackType;
    }

}