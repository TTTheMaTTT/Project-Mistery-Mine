using UnityEngine;
using System.Collections;

/// <summary>
/// Класс оружия ближнего боя
/// </summary>
public class SwordClass : WeaponClass
{
    public Vector2 attackPosition, attackSize;
    public float attackForce;

    /// <summary>
    /// Функция, что возвращает новый экземпляр класса, который имеет те же данные, что и экземпляр, выполняющий этот метод
    /// </summary>
    public override WeaponClass GetWeapon()
    {
        return new SwordClass(this);
    }

    public SwordClass(SwordClass _sword) : base(_sword)
    {
        attackPosition = _sword.attackPosition;
        attackSize = _sword.attackSize;
        attackForce = _sword.attackForce;
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    public virtual void Attack(HitBoxController hitBox, Vector3 position)
    {
        hitBox.SetHitBox(new HitParametres(damage, attackTime, attackSize, attackPosition, attackForce, attackType, effectChance));
        chargeValue = 0f;
    }

}