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

    [XmlAttribute("Agressive")]
    public bool agressive;//Находится ли монстр в агрессивном состоянии?

    public EnemyData()
    { }

    public EnemyData(AIController _ai)
    {
        objId = _ai.ID;
        position = _ai.transform.position;
        orientation = Mathf.RoundToInt(Mathf.Sign(_ai.transform.lossyScale.x));

        health = _ai.Health;
        agressive = _ai.Agressive;
    }
}