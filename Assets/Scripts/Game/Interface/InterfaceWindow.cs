using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Окошко, при появлении которого, игра ставится на паузу
/// </summary>
public class InterfaceWindow : MonoBehaviour
{

    public static InterfaceWindow openedWindow = null;//Окно интерфейса, открытое в данный момент

    [HideInInspector]public Canvas canvas;

    protected virtual void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.enabled = false;
    }

    /// <summary>
    /// Открыть окно интерфейса
    /// </summary>
    public virtual void OpenWindow()
    {
        if (openedWindow != null)
            return;
        openedWindow = this;
        canvas.enabled = true;
        SpecialFunctions.Player.GetComponent<HeroController>().SetImmobile(true);
        Cursor.visible = true;
        SpecialFunctions.PauseGame();
    }

    /// <summary>
    /// Закрыть окно интерфейса
    /// </summary>
    public virtual void CloseWindow()
    {
        openedWindow = null;
        canvas.enabled = false;
        SpecialFunctions.Player.GetComponent<HeroController>().SetImmobile(false);
        Cursor.visible = true;
        SpecialFunctions.PlayGame();
    }

}
