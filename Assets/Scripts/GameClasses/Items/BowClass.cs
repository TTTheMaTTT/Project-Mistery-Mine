using UnityEngine;
using System.Collections;

/// <summary>
/// Класс лука
/// </summary>
public class BowClass : WeaponClass
{
    public GameObject arrow;//Чем выстреливаем
    public float shootDistance;//Дальность выстрела
    public float shootRate;//Скорострельность, а точнее время, между выстрелами
}