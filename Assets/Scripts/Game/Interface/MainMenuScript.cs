using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Steamworks;

/// <summary>
/// Скрипт, управляющий окном главного меню игры 
/// </summary>
public class MainMenuScript : UIPanel, ILanguageChangeable
{

    #region consts

    private const float fadeSpeed = 3f;

    #endregion //consts

    #region fields

    private GameObject buttons;
    private GameObject creditsPanel;
    private UIPanel creditsUIWindow;
    private Transform developersPanel;
    private Text creditsText2;

    private Image fadeScreen;

    [SerializeField]private List<MultiLanguageTextInfo> languageChanges=new List<MultiLanguageTextInfo>();

    #endregion //fields

    #region parametres

    private bool activated = false;

    private float currentFadeSpeed=fadeSpeed;

    #endregion //parametres

    public override void Initialize()
    {
        buttons = transform.FindChild("Buttons").gameObject;
        creditsPanel = transform.FindChild("CreditsPanel").gameObject;
        creditsUIWindow = creditsPanel.GetComponent<UIPanel>();
        developersPanel = creditsPanel.transform.FindChild("DevelopersPanel");
        creditsText2 = creditsPanel.transform.FindChild("CreditsText2").GetComponent<Text>();

        fadeScreen = transform.FindChild("FadeScreen").GetComponent<Image>();
        fadeScreen.color = Color.black;
        StartCoroutine(FadeProcess());

        SpecialFunctions.PlayGame();
        activated = false;
    }

    public void Start()
    {
        if (!PlayerPrefs.HasKey("DefaultLanguage") && SteamManager.s_instance != null)
        {
            switch (SteamApps.GetCurrentGameLanguage())
            {
                case "english":
                    PlayerPrefs.SetString("DefaultLanguage", "English");
                    SpecialFunctions.Settings.ChangeLanguage(LanguageEnum.english);
                    break;
                case "russian":
                    PlayerPrefs.SetString("DefaultLanguage", "Russian");
                    SpecialFunctions.Settings.ChangeLanguage(LanguageEnum.russian);
                    break;
                default:
                    PlayerPrefs.SetString("DefaultLanguage", "English");
                    SpecialFunctions.Settings.ChangeLanguage(LanguageEnum.english);
                    break;
            }
        }
    }

    public void Update()
    {
        if (UIElementScript.activePanel != null)
        {
            if (InputCollection.instance.GetButtonDown("InterfaceMoveHorizontal"))
                UIElementScript.activePanel.MoveHorizontal(Mathf.RoundToInt(Mathf.Sign(InputCollection.instance.GetAxis("InterfaceMoveHorizontal"))));
            if (InputCollection.instance.GetButtonDown("InterfaceMoveVertical"))
                UIElementScript.activePanel.MoveVertical(-Mathf.RoundToInt(Mathf.Sign(InputCollection.instance.GetAxis("InterfaceMoveVertical"))));
            if (InputCollection.instance.GetButtonDown("Cancel"))
                UIElementScript.activePanel.Cancel();
        }

        if (UIElementScript.activeElement != null)
        {
            if (InputCollection.instance.GetButtonDown("Submit"))
                UIElementScript.activeElement.Activate();
        }
        if (InterfaceWindow.openedWindow != null)
            activated = false;
        else if (!activated)
        {
            activated = true;
            SetActive();
        }

        fadeScreen.color = Color.Lerp(fadeScreen.color, new Color(0f, 0f, 0f, 0f), Time.fixedDeltaTime * currentFadeSpeed);

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
        SpecialFunctions.loadMenu.OpenWindow();
    }

    /// <summary>
    /// Открыть окно с титрами
    /// </summary>
    public void OpenCredits()
    {
        creditsPanel.SetActive(true);
        developersPanel.gameObject.SetActive(true);
        creditsText2.gameObject.SetActive(false);
        //buttons.SetActive(false);
        if (activeElement)
        {
            activeElement.SetInactive();
            activeElement = null;
            currentIndex = new UIElementIndex(-1, -1);
        }
        SetInactive();
        creditsUIWindow.SetActive();
    }

    /// <summary>
    /// Показать определённую страницу титр
    /// </summary>
    public void ShowCredits(int creditsNumb)
    {
        developersPanel.gameObject.SetActive(creditsNumb == 1);
        creditsText2.gameObject.SetActive(creditsNumb == 2);
    }

    /// <summary>
    /// Закрыть окно с титрами
    /// </summary>
    public void CloseCredits()
    {
        creditsPanel.SetActive(false);
        buttons.SetActive(true);
        SetActive();
    }

    /// <summary>
    /// Активировать элемент интерфейса
    /// </summary>
    public override void SetActive()
    {
        activePanel = this;
        currentIndex = new UIElementIndex(-1, -1);
        creditsPanel.SetActive(false);
        buttons.SetActive(true);
    }

    /// <summary>
    /// Сброс окна
    /// </summary>
    public override void Cancel()
    {
    }

    /// <summary>
    /// Выдвинуться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    public override void MoveHorizontal(int direction)
    {
        if (currentIndex.indexX == -1 && currentIndex.indexY == -1)
            base.SetActive();
        else
            base.MoveHorizontal(direction);
    }

    /// <summary>
    /// Выдвинуться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    /// <param name="_index">Индекс, с которого происходит перемещение</param>
    public override void MoveHorizontal(int direction, UIElementIndex _index)
    {
        if (currentIndex.indexX == -1 && currentIndex.indexY == -1)
            base.SetActive();
        else
            base.MoveHorizontal(direction, _index);
    }

    /// <summary>
    /// Двинуться в вертикальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    public override void MoveVertical(int direction)
    {
        if (currentIndex.indexX == -1 && currentIndex.indexY == -1)
            base.SetActive();
        else
            base.MoveVertical(direction);
    }

    /// <summary>
    /// Двинуться в вертикальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    public override void MoveVertical(int direction, UIElementIndex _index)
    {
        if (currentIndex.indexX == -1 && currentIndex.indexY == -1)
            base.SetActive();
        else
            base.MoveVertical(direction, _index);
    }

    /// <summary>
    /// Выход 
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// Применить языковые изменения
    /// </summary>
    /// <param name="_language">Язык, на который переходит окно</param>
    public void MakeLanguageChanges(LanguageEnum _language)
    {
        foreach (MultiLanguageTextInfo _languageChange in languageChanges)
            _languageChange.text.text = _languageChange.mLanguageText.GetText(_language);
    }

    /// <summary>
    /// Процесс затухания экрана
    /// </summary>
    IEnumerator FadeProcess()
    {
        currentFadeSpeed = 0f;
        yield return new WaitForSecondsRealtime(1f);
        currentFadeSpeed = fadeSpeed/2f;
    }

}
