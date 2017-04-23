using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Унифицированный элемент UI, с которым можно работать и спомощью джойстика, и с помощью клавиатуры с мышкой.
/// </summary>
public class UIElementScript : MonoBehaviour
{

    #region fields

    public static UIElementScript activeElement;//С каким именно элементом UI пользователь взаимодействует в данный момент?

    #endregion //fields

    #region parametres

    protected UIElementStateEnum elementState = UIElementStateEnum.inactive;
    public virtual UIElementStateEnum ElementState
    {
        get
        {
            return elementState;
        }
        set
        {
            if (value != UIElementStateEnum.inactive)
                if (activeElement != null && activeElement != this)
                {
                    activeElement.ElementState = UIElementStateEnum.inactive;
                    activeElement = this;
                }
            elementState = value;
        }
    }


    #endregion //parametres

    protected void Awake()
    {
        Initialize();
    }

    public virtual void Initialize()
    {

    }

    /// <summary>
    /// Сделать элемент неактивным в данный момент
    /// </summary>
    public virtual void SetInactive()
    {
        ElementState = UIElementStateEnum.inactive;
    }

    /// <summary>
    /// Сделать элемент активным в данный момент
    /// </summary>
    public virtual void SetActive()
    {
        ElementState = UIElementStateEnum.active;
    }

    /// <summary>
    /// Сделать элемент нажатым
    /// </summary>
    public virtual void SetClicked()
    {
        ElementState = UIElementStateEnum.clicked;
    }

    /// <summary>
    /// Активировать механизм, что кроется за данным элементом
    /// </summary>
    public virtual void Activate()
    {

    }

}

//Енам, описывающий состояние элемента UI
public enum UIElementStateEnum { inactive=0, active=1, clicked=2}
