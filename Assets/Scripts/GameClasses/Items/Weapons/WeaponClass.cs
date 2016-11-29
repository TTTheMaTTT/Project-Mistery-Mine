using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, представляющий оружие
/// </summary>
public abstract class WeaponClass : ItemClass
{
    public float damage;
    public float attackTime, preAttackTime;//Время самой атаки и время подготовки к ней

}
