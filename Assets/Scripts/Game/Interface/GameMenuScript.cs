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
                SceneManager.LoadScene("cave_lvl1");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SceneManager.LoadScene("cave_lvl2");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SceneManager.LoadScene("cave_lvl3");
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SceneManager.LoadScene("cave_lvl4");
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SceneManager.LoadScene("cave_lvl5");
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                SpecialFunctions.player.GetComponent<HeroController>().Health = 100f;
            }
        }
    }

    public void Initialize()
    {
        canvas = GetComponent<Canvas>();
    }

    public void GoToTheMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void ChangeGameMod()
    {
        if (canvas.enabled)
            Return();
        else
            Pause();
    }

    public void Return()
    {
        canvas.enabled = false;
        SpecialFunctions.PlayGame();
        SpecialFunctions.player.GetComponent<HeroController>().SetImmobile(false);
        Cursor.visible = false;
    }

    public void Pause()
    {
        canvas.enabled = true;
        SpecialFunctions.PauseGame();
        SpecialFunctions.player.GetComponent<HeroController>().SetImmobile(true);
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
