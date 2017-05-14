using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт, управляющий кнопкой, у которой есть изображение
/// </summary>
public class UIImageButtonScript : UIElementScript
{

    #region fields

    [SerializeField] protected Sprite inactiveButtonImage, activeButtonImage, clickedButtonImage;//Изображения кнопки в различных состояниях
    protected Image img;//Изображение кнопки
    protected Button button;//Сама кнопка

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
                    img.sprite = inactiveButtonImage;
                    break;
                case UIElementStateEnum.active:
                    img.sprite = activeButtonImage;
                    break;
                case UIElementStateEnum.clicked:
                    img.sprite = clickedButtonImage;
                    break;
                default:
                    img.sprite = inactiveButtonImage;
                    break;
            }
        }
    }

    #endregion //parametres

    public override void Initialize()
    {
        base.Initialize();
        img=GetComponent<Image>();
        button = GetComponent<Button>();
        button.enabled = false;
    }

    /// <summary>
    /// Активировать данную кнопку
    /// </summary>
    public override void Activate()
    {
        SpecialFunctions.PlaySound("Button");
        SetActive();
        base.Activate();
        button.onClick.Invoke();
    }

}
