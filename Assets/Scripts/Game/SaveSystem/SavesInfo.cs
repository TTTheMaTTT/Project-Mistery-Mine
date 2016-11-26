using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Объект, что хранит в себе информацию о всех сделанных игроком сохранениях.
/// </summary>
[XmlType("SavesInfo")]
[XmlInclude(typeof(SaveInfo))]
public class SavesInfo 
{
    [XmlArray("Saves")]
    [XmlArrayItem("SaveInfo")]
    public List<SaveInfo> saves = new List<SaveInfo>();

    [XmlElement("CurrentProfileNumb")]
    public int currentProfileNumb = 0;

    public SavesInfo()
    {
    }

    public SavesInfo(int count)
    {
        saves = new List<SaveInfo>();
        for (int i = 0; i < count; i++)
        {
            saves.Add(new SaveInfo(string.Empty,string.Empty,false, "cave_lvl1"));
        }
        currentProfileNumb = 0;
    }

}

/// <summary>
/// Класс, в котором хранится информация об одном сохранении
/// </summary>
[XmlType ("SaveInformation")]
public class SaveInfo
{
    [XmlElement("SaveName")]
    public string saveName;

    [XmlElement("SaveTime")]
    public string saveTime;

    [XmlAttribute("HasData")]
    public bool hasData;//Имеются ли данные сохранённой игры в соответствующем сохранении?

    [XmlElement("LoadSceneName")]
    public string loadSceneName;

    public SaveInfo()
    {
    }

    public SaveInfo(string sName, string sTime, bool sHasData, string sLoadSceneName)
    {
        saveName = sName;
        saveTime = sTime;
        hasData = sHasData;
        loadSceneName = sLoadSceneName;
    }
}