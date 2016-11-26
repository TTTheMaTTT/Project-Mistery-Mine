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
[XmlInclude(typeof(NPCData))]
public class InterObjData
{

    [XmlElement("ID")]
    public int objId;

    public InterObjData()
    {
    }

    public InterObjData(int _id)
    {
        objId = _id;
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

    public DoorData(int _id, bool _opened)
    {
        objId = _id;
        opened = _opened;
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

    [XmlElement("Position")]
    public Vector3 position;

    public MechData()
    { }

    public MechData(int _id, bool _activated, Vector3 _position)
    {
        objId = _id;
        activated = _activated;
        position = _position;
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

    public MovPlatformData(int _id, bool _activated, Vector3 _position, int _direction, int _currentPosition)
    {
        objId = _id;
        activated = _activated;
        position = _position;
        direction = _direction;
        currentPosition = _currentPosition;
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

    public BoxData(int _id, float _hp)
    {
        objId = _id;
        health = _hp;
    }
}