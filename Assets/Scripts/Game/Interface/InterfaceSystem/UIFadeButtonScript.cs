using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт кнопки, которая меняет цвет при взаимодействии с ней
/// </summary>
public class UIFadeButtonScript : UIElementScript
{

    #region const

    protected const float inactiveIntensity = 1f, activeIntensity = .5f, clickedIntensity = .3f;//Как будет подкрашиваться кнопка при различных уровнях взаимодействия с ней

    #endregion const

    #region fields

    protected Image img;//Изображение кнопки
    protected Button button;

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
                    img.color = new Color(inactiveIntensity, inactiveIntensity, inactiveIntensity, 1f);
                    break;
                case UIElementStateEnum.active:
                    img.color = new Color(activeIntensity, activeIntensity, activeIntensity, 1f);
                    break;
                case UIElementStateEnum.clicked:
                    img.color = new Color(clickedIntensity, clickedIntensity, clickedIntensity, 1f);
                    break;
                default:
                    img.color = new Color(inactiveIntensity, inactiveIntensity, inactiveIntensity, 1f);
                    break;
            }
        }
    }

    #endregion //parametres

    public override void Initialize()
    {
        base.Initialize();
        img = GetComponent<Image>();
        button = GetComponent<Button>();
        button.enabled = false;
    }

    /// <summary>
    /// Активировать данную кнопку
    /// </summary>
    public override void Activate()
    {
        SetActive();    
        base.Activate();
        button.onClick.Invoke();
    }

}
