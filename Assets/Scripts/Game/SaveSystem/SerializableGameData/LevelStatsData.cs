using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Сериализованная статистка последнего уровня
/// </summary>
[XmlType("Level Statistics")]
[XmlInclude(typeof(LevelStatisticsInfo))]
public class LevelStatsData
{

    [XmlArray("Level Statistics List")]
    [XmlArrayItem("Level Statistics Info")]
    public List<LevelStatisticsInfo> statistics = new List<LevelStatisticsInfo>();

    public LevelStatsData()
    {
    }

    public LevelStatsData(GameStatistics _gameStats)
    {
        statistics = new List<LevelStatisticsInfo>();
        foreach (Statistics stats in _gameStats.statistics)
        {
            statistics.Add(new LevelStatisticsInfo(stats.statisticName, stats.value));
        }
    }

}

/// <summary>
/// Данные об одной статистике уровня
/// </summary>
[XmlType("Level Statistics Info")]
public class LevelStatisticsInfo
{
    [XmlElement("Statisctics Name")]
    public string statisticsName;

    [XmlElement("Statistics Value")]
    public int statisticsValue;

    public LevelStatisticsInfo()
    { }

    public LevelStatisticsInfo(string sName, int sValue)
    {
        statisticsName = sName;
        statisticsValue = sValue;
    }

}
