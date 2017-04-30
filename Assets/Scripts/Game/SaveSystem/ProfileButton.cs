using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Скрипт кнопки профиля игрока
/// </summary>
public class ProfileButton: UIElementScript
{

    #region consts

    protected const float activateAlpha = .5f;
    protected const float inactiveIntensity = 1f, activeIntensity = .5f, clickedIntensity = .3f;//Как будет подкрашиваться кнопка при различных уровнях взаимодействия с ней

    #endregion //consts

    #region fields

    protected LoadMenuScript saveMenu;//Экран сохранения игры, с которым согласуется работа

    protected Text saveName, saveTime;
    public string SaveName { get { return saveName.text; } }
    protected Image img, cellImage;

    protected SaveInfo sInfo;//Данные о профиле, которому соответствует данная кнопка
    public SaveInfo SInfo { get { return sInfo; } }

    #endregion //fields

    #region parametres

    Color color = new Color(0f, 100f/255f, 200f/255f);

    public override UIElementStateEnum ElementState
    {
        get
        {
            return base.ElementState;
        }

        set
        {
            base.ElementState = value;
            switch (value)
            {
                case UIElementStateEnum.inactive:
                    cellImage.color = new Color(inactiveIntensity, inactiveIntensity, inactiveIntensity, 1f);
                    break;
                case UIElementStateEnum.active:
                    cellImage.color = new Color(activeIntensity, activeIntensity, activeIntensity, 1f);
                    break;
                case UIElementStateEnum.clicked:
                    cellImage.color = new Color(clickedIntensity, clickedIntensity, clickedIntensity, 1f);
                    break;
                default:
                    cellImage.color = new Color(inactiveIntensity, inactiveIntensity, inactiveIntensity, 1f);
                    break;
            }
        }
    }

    #endregion //parametres

    /// <summary>
    /// Инициализация кнопки профиля
    /// </summary>
    public void Initialize(SaveInfo _sInfo, LoadMenuScript sMenu)
    {
        saveName = transform.FindChild("SaveName").GetComponent<Text>();
        saveTime = transform.FindChild("SaveTime").GetComponent<Text>();
        img = transform.FindChild("Button").GetComponent<Image>();
        cellImage = GetComponent<Image>();
        transform.FindChild("Button").GetComponent<Button>().enabled=false;
        img.color = new Color(color.r, color.g, color.b, .25f);
        saveMenu = sMenu;

        sInfo = _sInfo;
        SetButton();
    }

    /// <summary>
    /// Активировать данную кнопку
    /// </summary>
    public override void Activate()
    {
        base.Activate();
        ChooseSave();
        SetActive();
    }

    /// <summary>
    /// Настроить кнопку в соответствии с данными о профиле
    /// </summary>
    public void SetButton()
    {
        if (sInfo != null)
        {
            saveName.text = sInfo.saveName!=string.Empty? sInfo.saveName:"Новое сохранение";
            saveTime.text = sInfo.saveTime;
        }
    }

    /// <summary>
    /// Функция выбора сохраняемых, либо загружаемых данных
    /// </summary>
    public void ChooseSave()
    {
        saveMenu.ChooseButton(this);
    }

    /// <summary>
    /// Подсветить кнопку в зависимости от её активированности
    /// </summary>
    /// <param name="activate"></param>
    public void SetImage(bool activate)
    {
        img.color = activate ? new Color(color.r, color.g, color.b, activateAlpha) : new Color(color.r, color.g, color.b, .25f);
    }

}
