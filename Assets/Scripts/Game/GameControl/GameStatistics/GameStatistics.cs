using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Компонент игрового контроллера, ответственный за подсчёт игровых параметров, а также за хранение информации
/// </summary>
public class GameStatistics : MonoBehaviour
{

    #region eventHandlers

    public EventHandler<StoryEventArgs> StartGameEvent;

    #endregion //eventHandlers

    #region fields

    public List<Statistics> statistics=new List<Statistics>();//Здесь задаётся, подсчёт каких игровых параметров нас интересует

    public DatabaseClass database;//База данных в игре

    #endregion //fields

    void Start()
    {
        SpecialFunctions.StartStoryEvent(this, StartGameEvent, new StoryEventArgs());
    }

    /// <summary>
    /// Возвращает квест по названию
    /// </summary>
    public Quest GetQuest(string _questName)
    {
        if (database != null)
            return database.quests.Find(x => x.questName == _questName);
        else
            return null;
    }

    public void ConsiderStatistics(UnityEngine.Object obj)
    {
        List<Statistics> currentList = statistics.FindAll(x => (x.GetObjType.IsAssignableFrom(obj.GetType())));

        foreach (Statistics currentStatistics in currentList)
        {
            currentStatistics.Compare(obj);
        }

    }

}
