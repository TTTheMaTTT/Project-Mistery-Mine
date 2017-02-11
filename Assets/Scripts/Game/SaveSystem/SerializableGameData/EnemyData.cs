using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Информация о монстрах - врагах ГГ
/// </summary>
[XmlType("Enemy Data")]
[XmlInclude(typeof(TargetData))]
[XmlInclude(typeof(BuffListData))]
[XmlInclude(typeof(SpiderData))]
[XmlInclude(typeof(HumanoidData))]
[XmlInclude(typeof(WormData))]
public class EnemyData
{

    [XmlElement("ID")]
    public int objId;//Номер, по которому можно отличить монстра

    [XmlElement("Position")]
    public Vector3 position;//Где находится монстр

    [XmlElement("Orientation")]
    public int orientation;//В какую сторону повёрнут монстр

    [XmlElement("Health")]
    public float health;//Здоровье

    [XmlElement("Behaviour")]
    public string behavior;//Какую модель поведения реализует монстр в данный момент

    [XmlElement("Main Target Data")]
    public TargetData mainTargetData;//Информация о главной цели ИИ

    [XmlElement("Current Target Data")]
    public TargetData currentTargetData;//Информация о текущей цели ИИ

    [XmlArray("Waypoints")]
    [XmlArrayItem("Waypoint")]
    public List<Vector2> waypoints = new List<Vector2>();

    [XmlElement("Buff List Data")]
    public BuffListData bListData = new BuffListData();

    public EnemyData()
    { }

    public EnemyData(AIController _ai)
    {
        objId = _ai.ID;
        position = _ai.transform.position;
        orientation = Mathf.RoundToInt(Mathf.Sign(_ai.transform.lossyScale.x));

        behavior = _ai.Behavior.ToString();

        List<NavigationCell> _waypoints = _ai.GetWaypoints();
        if (_waypoints != null ? _waypoints.Count > 0 : false)
            waypoints = _waypoints.ConvertAll<Vector2>(x => x.cellPosition);
        else
            waypoints = new List<Vector2>();
        mainTargetData = new TargetData(_ai.MainTarget);
        currentTargetData = new TargetData(_ai.CurrentTarget);

        bListData = new BuffListData(_ai.Buffs);

        health = _ai.Health;
    }
}

/// <summary>
/// Информация о пауке 
/// </summary>
[XmlType("Spider Data")]
public class SpiderData : EnemyData
{
    [XmlElement("Spider Orientation")]
    public Vector2 spiderOrientation = Vector2.zero;

    public SpiderData()
    { }

    public SpiderData(SpiderController _ai)
    {
        objId = _ai.ID;
        position = _ai.transform.position;
        spiderOrientation = _ai.GetSpiderOrientation();
        orientation = Mathf.RoundToInt(Mathf.Sign(_ai.transform.lossyScale.x));

        behavior = _ai.Behavior.ToString();

        List<NavigationCell> _waypoints = _ai.GetWaypoints();
        if (_waypoints != null ? _waypoints.Count > 0 : false)
            waypoints = _waypoints.ConvertAll<Vector2>(x => x.cellPosition);
        else
            waypoints = new List<Vector2>();
        mainTargetData = new TargetData(_ai.MainTarget);
        currentTargetData = new TargetData(_ai.CurrentTarget);

        health = _ai.Health;
        bListData = new BuffListData(_ai.Buffs);
    }

}

/// <summary>
/// Информация о гуманоиде
/// </summary>
[XmlType("Humanoid Data")]
public class HumanoidData: EnemyData
{

    [XmlAttribute("On Ladder")]
    public bool onLadder = false;//Находится ли персонаж на лестнице?

    [XmlElement("Platform ID")]
    public int platformId = -1;//Id платформы, на которой находится персонаж. Если он не на платформе, то равен -1

    public HumanoidData()
    { }

    public HumanoidData(HumanoidController _ai)
    {
        objId = _ai.ID;
        position = _ai.transform.position;
        orientation = Mathf.RoundToInt(Mathf.Sign(_ai.transform.lossyScale.x));

        behavior = _ai.Behavior.ToString();

        List<NavigationCell> _waypoints = _ai.GetWaypoints();
        if (_waypoints != null ? _waypoints.Count > 0 : false)
            waypoints = _waypoints.ConvertAll<Vector2>(x => x.cellPosition);
        else
            waypoints = new List<Vector2>();
        mainTargetData = new TargetData(_ai.MainTarget);
        currentTargetData = new TargetData(_ai.CurrentTarget);

        onLadder = _ai.OnLadder;
        ETarget platformTarget = _ai.PlatformTarget;
        platformId = platformTarget.exists ? platformTarget.transform.GetComponent<MovingPlatform>().id : -1;

        bListData = new BuffListData(_ai.Buffs);

        health = _ai.Health;
    }

}

/// <summary>
/// Информация о черве
/// </summary>
[XmlType("Worm Data")]
public class WormData : EnemyData
{

    [XmlElement("Worm State")]
    public string wormState = "usual";

    public WormData()
    { }

    public WormData(WormController _ai)
    {
        objId = _ai.ID;
        position = _ai.transform.position;
        orientation = Mathf.RoundToInt(Mathf.Sign(_ai.transform.lossyScale.x));

        behavior = _ai.Behavior.ToString();

        List<NavigationCell> _waypoints = _ai.GetWaypoints();
        if (_waypoints != null ? _waypoints.Count > 0 : false)
            waypoints = _waypoints.ConvertAll<Vector2>(x => x.cellPosition);
        else
            waypoints = new List<Vector2>();
        mainTargetData = new TargetData(_ai.MainTarget);
        currentTargetData = new TargetData(_ai.CurrentTarget);

        wormState = _ai.wState == WormStateEnum.undergroundState ? "underground" :
                        _ai.wState == WormStateEnum.usualState ? "usual" : "up";
        bListData = new BuffListData(_ai.Buffs);

        health = _ai.Health;
    }

}

/// <summary>
/// Информация о цели ИИ
/// </summary>
[XmlType("Target Data")]
public class TargetData
{

    [XmlAttribute("Exists")]
    public bool exists = false;

    [XmlElement("Target Position")]
    public Vector2 position = Vector2.zero;

    [XmlElement("Target Name")]
    public string targetName = string.Empty;

    public TargetData()
    { }

    public TargetData(ETarget _target)
    {
        exists = _target.exists;
        position = _target;
        targetName = _target.transform != null ? _target.transform.gameObject.name : string.Empty;
    }

}