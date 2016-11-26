using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Информация о всех дропах на уровне
/// </summary>
[XmlType("Drop Data")]
[XmlInclude(typeof(DropInfo))]
public class DropData
{
    [XmlArray("Drops")]
    [XmlArrayItem("Drop Info")]
    public List<DropInfo> drops = new List<DropInfo>();

    public DropData()
    {
    }

    public DropData(List<DropClass> _drops)
    {
        drops = new List<DropInfo>();
        foreach (DropClass _drop in _drops)
            drops.Add(new DropInfo(_drop));
    }
     
}

/// <summary>
/// Информация о дропе
/// </summary>
[XmlType("Drop Info")]
public class DropInfo
{
    [XmlElement("Position")]
    public Vector3 position;

    [XmlElement("Item Name")]
    public string itemName;

    [XmlAttribute("Custom Drop")]
    public bool customDrop = false;//Если дроп представляет собой мешочек с каким-то предметом, то это поле будет true

    public DropInfo()
    { }

    public DropInfo(DropClass drop)
    {
        position = drop.transform.position;
        itemName = drop.item!=null?drop.item.itemName:string.Empty;
        if (drop.gameObject.name.Contains("ItemDrop"))
            customDrop = true;
    }
}