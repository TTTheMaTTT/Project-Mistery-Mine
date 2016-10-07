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

    #endregion //fields

    public Quest()
    {
    }

    public Quest(Quest _quest)
    {
        questName = _quest.questName;
        stage = 0;
        questLine = _quest.questLine;
    }

}
