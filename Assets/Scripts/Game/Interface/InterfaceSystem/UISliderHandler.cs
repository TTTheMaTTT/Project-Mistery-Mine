using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт, который реализует взаимодействие со слайдером - элементом универсального UI
/// </summary>
public class UISliderHandler : UIElementScript
{

    #region const

    protected const float inactiveIntensity = 1f, activeIntensity = .8f, clickedIntensity = .6f;//Как будет подкрашиваться кнопка при различных уровнях взаимодействия с ней

    #endregion const

    #region fields

    protected Image img;//Изображение хэндлера

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
    }

}
