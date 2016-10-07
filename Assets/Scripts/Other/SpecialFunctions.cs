using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Функция, хранящая в себе методы, что могут быть использованы всеми скриптами в игре
/// </summary>
public static class SpecialFunctions
{
    public static GameObject player { get { return GameObject.FindGameObjectWithTag("player"); } }

    public static GameController gameController { get { return GameObject.FindGameObjectWithTag("gameController").GetComponent<GameController>(); } }

    public static History history { get { return gameController.GetComponent<GameHistory>().history; } }

    public static GameStatistics statistics { get { return gameController.GetComponent<GameStatistics>(); } }

    /// <summary>
    /// Функция, которая позволяет использовать ComparativeClass и по сути ей можно заменять 
    /// простейшие операции сравнения int c int'ом.
    /// Зачем это нужно? Да чтобы можно было операции сравнения с нужным числом задавать в самом редакторе.
    /// </summary>
    public static bool ComprFunctionality(int arg1, string opr, int arg2)
    {
        return (((arg1 < arg2) && (string.Equals(opr, "<"))) ||
                        ((arg1 <= arg2) && (string.Equals(opr, "<="))) ||
                        ((arg1 == arg2) && (string.Equals(opr, "="))) ||
                        ((arg1 > arg2) && (string.Equals(opr, ">"))) ||
                        ((arg1 >= arg2) && (string.Equals(opr, ">="))) ||
                        ((arg1 != arg2) && (string.Equals(opr, "!="))) ||
                        (string.Equals(opr, "!")));
    }

    public static void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public static void PlayGame()
    {
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Запустить событие, связанное с сюжетом игры
    /// </summary>
    public static void StartStoryEvent(object sender, EventHandler<StoryEventArgs> handler, StoryEventArgs e)
    {
        if (handler != null)
        {
            handler(sender, e);
        }
    } 

}
