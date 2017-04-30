using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Окошко, при появлении которого, игра ставится на паузу
/// </summary>
public class InterfaceWindow : UIPanel, ILanguageChangeable
{

    #region fields

    public static InterfaceWindow openedWindow = null;//Окно интерфейса, открытое в данный момент

    [HideInInspector]public Canvas canvas;

    [SerializeField]protected List<MultiLanguageTextInfo> languageChanges = new List<MultiLanguageTextInfo>();

    #endregion //fields

    #region parametres

    protected bool setImmoblie = false;//Управляет ли данное окошко подвижностью главного персонажа
    protected bool canClose = true, canOpen=true;

    #endregion //parametres

    public override void Initialize()
    {
        base.Initialize();
        canvas = GetComponent<Canvas>();
        canvas.enabled = false;
    }

    /// <summary>
    /// Активировать элемент интерфейса
    /// </summary>
    public override void SetActive()
    {
        activePanel = this;
        currentIndex = new UIElementIndex(-1, -1);
    }

    /// <summary>
    /// Сброс окна
    /// </summary>
    public override void Cancel()
    {
        if (canClose)
            CloseWindow();
    }

    /// <summary>
    /// Выдвинуться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    public override void MoveHorizontal(int direction)
    {
        if (currentIndex.indexX==-1 && currentIndex.indexY==-1)
        {
            base.Activate();
        }
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
            base.MoveHorizontal(direction,_index);
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
    /// Открыть окно интерфейса
    /// </summary>
    public virtual void OpenWindow()
    {
        if (openedWindow != null)
            return;
        openedWindow = this;
        canvas.enabled = true;
        HeroController hero = SpecialFunctions.Player.GetComponent<HeroController>();
        setImmoblie = !hero.Immobile;
        if (setImmoblie)  
            hero.SetImmobile(true);

        activePanel = this;
        currentIndex = new UIElementIndex(-1, -1);
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
        if (setImmoblie)
            SpecialFunctions.Player.GetComponent<HeroController>().SetImmobile(false);

        if (activeElement)
        {
            activeElement.SetInactive();
            activeElement = null;
        }
        activePanel = null;
        Cursor.visible = true;
        SpecialFunctions.PlayGame();
    }

    /// <summary>
    /// Процесс, в течение которого окно нельзя закрыть
    /// </summary>
    protected IEnumerator CantInteractProcess()
    {
        canClose = false;
        canOpen = false;
        yield return new WaitForSecondsRealtime(.5f);
        canClose = true;
        canOpen = true;
    }

    /// <summary>
    /// Применить языковые изменения
    /// </summary>
    /// <param name="_language">Язык, на который переходит окно</param>
    public virtual void MakeLanguageChanges(LanguageEnum _language)
    {
        foreach (MultiLanguageTextInfo _languageChange in languageChanges)
            _languageChange.text.text = _languageChange.mLanguageText.GetText(_language);
    }

}
