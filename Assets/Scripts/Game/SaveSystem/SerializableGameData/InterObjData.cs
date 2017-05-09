using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Необходимая информация об интерактивном объекте
/// </summary>
[XmlType("InterObjData")]
[XmlInclude(typeof(DoorData))]
[XmlInclude(typeof(MechData))]
[XmlInclude(typeof(MovPlatformData))]
[XmlInclude(typeof(BoxData))]
[XmlInclude(typeof(AlchemyLabData))]
[XmlInclude(typeof(NPCData))]
[XmlInclude(typeof(SpiderSpyData))]
public class InterObjData
{

    [XmlElement("ObjectName")]
    public string objName;

    [XmlElement("ID")]
    public int objId;

    [XmlElement("Position")]
    public Vector3 position=Vector3.zero;

    public InterObjData()
    {
    }

    public InterObjData(int _id, string _name, Vector3 _position)
    {
        objId = _id;
        objName = _name.Substring(0, _name.Contains("(") ? _name.IndexOf("(") : _name.Length);
        position = _position;
    }

}

/// <summary>
/// Класс, который нужен для гипотетического сохранения единственного за всю игру объекта в своём роде - паука-лазутчика
/// </summary>
[XmlType("SpiderSpyData")]
public class SpiderSpyData : InterObjData
{
    [XmlAttribute("Activated")]
    public bool activated = false;

    public SpiderSpyData()
    { }

    public SpiderSpyData(int _id, string _name, bool _activated)
    {
        objId = _id;
        objName = _name.Substring(0, _name.Contains("(") ? _name.IndexOf("(") : _name.Length);
        activated = _activated;
    }

}

/// <summary>
/// Информация о двери
/// </summary>
[XmlType("Door Data")]
public class DoorData : InterObjData
{
    [XmlAttribute("Opened")]
    public bool opened=false;
     
    public DoorData()
    { }

    public DoorData(int _id, bool _opened, string _name)
    {
        objId = _id;
        opened = _opened;
        objName = _name.Substring(0, _name.Contains("(") ? _name.IndexOf("(") : _name.Length);
    }

}

/// <summary>
/// Информация о механизме (а также о рычагах)
/// </summary>
[XmlType("Mechanism Data")]
[XmlInclude(typeof(MovPlatformData))]
public class MechData : InterObjData
{
    [XmlElement("Activated")]
    public bool activated;

    public MechData()
    { }

    public MechData(int _id, bool _activated, Vector3 _position, string _name)
    {
        objId = _id;
        activated = _activated;
        position = _position;
        objName = _name.Substring(0, _name.Contains("(") ? _name.IndexOf("(") : _name.Length);
    }

}

/// <summary>
/// Информация о движущейся платформе
/// </summary>
[XmlType("Moving Platform Data")]
public class MovPlatformData: MechData
{
    [XmlElement("Direction")]
    public int direction;

    [XmlElement("Current Position")]
    public int currentPosition;

    public MovPlatformData()
    {
    }

    public MovPlatformData(int _id, bool _activated, Vector3 _position, int _direction, int _currentPosition, string _name)
    {
        objId = _id;
        activated = _activated;
        position = _position;
        direction = _direction;
        currentPosition = _currentPosition;
        objName = _name.Substring(0, _name.Contains("(") ? _name.IndexOf("(") : _name.Length);
    }

}

/// <summary>
/// Информация о головоломке
/// </summary>
[XmlType("Riddle Data")]
public class RiddleData : MechData
{

    [XmlElement("Progress")]
    public int progress;

    public RiddleData()
    {
    }

    public RiddleData(int _id, bool _activated, int _progress)
    {
        objId = _id;
        activated = _activated;
        progress = _progress;
    }

}

/// <summary>
/// Информация о ящике
/// </summary>
[XmlType("Box Data")]
public class BoxData : InterObjData
{
    [XmlElement("Health")]
    public float health;

    public BoxData()
    {
    }

    public BoxData(int _id,Vector3 _position, float _hp, string _name)
    {
        objId = _id;
        position = _position;
        health = _hp;
        objName = _name.Substring(0, _name.Contains("(") ? _name.IndexOf("(") : _name.Length);
    }
}

/// <summary>
/// Информация об алхимическом столе
/// </summary>
[XmlType("Alchemy Lab Data")]
public class AlchemyLabData : InterObjData
{
    [XmlArray("Usage List")]
    [XmlArrayItem("Usage Data")]
    public List<bool> usageList=new List<bool>();//Данные о том, какие зелья уже были использованы игроком

    public AlchemyLabData()
    {
    }

    public AlchemyLabData(int _id, string _name, List<PotionClass> potions)
    {
        objId = _id;
        objName = _name.Substring(0, _name.Contains("(") ? _name.IndexOf("(") : _name.Length);
        usageList = new List<bool>();
        foreach (PotionClass potion in potions)
            usageList.Add(potion.haveUsed);
    }
}