﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Информаци об НПС и его репликах
/// </summary>
[XmlType("NPC Data")]
public class NPCData: InterObjData 
{
    [XmlArray("Dialogs")]
    [XmlArrayItem("Dialog")]
    public List<string> dialogs = new List<string>();

    public NPCData()
    {
    }

    public NPCData(int _id, List<Dialog> _dialogs, string _name, Vector3 _position)
    {
        objName = _name.Substring(0, _name.Contains("(") ? _name.IndexOf("(") : _name.Length);
        objId = _id;
        dialogs = new List<string>();
        for (int i = 0; i < _dialogs.Count; i++)
            dialogs.Add(_dialogs[i].dialogName);
        position = _position;
    }

}
