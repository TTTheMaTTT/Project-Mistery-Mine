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

    public static GameObject gameInterface { get { return GameObject.FindGameObjectWithTag("interface"); } }

    public static GameUIScript gameUI { get { return gameInterface.GetComponentInChildren<GameUIScript>(); } }

    public static LoadMenuScript loadMenu { get { return GameObject.Find("SaveScreen").GetComponent<LoadMenuScript>(); } }

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

    /// <summary>
    /// Функция, выводящая заданный текст на экран на заданное время
    /// </summary>
    public static void SetText(string _info, float textTime)
    {
        gameUI.SetText(_info, textTime);
    }

    /// <summary>
    /// Функция, вызывающая либо затухание, либо проявление экрана
    /// </summary>
    public static void SetFade(bool fadeIn)
    {
        if (fadeIn)
            gameUI.FadeIn();
        else
            gameUI.FadeOut();
    }

    /// <summary>
    /// Сделать игровой экран тёмным
    /// </summary>
    public static void SetDark()
    {
        gameUI.SetDark();
    }

    /// <summary>
    /// Переместить главного героя к чекпоинту
    /// </summary>
    public static void MoveToCheckpoint(CheckpointController checkpoint)
    {
        Vector3 cPos = checkpoint.transform.position, pPos = player.transform.position;
        player.transform.position = new Vector3(cPos.x, cPos.y, pPos.z);
    }

}
