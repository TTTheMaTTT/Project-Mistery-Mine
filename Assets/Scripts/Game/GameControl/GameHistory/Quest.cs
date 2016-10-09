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

    #endregion //fields

    public Quest()
    {
    }

    public Quest(Quest _quest)
    {
        questName = _quest.questName;
        stage = 0;
        questLine = _quest.questLine;
        hasStatistic = _quest.hasStatistic;
        statisticName = _quest.statisticName;
    }

}
