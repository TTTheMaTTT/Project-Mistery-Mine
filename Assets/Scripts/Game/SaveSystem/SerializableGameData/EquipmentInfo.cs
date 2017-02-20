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

    [XmlArray("Weapons")]
    [XmlArrayItem("Available Weapon")]
    public List<string> weapons = new List<string>();

    public EquipmentInfo()
    { }

    public EquipmentInfo (WeaponClass _weapon, EquipmentClass _equip)
    {
        weapon = _weapon != null ? _weapon.itemName : string.Empty;

        bagItems = new List<string>();
        weapons = new List<string>();

        foreach (ItemClass _item in _equip.bag)
            bagItems.Add(_item != null ? _item.itemName : string.Empty);
        foreach (ItemClass _item in _equip.weapons)
            weapons.Add(_item != null ? _item.itemName : string.Empty);
    }
}