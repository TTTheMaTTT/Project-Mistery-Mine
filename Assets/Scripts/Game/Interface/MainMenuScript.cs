using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Скрипт, управляющий окном главного меню игры 
/// </summary>
public class MainMenuScript : MonoBehaviour
{

    public void Awake()
    {
        SpecialFunctions.PlayGame();
    }

    public void Update()
    {
        if (Input.GetButtonDown("Cancel"))
            Application.Quit();
    }

    /// <summary>
    /// Продолжить игру с последнего сохранения (если такое имеется)
    /// </summary>
    public void ContinueGame()
    {
        SpecialFunctions.loadMenu.Continue();
    }

    /// <summary>
    /// Начать игру в одном из профилей (открыть меню загрузки)
    /// </summary>
    public void StartGame()
    {
        SpecialFunctions.loadMenu.OpenLoadMenu();
    }

}
