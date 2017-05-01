using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, управляющий окном главного меню игры 
/// </summary>
public class MainMenuScript : UIPanel, ILanguageChangeable
{

    #region fields

    private GameObject buttons;
    private GameObject creditsPanel;
    private UIPanel creditsUIWindow;
    private Text creditsText;

    [SerializeField]private List<MultiLanguageTextInfo> languageChanges=new List<MultiLanguageTextInfo>();

    #endregion //fields

    #region parametres

    private bool activated = false;

    #endregion //parametres

    public override void Initialize()
    {
        buttons = transform.FindChild("Buttons").gameObject;
        creditsPanel = transform.FindChild("CreditsPanel").gameObject;
        creditsUIWindow = creditsPanel.GetComponent<UIPanel>();
        creditsText = creditsPanel.transform.FindChild("CreditsText").GetComponent<Text>();

        SpecialFunctions.PlayGame();
        activated = false;
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

}
