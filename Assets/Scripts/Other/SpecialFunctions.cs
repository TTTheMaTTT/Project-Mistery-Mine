using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using Steamworks;

/// <summary>
/// Функция, хранящая в себе методы, что могут быть использованы всеми скриптами в игре
/// </summary>
public static class SpecialFunctions
{
    public static GameObject player = null;
    public static BattleField battleField = null;
    public static GameObject Player
    {
        get
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("player");
                battleField = player.transform.FindChild("Indicators").GetComponentInChildren<BattleField>();
            }
            return player;
        }
    }

    public static CameraController camControl;
    public static CameraController CamController { get { camControl = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraController>(); return camControl; } }

    public static GameController gameController { get { return GameObject.FindGameObjectWithTag("gameController").GetComponent<GameController>(); } }

    public static History history { get { return gameController.GetComponent<GameHistory>().history; } }

    public static GameStatistics statistics { get { return gameController.GetComponent<GameStatistics>(); } }

    public static GameObject gameInterface { get { return GameObject.FindGameObjectWithTag("interface"); } }
    private static SettingsScript settings;
    public static SettingsScript Settings { get { if (settings == null) settings=gameInterface.GetComponentInChildren<SettingsScript>(); return settings; } set { settings = value; } }

    public static GameUIScript gameUI { get { return gameInterface.GetComponentInChildren<GameUIScript>(); } }
    public static DialogWindowScript dialogWindow { get { return gameInterface.GetComponentInChildren<DialogWindowScript>(); } }

    public static EquipmentMenu equipWindow { get { return gameInterface.GetComponentInChildren<EquipmentMenu>(); } }

    public static LoadMenuScript loadMenu { get { return GameObject.Find("SaveScreen").GetComponent<LoadMenuScript>(); } }

    public static bool totalPaused = false;//Пауза, которая не может быть снята функцией PlayGame()
    public static bool levelEnd = false;//Закончен ли уровень
    public static string nextLevelName = "";//Название следующего уровня

    public static float soundVolume;//Громкость звуков

    /// <summary>
    /// Получиьб название уровня
    /// </summary>
    public static string GetLevelName()
    {
        return statistics.LevelTextName;
    }

    /// <summary>
    /// Проинициализировать важные игровые объекты перед началом игры
    /// </summary>
    public static void InitializeObjects()
    {
        totalPaused = false;
        levelEnd = false;
        camControl= GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraController>();
        player = GameObject.FindGameObjectWithTag("player");
        battleField = player.transform.FindChild("Indicators").GetComponentInChildren<BattleField>();
        settings = gameInterface.GetComponentInChildren<SettingsScript>();
    }

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
                        (string.Equals(opr, "!")) ||
                        (opr==string.Empty));
    }

    /// <summary>
    /// Поставить игру на паузу
    /// </summary>
    public static void PauseGame()
    {
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Возобновить ход игры
    /// </summary>
    public static void PlayGame()
    {
        if (!totalPaused)
            Time.timeScale = 1f;
    }

    /// <summary>
    /// Сменить основного игрока
    /// </summary>
    public static void SwitchPlayer(HeroController _hero)
    {
        HeroController prevHero = player.GetComponent<HeroController>();
        if (battleField != null)
            battleField.ResetBattlefield();
        _hero.Equipment = prevHero.Equipment;
        _hero.CurrentWeapon = prevHero.CurrentWeapon;
        _hero.Health = prevHero.Health;
        _hero.MaxHealth = prevHero.MaxHealth;
        player = _hero.gameObject;
        battleField = player.transform.FindChild("Indicators").GetComponentInChildren<BattleField>();
        if (CamController != null)
            CamController.SetPlayer(_hero.transform);
        if (gameUI != null)
            gameUI.ConsiderPlayer(_hero);
        if (dialogWindow.activated)
            _hero.SetImmobile(true);
        if (equipWindow != null)
            equipWindow.ConsiderPlayer(_hero);
        gameController.ConsiderHero(_hero);
    }

    /// <summary>
    /// Сохранить игру у определённого чекпоинта
    /// </summary>
    /// <param name="checkpointNumb"></param>
    public static void SaveGame(int checkpointNumb)
    {
        gameController.StartSaveGameProcess(checkpointNumb, false, SceneManager.GetActiveScene().name);
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
        gameUI.SetMessage(_info, textTime);
    }

    /// <summary>
    /// Функция, выводящая заданный тект в поле сообщений о секретах и эффектах
    /// </summary>
    public static void SetSecretText(float textTime, string _text = "Вы нашли секретное место!")
    {
        gameUI.SetSecretMessage(textTime,_text);
    }

    /// <summary>
    /// Функция, обрабатывающая событие нахождения секретного места
    /// </summary>
    public static void FindSecretPlace(float textTime)
    {
        gameUI.SetSecretMessage(textTime);
        gameController.FindSecretPlace();
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
    /// Установить скорость затухания
    /// </summary>
    public static void SetFadeSpeed(float _fadeSpeed)
    {
        gameUI.FadeSpeed = _fadeSpeed;
    }

    /// <summary>
    /// Установить дефолтную скорость затухания
    /// </summary>
    public static void SetDefaultFadeSpeed()
    {
        gameUI.SetDefaultFadeSpeed();
    }

    /// <summary>
    /// Переместить главного героя к чекпоинту
    /// </summary>
    public static void MoveToCheckpoint(CheckpointController checkpoint)
    {
        Vector3 cPos = checkpoint.transform.position, pPos = Player.transform.position;
        Player.transform.position = new Vector3(cPos.x, cPos.y, pPos.z);
    }

    /// <summary>
    /// Функция, сбрасывающая все достижения (нужно для тестов системы ачивок)
    /// </summary>
    public static void ResetAchievements()
    {
        SteamUserStats.ResetAllStats(true);
        SteamUserStats.RequestCurrentStats();
    }

    /// <summary>
    /// Проиграть звук
    /// </summary>
    public static void PlaySound(AudioSource source)
    {
        if (!source)
            return;
        source.volume = soundVolume;
        source.Play();
    }

}
