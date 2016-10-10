using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс реализующий такое понятие, как квест
/// </summary>
public class Quest : ScriptableObject
{

    #region fields

    public string questName;//Имя квеста
    public List<string> questLine = new List<string>();//Какие есть подзадачи у квеста?

    [HideInInspector]
    public int stage = 0;// На какой стадии находится квест?

    public bool hasStatistic=false;//Ведётся ли статистика по выполнению задания?
    public string statisticName = "";//По какой статистике отслеживается задание?

    public int statisticCount=0;

    #endregion //fields

    public Quest()
    {
    }

    public Quest(Quest _quest)
    {
        questName = _quest.questName;
        stage = 0;
        questLine = new List<string>();
        for (int i = 0; i < _quest.questLine.Count; i++)
        {
            questLine.Add(_quest.questLine[i]);
        }
        hasStatistic = _quest.hasStatistic;
        statisticName = _quest.statisticName;
        statisticCount = 0;
    }

}
