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
    public List<QuestLine> questLines = new List<QuestLine>();//Какие есть подзадачи у квеста?

    [HideInInspector]
    public int stage = 0;// На какой стадии находится квест?

    public bool hasStatistic = false;//Ведётся ли статистика по выполнению задания?
    public string statisticName = "";//По какой статистике отслеживается задание?

    public int statisticCount = 0;

    #endregion //fields

    public Quest()
    {
    }

    public Quest(Quest _quest)
    {
        questName = _quest.questName;
        stage = 0;
        questLines = new List<QuestLine>();
        for (int i = 0; i < _quest.questLines.Count; i++)
        {
            questLines.Add(new QuestLine(_quest.questLines[i]));
        }
        hasStatistic = _quest.hasStatistic;
        statisticName = _quest.statisticName;
    }
}

[System.Serializable]
public class QuestLine
{
    public string questLineName;
    public MultiLanguageText mlText;

    public QuestLine(string _questLineName, MultiLanguageText _mlText)
    {
        questLineName = _questLineName;
        mlText = _mlText;
    }

    public QuestLine(QuestLine _qLine)
    {
        questLineName = _qLine.questLineName;
        mlText = _qLine.mlText;
    }

}