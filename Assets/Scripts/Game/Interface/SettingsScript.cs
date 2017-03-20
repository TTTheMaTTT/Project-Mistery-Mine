using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт, управляющий окошком, задающим настройки игры
/// </summary>
public class SettingsScript : InterfaceWindow
{

    #region fields

    protected GameMenuScript gameMenu;//Меню паузы игры

    #endregion //fields

    #region EventHandlers

    public EventHandler<SoundChangesEventArgs> soundEventHandler;

    #endregion //eventHandlers

    #region fields

    private Slider musSlider;
    private Slider fxSlider;

    #endregion //fields

    protected override void Awake()
    {
        base.Awake();
        gameMenu = SpecialFunctions.gameInterface.GetComponentInChildren<GameMenuScript>();
        Transform panel = transform.FindChild("Panel");
        musSlider = panel.FindChild("MusicSlider").GetComponentInChildren<Slider>();
        fxSlider = panel.FindChild("SoundSlider").GetComponentInChildren<Slider>();
        //tutorToggle = transform.FindChild("TutorialReset").GetComponent<Toggle>();
        if (PlayerPrefs.HasKey("MusicVolume"))
            musSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        else
            PlayerPrefs.SetFloat("MusicVolume", musSlider.value);
        if (PlayerPrefs.HasKey("SoundVolume"))
            fxSlider.value = PlayerPrefs.GetFloat("SoundVolume");
        else
            PlayerPrefs.SetFloat("SoundVolume", fxSlider.value);
        SpecialFunctions.soundVolume = fxSlider.value;
    }

    /// <summary>
    /// Изменить громкость музыки
    /// </summary>
    public void ChangeMusicVolume()
    {
        PlayerPrefs.SetFloat("MusicVolume", musSlider.value);
        SpecialFunctions.gameController.ChangeMusicVolume(musSlider.value);
    }

    /// <summary>
    /// Изменить громкость звуков
    /// </summary>
    public void ChangeSoundVolume()
    {
        PlayerPrefs.SetFloat("SoundVolume", fxSlider.value);
        OnSoundChange(new SoundChangesEventArgs(fxSlider.value));
        SpecialFunctions.soundVolume = fxSlider.value;
    }

    /// <summary>
    /// Событие о том, что изменились параметры звуков
    /// </summary>
    protected void OnSoundChange(SoundChangesEventArgs e)
    {
        if (soundEventHandler != null)
            soundEventHandler(this, e);
    }

    /// <summary>
    /// Открыть меню паузы
    /// </summary>
    public void GoToTheGameMenu()
    {
        if (gameMenu == null)
            return;
        if (openedWindow == this)
            CloseWindow();
        gameMenu.OpenWindow();
    }

}
