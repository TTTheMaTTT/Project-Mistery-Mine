using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт экрана загрузки игры
/// </summary>
public class LoadMenuScript : MonoBehaviour
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

    protected GameObject createNewFadePanel;
    protected GameObject warningPanel;
    protected InputField saveNameInputPanel;

    #endregion //fields

    public void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// Инициализация окна сохранения
    /// </summary>
    public void Initialize()
    {
        savePath = (Application.dataPath) + "/StreamingAssets/Saves/";
        savesInfoPath = (Application.dataPath) + "/StreamingAssets/SavesInfo.xml";
        savesInfo = Serializator.DeXmlSavesInfo(savesInfoPath);

        FileInfo[] fInfos = new DirectoryInfo((Application.dataPath) + "/StreamingAssets/").GetFiles();
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
                Serializator.SaveXml(null, savePath + "Profile" + i.ToString()+".xml");
        }

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
        }
    }

    /// <summary>
    /// Загрузить профиль с данным номаером
    /// </summary>
    public void Load(int _profileNumber)
    {
        PlayerPrefs.SetInt("Profile Number", _profileNumber);
        PlayerPrefs.SetFloat("Hero Health", 12f);

        SceneManager.LoadScene(savesInfo.saves[_profileNumber].loadSceneName);


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
    }

    /// <summary>
    /// Начать создание нового сохранения
    /// </summary>
    public void OpenCreateNewGameWindow(bool yes)
    {
        warningPanel.SetActive(false);
        createNewFadePanel.SetActive(yes);
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
                Serializator.SaveXml(null, savePath + "Profile" + savesInfo.currentProfileNumb.ToString()+".xml");
                SceneManager.LoadScene(firstLevelName);
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
    public void OpenLoadMenu()
    {
        GetComponent<Canvas>().enabled = true;
    }

    /// <summary>
    /// Закрыть меню загрузки
    /// </summary>
    public void CloseLoadMenu()
    {
        GetComponent<Canvas>().enabled = false;
        OpenWarningWindow(false);
        OpenCreateNewGameWindow(false);
        if (chosenButton != null)
        {
            chosenButton.SetImage(false);
            chosenButton = null;
        }
    }

}
