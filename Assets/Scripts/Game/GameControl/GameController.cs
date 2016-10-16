using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Объект, ответственный за управление игрой
/// </summary>
public class GameController : MonoBehaviour
{

    #region fields

    protected DialogWindowScript dialogWindow;
    protected GameMenuScript gameMenu;

    #endregion //fields

    protected void Update()
    {
        if (Input.GetButtonDown("Cancel"))
            gameMenu.ChangeGameMod();
    }

    protected void Awake()
    {
        Initialize();
    }

    protected void Initialize()
    {
        Transform interfaceWindows = SpecialFunctions.gameInterface.transform;
        dialogWindow = interfaceWindows.GetComponentInChildren<DialogWindowScript>();
        gameMenu = interfaceWindows.GetComponentInChildren<GameMenuScript>();
        SpecialFunctions.PlayGame();
        if (SceneManager.GetActiveScene().name != "MainMenu")
            Cursor.visible = false;
    }

    public static void GoToTheNextLevel()
    {
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    ///  Начать диалог
    /// </summary>
    public void StartDialog(NPCController npc, Dialog dialog)
    {
        Transform player = SpecialFunctions.player.transform;
        dialogWindow.BeginDialog(player, npc, dialog);
    }

}
