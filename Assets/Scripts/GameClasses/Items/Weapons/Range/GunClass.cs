using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс стрелкового оружия, типа пистолета
/// </summary>
public class GunClass : BowClass
{

    #region parametres

    protected static List<string> enemies = new List<string> { "enemy", "box", "destroyable", "boss" };

    public float arrowSpeed;//Скорость снаряда после выстрела
    public float attackForce;//Сила отталкивания снаряда
    #endregion //parametres

    /// <summary>
    /// Функция, что возвращает новый экземпляр класса, который имеет те же данные, что и экземпляр, выполняющий этот метод
    /// </summary>
    public override WeaponClass GetWeapon()
    {
        return new GunClass(this);
    }

    public GunClass(GunClass _bow): base(_bow)
    {
        arrowSpeed = _bow.arrowSpeed;
    }

    /// <summary>
    /// Функция выстрела из оружия
    /// </summary>
    public override void Shoot(HitBoxController hitBox, Vector3 position, int orientation, LayerMask whatIsAim, List<string> enemies)
    {
        Vector2 shootPosition = (Vector2)position + Vector2.up * shootOffset;
        hitBox.StartCoroutine(DontShootProcess());
        GameObject newMissile = Instantiate(arrow, shootPosition, Quaternion.identity) as GameObject;
        Rigidbody2D missileRigid = newMissile.GetComponent<Rigidbody2D>();
        missileRigid.velocity = Vector2.right*orientation* arrowSpeed;
        HitBoxController missileHitBox = missileRigid.GetComponentInChildren<HitBoxController>();
        if (missileHitBox != null)
        {
            missileHitBox.SetEnemies(enemies);
            missileHitBox.SetHitBox(new HitParametres(damage,-1f, attackForce, attackType, effectChance,attackPower));
            missileHitBox.heroHitBox = true;
            missileHitBox.AttackerInfo = new AttackerClass(hitBox.transform.parent.gameObject, AttackTypeEnum.range);
        }
        SpecialFunctions.camControl.PushCamera(orientation * Vector2.left * .015f);
    }



}
