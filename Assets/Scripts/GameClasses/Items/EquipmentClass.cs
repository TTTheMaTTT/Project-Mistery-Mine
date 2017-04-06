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
    public List<TrinketClass> trinkets = new List<TrinketClass>();//Хранилище различных видов тринкетов
    public List<TrinketClass> activeTrinkets = new List<TrinketClass>();//Надетые на персонажа тринкеты

    public EquipmentClass()
    {
        bag = new List<ItemClass>();
        weapons = new List<WeaponClass>();
    }

    /// <summary>
    /// Функция, возвращающая предмет из рюкзака с заданным названием (или возвращает null, если такого предмета нет)
    /// </summary>
    public ItemClass GetItem(string _itemName)
    {
        ItemClass _item = bag.Find(x => x.itemName == _itemName);
        return _item;
    }

}
