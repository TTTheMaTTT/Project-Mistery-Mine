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

    #region EventHandlers

    public EventHandler<SoundChangesEventArgs> soundEventHandler;

    #endregion //eventHandlers

    #region fields

    protected GameMenuScript gameMenu;//Меню паузы игры
    private Slider musSlider;
    private Slider fxSlider;
    private Text languageText;

    #endregion //fields

    #region parametres

    public static LanguageEnum language = LanguageEnum.russian;

    #endregion //parametres

    public override void Initialize()
    {
        base.Initialize();
        if (SpecialFunctions.gameInterface == null)
            gameMenu = null;
        else
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

        languageText = panel.FindChild("LanguageChange").FindChild("LanguageText").GetComponent<Text>();

        if (PlayerPrefs.HasKey("Language"))
        {
            language = (LanguageEnum)PlayerPrefs.GetInt("Language");
            languageText.text = language == LanguageEnum.russian ? "Русский" : language == LanguageEnum.english ? "English" : language == LanguageEnum.ukrainian ? "Український" :
                                language == LanguageEnum.polish ? "Polski" : language == LanguageEnum.french ? "Français" : "Русский";
        }
        else
        {
            PlayerPrefs.SetInt("Language", (int)LanguageEnum.russian);
            language = LanguageEnum.russian;
            languageText.text = "Русский";
        }

        Transform interfaceWindows = SpecialFunctions.gameInterface.transform;
        for (int i = 0; i < interfaceWindows.childCount; i++)
        {
            ILanguageChangeable lChangeable = interfaceWindows.GetChild(i).GetComponent<ILanguageChangeable>();
            if (lChangeable != null)
                lChangeable.MakeLanguageChanges(language);

        }

    }

    /// <summary>
    /// Изменить громкость музыки
    /// </summary>
    public void ChangeMusicVolume()
    {
        PlayerPrefs.SetFloat("MusicVolume", musSlider.value);
        if (SpecialFunctions.gameController!=null)
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
    /// Изменить язык
    /// </summary>
    /// <param name="direction">направление, в котором меняется язык</param>
    public void ChangeLanguage(int direction)
    {
        language = (LanguageEnum)(Mathf.RoundToInt(Mathf.Repeat((int)language + direction, 2)));
        PlayerPrefs.SetInt("Language", (int)language);
        languageText.text = language == LanguageEnum.russian ? "Русский" : language == LanguageEnum.english ? "English" : language == LanguageEnum.ukrainian ? "Український" :
                                language == LanguageEnum.polish ? "Polski" : language == LanguageEnum.french ? "Français" : "Русский";
        //Применить изменения ко всем объектам интерфейса
        Transform interfaceWindows = SpecialFunctions.gameInterface.transform;
        for (int i = 0; i < interfaceWindows.childCount; i++)
        {
            ILanguageChangeable lChangeable = interfaceWindows.GetChild(i).GetComponent<ILanguageChangeable>();
            if (lChangeable != null)
                lChangeable.MakeLanguageChanges(language);

        }
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

    /// <summary>
    /// Открыть окно настроек
    /// </summary>
    public override void OpenWindow()
    {
        if (openedWindow != null)
            return;
        openedWindow = this;
        canvas.enabled = true;

        activePanel = this;
        currentIndex = new UIElementIndex(-1, -1);
        Cursor.visible = true;
        SpecialFunctions.PauseGame();
    }

    public override void CloseWindow()
    {
        openedWindow = null;
        canvas.enabled = false;

        if (activeElement)
        {
            activeElement.SetInactive();
            activeElement = null;
        }
        activePanel = null;
        Cursor.visible = true;
        SpecialFunctions.PlayGame();
    }

}
