using UnityEngine;
using System.Collections;
using System.Xml.Serialization;

/// <summary>
/// Данные, что дают общую информацию о всём ходе игры
/// </summary>
[XmlType("General Game Data")]
[XmlInclude(typeof(EquipmentInfo))]
public class GameGeneralData
{

    [XmlElement("First Checkpoint")]
    public int firstCheckpointNumber = 0;//Если игрок решит переиграть уровень, именно с этого чекпоинта он начнёт игру

    [XmlElement("Equipment Info")]
    public EquipmentInfo eInfo;//Данные об инвентаре персонажа

    public GameGeneralData()
    {
    }

    public GameGeneralData(int cNumb, HeroController player)
    {
        firstCheckpointNumber = cNumb;
        eInfo = new EquipmentInfo(player.CurrentWeapon, player.Bag);
    }

}
