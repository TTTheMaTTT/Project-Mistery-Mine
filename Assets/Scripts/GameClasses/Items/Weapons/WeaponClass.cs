using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, представляющий оружие
/// </summary>
public abstract class WeaponClass : ItemClass
{
    public float damage;
    public float attackTime, preAttackTime, endAttackTime;//Время самой атаки и время подготовки к ней, а также время завершения атаки
    public DamageType attackType = DamageType.Physical;//Каким типом атакует это оружие
    public float effectChance;//Каков шанс произвести особый эффект типа урона?

    public bool chargeable = false;//Можно ли заряжать оружие
    protected float chargeValue = 0f;
    public float ChargeValue { set { chargeValue = value; } }

    /// <summary>
    /// Функция, возвращающая точную копия экземпляра класса оружия, который вызывает этот метод
    /// </summary>
    public virtual WeaponClass GetWeapon()
    {
        return null;
    }

    public WeaponClass(WeaponClass _weapon)
    {
        itemName = _weapon.itemName;
        itemImage = _weapon.itemImage;
        damage = _weapon.damage;
        attackTime = _weapon.attackTime;
        preAttackTime = _weapon.preAttackTime;
        endAttackTime = _weapon.endAttackTime;
        attackType = _weapon.attackType;
        effectChance = _weapon.effectChance;
        chargeValue = 0f;
        chargeable = _weapon.chargeable;
    }

}
