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
    /// Совершить атаку
    /// </summary>
    public virtual void Attack(HitBox hitBox, Vector3 position)
    {
        hitBox.SetHitBox(new HitClass(damage, attackTime, attackSize, attackPosition, attackForce));
    }

}