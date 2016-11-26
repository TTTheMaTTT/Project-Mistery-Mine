using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Информация об инвентаре персонажа
/// </summary>
[XmlType("Equipment Info")]
public class EquipmentInfo
{
    [XmlElement("Weapon")]
    public string weapon;

    [XmlArray("Bag Items")]
    [XmlArrayItem("Item")]
    public List<string> bagItems = new List<string>();

    public EquipmentInfo()
    { }

    public EquipmentInfo (WeaponClass _weapon, List<ItemClass> _items)
    {
        weapon = _weapon != null ? _weapon.itemName : string.Empty;

        bagItems = new List<string>();

        foreach (ItemClass _item in _items)
            bagItems.Add(_item != null ? _item.itemName : string.Empty);
    }
}