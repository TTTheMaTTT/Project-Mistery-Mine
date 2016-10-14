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
    }

    public void Pause()
    {
        canvas.enabled = true;
        SpecialFunctions.PauseGame();
        SpecialFunctions.player.GetComponent<HeroController>().SetImmobile(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
