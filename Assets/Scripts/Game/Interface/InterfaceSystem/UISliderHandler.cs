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

    protected const float inactiveIntensity = 1f, activeIntensity = .5f, clickedIntensity = .3f;//Как будет подкрашиваться кнопка при различных уровнях взаимодействия с ней

    #endregion const

    #region fields

    protected Image img;//Изображение хэндлера
    protected Slider slider;

    public UIPanel parentElement;

    #endregion //fields

    #region parametres

    protected bool moveable=false;//Находится ли слайдер в режиме перемещения
    public float delta;//Какое приращение к значению слайдера произойдёт при использовании стрелок джойстика

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
        slider = transform.parent.parent.GetComponent<Slider>();
        moveable = false;
    }

    /// <summary>
    /// Активировать слайдер
    /// </summary>
    public override void Activate()
    {
        SetClicked();
        moveable = true;
        activePanel = this;
    }

    /// <summary>
    /// Сброс
    /// </summary>
    public override void Cancel()
    {
        SetActive();
        activePanel = parentElement;
    }

    /// <summary>
    /// Горизонтальное перемещение
    /// </summary>
    /// <param name="direction">Знак направления</param>
    public override void MoveHorizontal(int direction)
    {
        slider.value = Mathf.Clamp(slider.value + direction * delta, 0f, 1f);
        slider.onValueChanged.Invoke(slider.value);
    }

    /// <summary>
    /// Горизонтальное перемещение
    /// </summary>
    public override void MoveHorizontal(int direction, UIElementIndex _index)
    {
        slider.value = Mathf.Clamp(slider.value + direction * delta, 0f, 1f);
        slider.onValueChanged.Invoke(slider.value);
    }

}
