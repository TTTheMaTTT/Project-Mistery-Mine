using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, представляющий инвентарь персонажа (а именно, главного героя)
/// </summary>
[System.Serializable]
public class EquipmentClass
{
    public List<ItemClass> bag=new List<ItemClass>();//Сумка с различными предметами
    public List<WeaponClass> weapons=new List<WeaponClass>();//Хранилище различных видов оружия

    public EquipmentClass()
    {
        bag = new List<ItemClass>();
        weapons = new List<WeaponClass>();
    }

}
