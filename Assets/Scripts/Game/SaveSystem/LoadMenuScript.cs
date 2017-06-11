using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт экрана загрузки игры
/// </summary>
public class LoadMenuScript : InterfaceWindow
{

    #region consts

    private const string firstLevelName = "BeginComics";

    #endregion //consts

    #region fields

    protected SavesInfo savesInfo;//Информация о самих сохранениях
    protected string savesInfoPath;

    protected string savePath;//По какому пути находятся все сохранения

    public ProfileButton[] saveButtons;//Кнопки, которые соответствуют трём различным профилям

    protected ProfileButton chosenButton;

    protected Transform savesPanel;

    protected GameObject createNewFadePanel, warningPanel;
    protected UIPanel createNewUIPanel, warningUIPanel;
    protected InputField saveNameInputPanel;

    #endregion //fields

    #region parametres

    protected MultiLanguageText defaultMLSaveName = new MultiLanguageText("Новое сохранение", "New Game", "Нове збереження", "", "");

    #endregion //parametres

    /// <summary>
    /// Инициализация окна сохранения
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        savePath = Application.streamingAssetsPath;
        savesInfoPath = Application.streamingAssetsPath+"/SavesInfo.xml";

        FileInfo[] fInfos = new DirectoryInfo(Application.streamingAssetsPath).GetFiles();
        bool hasFile = false;
        for (int i = 0; i < fInfos.Length; i++)
            if (fInfos[i].Name == "SavesInfo.xml")
            {
                hasFile = true;
                break;
            }

        if (!hasFile)
        {
            Serializator.SaveXmlSavesInfo(new SavesInfo(3), savesInfoPath);
            for (int i = 0; i < 3; i++)
            {
                string savePath1 = savePath + "Profile" + i.ToString() + ".xml";
                Serializator.SaveXml(null, savePath + "/Profile" + i.ToString() + ".xml");
            }
        }

        savesInfo = Serializator.DeXmlSavesInfo(savesInfoPath);

        chosenButton = null;

        Transform mainPanel = transform.FindChild("MainPanel");
        savesPanel = mainPanel.FindChild("Saves").FindChild("SavesPanel");
        saveButtons = new ProfileButton[3];
        for (int i = 0; i < 3; i++)
        {
            saveButtons[i] = savesPanel.FindChild("Profile" + (i + 1).ToString()).GetComponent<ProfileButton>();
            saveButtons[i].Initialize(savesInfo.saves[i], this);
        }

        warningPanel = transform.FindChild("WarningPanel").gameObject;
        createNewFadePanel = transform.FindChild("CreateNewFadePanel").gameObject;
        createNewUIPanel = createNewFadePanel.GetComponent<UIPanel>();
        warningUIPanel = warningPanel.GetComponent<UIPanel>();
        saveNameInputPanel = createNewFadePanel.transform.FindChild("CreateNewPanel").GetComponentInChildren<InputField>();
    }

    /// <summary>
    /// Выбрать то или иное сохранение и произвести ним действие
    /// </summary>
    public void ChooseButton(ProfileButton pButton)
    {
        if (chosenButton!=null? pButton != chosenButton: true)
        {
            if (chosenButton!=null)
                chosenButton.SetImage(false);
            chosenButton = pButton;
            chosenButton.SetImage(true);
        }
        else
        {
            if (pButton.SInfo.hasData)
                Load(savesInfo.saves.IndexOf(pButton.SInfo));
            else
                ChooseNewGameCreation();
        }
    }

    /// <summary>
    /// Загрузить профиль с данным номаером
    /// </summary>
    public void Load(int _profileNumber)
    {
        PlayerPrefs.SetInt("Profile Number", _profileNumber);
        PlayerPrefs.SetFloat("Hero Health", 12f);

        LoadingScreenScript.instance.LoadScene(savesInfo.saves[_profileNumber].loadSceneName);

    }

    /// <summary>
    /// Продолжить игру с последнего сохранения (Если оно вообще есть)
    /// </summary>
    public void Continue()
    {
        if (savesInfo.saves[savesInfo.currentProfileNumb].hasData)
            Load(savesInfo.currentProfileNumb);
    }

    /// <summary>
    /// Начать новую игру
    /// </summary>
    public void ChooseNewGameCreation()
    {
        if (chosenButton != null)
        {
            if (chosenButton.SInfo.hasData)
                OpenWarningWindow(true);
            else
                OpenCreateNewGameWindow(true);
        }
    }

    /// <summary>
    /// Открыть окно предупреждения о перезаписи игры
    /// </summary>
    public void OpenWarningWindow(bool yes)
    {
        warningPanel.SetActive(yes);
        if (yes)
            warningUIPanel.SetActive();
        else
            SetActive();
    }

    /// <summary>
    /// Начать создание нового сохранения
    /// </summary>
    public void OpenCreateNewGameWindow(bool yes)
    {
        warningPanel.SetActive(false);
        createNewFadePanel.SetActive(yes);
        if (yes)
            createNewUIPanel.SetActive();
        else
            SetActive();
    }

    /// <summary>
    /// Перезаписать профиль и начать новую игру на нём
    /// </summary>
    public void CreateNewGame()
    {
        if ((saveNameInputPanel.text != string.Empty))
        {
            if (chosenButton != null)
            {
                SaveInfo sInfo = chosenButton.SInfo;
                sInfo.hasData = true;
                sInfo.loadSceneName = firstLevelName;
                sInfo.saveName = saveNameInputPanel.text;
                sInfo.saveTime = System.DateTime.Now.ToString();
                savesInfo.currentProfileNumb = savesInfo.saves.IndexOf(sInfo);

                PlayerPrefs.SetInt("Profile Number", savesInfo.currentProfileNumb);
                PlayerPrefs.SetFloat("Hero Health", 12f);
                PlayerPrefs.SetInt("Checkpoint Number", 0);

                createNewFadePanel.SetActive(false);
                Serializator.SaveXmlSavesInfo(savesInfo, savesInfoPath);
                string save1 = savePath + "Profile" + savesInfo.currentProfileNumb.ToString() + ".xml";
                Serializator.SaveXml(null, savePath + "/Profile" + savesInfo.currentProfileNumb.ToString()+".xml");
                LoadingScreenScript.instance.LoadScene(firstLevelName);
                CloseWindow();
            }
            else
            {
                OpenCreateNewGameWindow(false);
            }
        }
    }

    /// <summary>
    /// Открыть окно загрузки
    /// </summary>
    public override void OpenWindow()
    {
        if (openedWindow != null)
            return;
        openedWindow = this;
        canvas.enabled = true;
        activePanel = this;
        currentIndex = new UIElementIndex(-1, -1);
        SpecialFunctions.PauseGame();
    }

    /// <summary>
    /// Закрыть меню загрузки
    /// </summary>
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
        SpecialFunctions.PlayGame();

        OpenWarningWindow(false);
        OpenCreateNewGameWindow(false);
        if (chosenButton != null)
        {
            chosenButton.SetImage(false);
            chosenButton = null;
        }
    }

    /// <summary>
    /// Применить настройки языка
    /// </summary>
    /// <param name="_language">текущий язык игры</param>
    public override void MakeLanguageChanges(LanguageEnum _language)
    {
        base.MakeLanguageChanges(_language);
        foreach (ProfileButton _profile in saveButtons)
            if (!_profile.SInfo.hasData)
                _profile.transform.FindChild("SaveName").GetComponent<Text>().text = defaultMLSaveName.GetText(_language);
    }

}
