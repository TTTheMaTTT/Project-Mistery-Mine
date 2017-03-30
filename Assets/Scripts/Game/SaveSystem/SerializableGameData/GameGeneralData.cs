using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Данные, что дают общую информацию о всём ходе игры
/// </summary>
[XmlType("General Game Data")]
[XmlInclude(typeof(EquipmentInfo))]
[XmlInclude(typeof(CollectionInfo))]
[XmlInclude(typeof(GameProgressData))]
public class GameGeneralData
{

    [XmlElement("First Checkpoint")]
    public int firstCheckpointNumber = 0;//Если игрок решит переиграть уровень, именно с этого чекпоинта он начнёт игру

    [XmlElement("Equipment Info")]
    public EquipmentInfo eInfo;//Данные об инвентаре персонажа

    [XmlArray("Collections Info")]
    [XmlArrayItem("Information About Collection")]
    public List<CollectionInfo> cInfo = new List<CollectionInfo>();//Данные о собранных коллекциях

    public GameProgressData progressInfo;//Данные о прогрессе игры

    public GameGeneralData()
    {
    }

    public GameGeneralData(int cNumb, HeroController player, List<ItemCollection> _collections)
    {
        firstCheckpointNumber = cNumb;
        eInfo = new EquipmentInfo(player.CurrentWeapon, player.Equipment);

        cInfo = new List<CollectionInfo>();
        for (int i = 0; i < _collections.Count; i++)
            cInfo.Add(new CollectionInfo(_collections[i]));
        GameStatistics gStats = SpecialFunctions.statistics;
        if (gStats!=null)
            progressInfo = new GameProgressData(gStats.gameHistoryProgress);

    }

}
