using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт, реализующий ячейку с предметом (обычным предметом, который исопользуется для квестов, либо это ключ от двери)
/// </summary>
public class ItemCell : UIElementScript
{

    #region consts

    protected const float inactiveIntensity = 1f, activeIntensity = .5f, clickedIntensity = .3f;//Как будет подкрашиваться кнопка при различных уровнях взаимодействия с ней

    #endregion //consts

    #region fields

    public Image itemImage;

    private Image cellImage;

    private ItemClass item;
    public ItemClass Item
    {
        get
        {
            return item;
        }
        set
        {
            item = value;
            itemImage.sprite = value != null ? value.itemImage : null;
            itemImage.enabled = value!=null;
            itemImage.color = Color.white;
        }
    }

    public static Text itemNameText;

    #endregion //fields

    #region parametres

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

    public override void SetActive()
    {
        base.SetActive();
        ShowItemText();
    }

    public override void Initialize()
    {
        itemImage = GetComponent<Image>();
        cellImage = transform.parent.GetComponent<Image>();
    }

    /// <summary>
    /// Отобразить текст с названием предмета
    /// </summary>
    public void ShowItemText()
    {
        if (item != null)
            itemNameText.text = item.itemMLTextName.GetText(SettingsScript.language);
    }

}
