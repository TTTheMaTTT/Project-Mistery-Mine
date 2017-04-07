using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Скрипт, управляющий окошком игрового меню
/// </summary>
public class GameMenuScript : InterfaceWindow
{

    #region fields

    protected SettingsScript settings;//Окошко, в котором можно настроить игру

    #endregion //fields

    protected override void Awake()
    {
        base.Awake();
        settings = SpecialFunctions.gameInterface.GetComponentInChildren<SettingsScript>();
    }

    public void Update()
    {
        //Читы для разработчиков. Потом надо будет убрать
        if (canvas.enabled && Input.anyKeyDown)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                GoToTheLevel("cave_lvl1");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                GoToTheLevel("cave_lvl5");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                GoToTheLevel("mine_lvl1");
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                GoToTheLevel("mine_lvl4");
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                GoToTheLevel("mine_lvl8");
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                GoToTheLevel("DM_lvl1");
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                GoToTheLevel("DM_lvl3");
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                GoToTheLevel("DM_lvl5");
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                GoToTheLevel("underworld_lvl1");
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                GoToTheLevel("underworld_lvl3");
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                SpecialFunctions.Player.GetComponent<HeroController>().Health = 100f;
            }
        }
    }

    /// <summary>
    /// Перейти в главное меню
    /// </summary>
    public void GoToTheMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Перейти на уровень с указанным названием
    /// </summary>
    void GoToTheLevel(string levelName)
    {
        if (levelName != SceneManager.GetActiveScene().name)
        {
            PlayerPrefs.SetInt("Checkpoint Number", 0);
            SpecialFunctions.gameController.SaveGame(0, true, levelName);
            SceneManager.LoadScene(levelName);
        }
    }

    /// <summary>
    /// Сменить режим хода игры (пауза или проигрывание)
    /// </summary>
    public void ChangeGameMod()
    {
        if (SpecialFunctions.levelEnd)
            return;
        if (openedWindow != null)
            openedWindow.CloseWindow();
        else
            OpenWindow();
    }


    /// <summary>
    /// Перезапустить игру с последнего сохранения
    /// </summary>
    public void RestartGame()
    {
        CloseWindow();
        SpecialFunctions.gameController.EndLevel();
    }

    /// <summary>
    /// Перезапустить весь уровень
    /// </summary>
    public void RestartLevel()
    {
        SpecialFunctions.gameController.ResetLevelData();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Открыть окно настроек
    /// </summary>
    public void GoToSettings()
    {
        if (settings == null)
            return;
        if (openedWindow == this)
            CloseWindow();
        settings.OpenWindow();
    }

    /// <summary>
    /// Функция сброса всех игровых достижений
    /// </summary>
    public void ResetAchievements()
    {
        SpecialFunctions.ResetAchievements();
    }

}
