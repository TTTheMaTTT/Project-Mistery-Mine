using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Информация о монстрах - врагах ГГ
/// </summary>
[XmlType("Enemy Data")]
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

    [XmlArray("Waypoints")]
    [XmlArrayItem("Waypoint")]
    public List<Vector2> waypoints = new List<Vector2>();

    public EnemyData()
    { }

    public EnemyData(AIController _ai)
    {
        objId = _ai.ID;
        position = _ai.transform.position;
        orientation = Mathf.RoundToInt(Mathf.Sign(_ai.transform.lossyScale.x));

        behavior = _ai.Behavior.ToString();

        List<NavigationCell> _waypoints = _ai.GetWaypoints();
        if (_waypoints != null ? waypoints.Count > 0 : false)
            waypoints = _waypoints.ConvertAll<Vector2>(x => x.cellPosition);
        else
            waypoints = new List<Vector2>();
        health = _ai.Health;
    }
}