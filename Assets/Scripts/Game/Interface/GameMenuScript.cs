using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Скрипт, управляющий окошком игрового меню
/// </summary>
public class GameMenuScript : MonoBehaviour
{

    #region fields

    Canvas canvas;

    #endregion //fields

    public void Awake()
    {
        Initialize();
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
                GoToTheLevel("cave_lvl2");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                GoToTheLevel("cave_lvl3");
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                GoToTheLevel("cave_lvl4");
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                GoToTheLevel("cave_lvl5");
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                GoToTheLevel("mine_lvl1");
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                GoToTheLevel("mine_lvl3");
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                GoToTheLevel("mine_lvl5");
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                GoToTheLevel("mine_lvl7");
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                GoToTheLevel("mine_lvl8");
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                SpecialFunctions.Player.GetComponent<HeroController>().Health = 100f;
            }
        }
    }

    public void Initialize()
    {
        canvas = GetComponent<Canvas>();
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
        if (canvas.enabled)
            Return();
        else
            Pause();
    }

    /// <summary>
    /// Выйти из игрового меню
    /// </summary>
    public void Return()
    {
        canvas.enabled = false;
        SpecialFunctions.PlayGame();
        SpecialFunctions.Player.GetComponent<HeroController>().SetImmobile(false);
        Cursor.visible = false;
    }

    /// <summary>
    /// Поставить игру на паузу
    /// </summary>
    public void Pause()
    {
        canvas.enabled = true;
        SpecialFunctions.PauseGame();
        SpecialFunctions.Player.GetComponent<HeroController>().SetImmobile(true);
        Cursor.visible = true;
    }

    /// <summary>
    /// Перезапустить игру с последнего сохранения
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Перезапустить весь уровень
    /// </summary>
    public void RestartLevel()
    {
        SpecialFunctions.gameController.ResetLevelData();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
