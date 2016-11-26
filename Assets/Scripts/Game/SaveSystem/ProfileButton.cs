using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Скрипт кнопки профиля игрока
/// </summary>
public class ProfileButton : MonoBehaviour
{

    #region consts

    protected const float activateAlpha = .5f;

    #endregion //consts

    #region fields

    protected LoadMenuScript saveMenu;//Экран сохранения игры, с которым согласуется работа

    protected Text saveName, saveTime;
    public string SaveName { get { return saveName.text; } }
    protected Image img;

    protected SaveInfo sInfo;//Данные о профиле, которому соответствует данная кнопка
    public SaveInfo SInfo { get { return sInfo; } }

    #endregion //fields

    #region parametres

    Color color = new Color(1f, .6f, .2f);

    #endregion //parametres

    /// <summary>
    /// Инициализация кнопки профиля
    /// </summary>
    public void Initialize(SaveInfo _sInfo, LoadMenuScript sMenu)
    {
        saveName = transform.FindChild("SaveName").GetComponent<Text>();
        saveTime = transform.FindChild("SaveTime").GetComponent<Text>();
        img = transform.FindChild("Button").GetComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0f);
        saveMenu = sMenu;

        sInfo = _sInfo;
        SetButton();
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
        img.color = activate ? new Color(color.r, color.g, color.b, activateAlpha) : new Color(color.r, color.g, color.b, 0f);
    }

}
