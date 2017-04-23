using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Сериализованные данные о всех баффах, что действовали на персонажа
/// </summary>
[XmlType ("Buff List Data")]
[XmlInclude(typeof(BuffData))]
public class BuffListData
{
    [XmlArray("List of Buff Datas")]
    [XmlArrayItem("Buff Data")]
    public List<BuffData> buffs = new List<BuffData>();

    public BuffListData()
    { }

    public BuffListData(List<BuffClass> _buffs)
    {
        buffs = new List<BuffData>();
        foreach (BuffClass buff in _buffs)
            buffs.Add(new BuffData(buff));
    }


}

/// <summary>
/// Сериализованные данные о баффе
/// </summary>
[XmlType("Buff Data")]
public class BuffData
{

    [XmlElement("Buff Name")]
    public string buffName;

    [XmlElement("Buff Duration")]
    public float buffDuration;//Как долго действует бафф

    [XmlElement("Buff Argument")]
    public int buffArgument;

    [XmlElement("Buff ID")]
    public string buffID;

    public BuffData()
    { }

    public BuffData(string _buffName, float _duration, int _buffArgument = 0, string _buffID = "")
    {
        buffName = _buffName;
        buffDuration = _duration;
        buffArgument = _buffArgument;
        buffID = _buffID;
    }

    public BuffData(BuffClass _buff)
    {
        buffName = _buff.buffName;
        buffDuration = Time.time - _buff.beginTime;
        buffArgument = _buff.argument;
        buffID = _buff.id;
    }
}