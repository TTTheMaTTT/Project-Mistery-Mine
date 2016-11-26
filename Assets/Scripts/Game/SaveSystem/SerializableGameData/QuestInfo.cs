using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Класс, в котором содержится информация о состоянии журнала
/// </summary>
[XmlType("Quest Info")]
public class QuestInfo
{

    [XmlArray("Quests")]
    [XmlArrayItem("Quest")]
    public List<string> quests = new List<string>();

    public QuestInfo()
    { }

    public QuestInfo(List<Quest> _quests)
    {
        quests = new List<string>();
        foreach (Quest quest in _quests)
        {
            quests.Add(quest.questName);
        }
    }

}
