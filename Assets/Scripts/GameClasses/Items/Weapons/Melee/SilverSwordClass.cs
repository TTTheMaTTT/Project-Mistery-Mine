using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, описывающий работу серебрянного меча
/// </summary>
public class SilverSwordClass : SwordClass
{

    #region fields

    protected HitBoxController hBox;

    #endregion //fields

    #region parametres

    public static List<string> swordVictims = new List<string> (){ "Ghost", "Lich" };//Персонажи, по которым этот меч наносит доп урон (игровые объекты должны содержать в своём имени одно из этих слов
    public float additionalGhostDamage;//Дополнительный урон, который наносит этот меч по призракам

    #endregion //parametres

    /// <summary>
    /// Функция, что возвращает новый экземпляр класса, который имеет те же данные, что и экземпляр, выполняющий этот метод
    /// </summary>
    public override WeaponClass GetWeapon()
    {
        return new SilverSwordClass(this);
    }

    public SilverSwordClass(SilverSwordClass _sword) : base(_sword)
    {
        additionalGhostDamage = _sword.additionalGhostDamage;
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    public override void Attack(HitBoxController hitBox, Vector3 position)
    {
        hBox = hitBox;
        hBox.AttackEventHandler += HandleAttackEvent;
        hitBox.SetHitBox(new HitParametres(damage, attackTime, attackSize, attackPosition, attackForce, attackType, effectChance, attackPower));
        chargeValue = 0f;
    }

    public override void StopAttack()
    {
        if (hBox!=null)
            hBox.AttackEventHandler -= HandleAttackEvent;
    }

    /// <summary>
    /// Обработать событие "Был нанесён урон"
    /// </summary>
    void HandleAttackEvent(object sender, HitEventArgs e)
    {
        foreach (string _name in swordVictims)
            if (e.Target.name.Contains(_name))
            {
                e.Target.GetComponent<IDamageable>().TakeDamage(new HitParametres(additionalGhostDamage, DamageType.Physical,5));
                break;
            }
    }

}
