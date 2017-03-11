using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Xml.Serialization;

/// <summary>
/// Класс, ответственный за сериализацию и десериализацию
/// </summary>
public class Serializator
{
    /// <summary>
    /// Сериализация и сохранение данных игры
    /// </summary>
    public static void SaveXml(GameData gData, string datapath)
    {
        Type[] extraTypes = { typeof(LevelData),
                              typeof(GameGeneralData),
                              typeof(LevelStatsData),
                              typeof (EnemyData),
                              typeof (InterObjData),
                              typeof(NPCData),
                              typeof (DropData),
                              typeof (DropInfo),
                              typeof(QuestInfo),
                              typeof(CollectionInfo),
                              typeof(EquipmentInfo),
                              typeof(StoryInfo)};

        XmlSerializer serializer = new XmlSerializer(typeof(GameData), extraTypes);
        var encoding = Encoding.GetEncoding("UTF-8");
        using (StreamWriter stream = new StreamWriter(datapath, false, encoding))
        {
            serializer.Serialize(stream, gData);
        }
    }

    /// <summary>
    /// Загрузка игровых данных
    /// </summary>
    public static GameData DeXml(string datapath)
    {
        Type[] extraTypes = { typeof(LevelData),
                              typeof(GameGeneralData),
                              typeof(LevelStatsData),
                              typeof (EnemyData),
                              typeof (InterObjData),
                              typeof(NPCData),
                              typeof (DropData),
                              typeof (DropInfo),
                              typeof(QuestInfo),
                              typeof(CollectionInfo),
                              typeof(EquipmentInfo),
                              typeof(StoryInfo)};

        XmlSerializer serializer = new XmlSerializer(typeof(GameData), extraTypes);

        FileStream fs = new FileStream(datapath, FileMode.Open);
        GameData gData = (GameData)serializer.Deserialize(fs);
        fs.Close();
        return gData;
    }

    /// <summary>
    /// Сериализация и сохранение данных о сохранениях
    /// </summary>
    public static void SaveXmlSavesInfo(SavesInfo _savesInfo, string datapath)
    {
        Type[] extraTypes = { typeof(SaveInfo) };
        XmlSerializer serializer = new XmlSerializer(typeof(SavesInfo), extraTypes);
        var encoding = Encoding.GetEncoding("UTF-8");
        using (StreamWriter stream = new StreamWriter(datapath, false, encoding))
        {
            serializer.Serialize(stream, _savesInfo);
        }
    }

    /// <summary>
    /// Загрузка данных о сохранениях
    /// </summary>
    public static SavesInfo DeXmlSavesInfo(string datapath)
    {
        Type[] extraTypes = { typeof(SaveInfo) };
        XmlSerializer serializer = new XmlSerializer(typeof(SavesInfo), extraTypes);

        FileStream fs = new FileStream(datapath, FileMode.Open);
        SavesInfo _savesInfo = (SavesInfo)serializer.Deserialize(fs);
        fs.Close();
        return _savesInfo;
    }

    public static bool HasSavesInfo(string datapath)
    {
        FileInfo fInfo = new FileInfo(datapath);
        return (fInfo != null);
    }

}
