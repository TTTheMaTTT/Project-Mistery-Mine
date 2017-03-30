using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// В этом классе будут храниться все необходимые данные о последнем уровне.
/// </summary>
[XmlType("Game Data")]
[XmlInclude(typeof(LevelStatsData))]
[XmlInclude(typeof(EquipmentInfo))]
[XmlInclude(typeof(CollectionInfo))]
[XmlInclude(typeof(DropData))]
[XmlInclude(typeof(EnemyData))]
[XmlInclude(typeof(NPCData))]
[XmlInclude(typeof(InterObjData))]
[XmlInclude(typeof(QuestInfo))]
[XmlInclude(typeof(StoryInfo))]
[XmlInclude(typeof(GameProgressData))]
public class LevelData
{

    [XmlAttribute("Active")]
    public bool active = false;

    [XmlElement("Checkpoint")]
    public int checkpointNumber = 0;//На каком чекпоинте произошло сохранение

    [XmlElement("Light Intensity")]
    public float lightIntensity = 0f;//Освещение уровня

    [XmlElement("Light HDR")]
    public float lightHDR = 0f;//Сила источников света

    [XmlElement("Level Statistics Data")]
    public LevelStatsData lStatsInfo;

    [XmlElement("Equipment Level Data")]
    public EquipmentInfo eInfo;//Данные об инвентаре персонажа на данном уровне

    [XmlArray("Collections Level Data")]
    [XmlArrayItem("Coolection Level Information")]
    public List<CollectionInfo> cInfo = new List<CollectionInfo>();//Информация о собранных коллекциях на данном уровне

    [XmlElement("Drop Data")]
    public DropData dropInfo;//Данные о дропе, что разбросан по уровню

    [XmlArray("Enemies Info")]
    [XmlArrayItem("Enemy Data")]
    public List<EnemyData> enInfo = new List<EnemyData>();//Информация о монстрах

    [XmlArray("Interactive Objects Info")]
    [XmlArrayItem("Interactive Object Data")]
    public List<InterObjData> intInfo = new List<InterObjData>();//Информация об интерактивных объектах

    [XmlArray("NPCs Data")]
    [XmlArrayItem("NPC Info")]
    public List<NPCData> npcInfo = new List<NPCData>();//Информация об НПС

    [XmlElement("Quests Data")]
    public QuestInfo qInfo;//Информация об активных квестах

    [XmlElement("History Data")]
    public StoryInfo sInfo;//Информация об игровой истории

    [XmlElement("GameStoryData")]
    public GameProgressData progressInfo;//Информация о сюжетном прогрессе

    public LevelData()
    {
    }

    public LevelData(int cNumber, HeroController player, List<ItemCollection> _collection, List<DropClass> drops, History history, GameStatistics gStats,
                                                                                List<EnemyData> _enInfo, List<InterObjData> _intInfo, List<NPCData> _npcInfo)
    {
        active = true;
        checkpointNumber = cNumber;
        qInfo = new QuestInfo(history.ActiveQuests);
        sInfo = new StoryInfo(history);
        lStatsInfo = new LevelStatsData(gStats);
        eInfo = new EquipmentInfo(player.CurrentWeapon, player.Equipment);
        dropInfo = new DropData(drops);
        enInfo = _enInfo;
        intInfo = _intInfo;
        npcInfo = _npcInfo;
        SpriteLightKitImageEffect lightManager = SpecialFunctions.CamController.GetComponent<SpriteLightKitImageEffect>();
        if (lightManager!=null)
        {
            lightIntensity = lightManager.intensity;
            lightHDR = lightManager.HDRRatio;
        }

        cInfo = new List<CollectionInfo>();
        for (int i = 0; i < _collection.Count; i++)
            cInfo.Add(new CollectionInfo(_collection[i]));
        progressInfo = new GameProgressData(gStats.gameHistoryProgress);

    }

}
