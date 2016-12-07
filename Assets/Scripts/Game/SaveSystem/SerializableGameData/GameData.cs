using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// В этом классе будут храниться все необходимые данные игры. Экземпляр именно этого класса храниться в сохранениях.
/// </summary>
[XmlType("Game Data")]
[XmlInclude(typeof(GameGeneralData))]
[XmlInclude(typeof(LevelData))]
public class GameData
{

    [XmlElement("Level Data")]
    public LevelData lData;

    [XmlElement("General Game Data")]
    public GameGeneralData gGData;

    public GameData()
    {
        lData = new LevelData();
        gGData = new GameGeneralData();
    }

    /// <summary>
    /// Задать данные уровня
    /// </summary>
    public void SetLevelData(int cNumber,HeroController player, List<ItemCollection> _collection, List<DropClass> drops, History history,
                                                                GameStatistics gStats, List<EnemyData> enemyList, List<InterObjData> intObjList, List<NPCData> npcList)
    {
        lData = new LevelData(cNumber, player, _collection, drops, history, gStats, enemyList,intObjList, npcList);
    }

    /// <summary>
    /// Сбросить данные уровня (используется при переходе на новый уровень)
    /// </summary>
    public void ResetLevelData()
    {
        lData.active = false;
    }

    /// <summary>
    /// Задать общие данные по игре
    /// </summary>
    public void SetGeneralGameData(int cNumber, HeroController player, List<ItemCollection> _collection)
    {
        gGData = new GameGeneralData(cNumber,player,_collection);
    }

}
