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
    public static UIElementScript activePanel;//С какой панелью элементов UI пользователь взаимодействует в данный момент?

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
                if (activeElement != this)
                {
                    if (activeElement != null)
                        activeElement.ElementState = UIElementStateEnum.inactive;
                    activeElement = this;
                }
            elementState = value;
        }
    }

    public UIElementIndex uiIndex = new UIElementIndex(0, 0);//Индекс UI-элемента

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

    /// <summary>
    /// Двинуться в горизонтальном направлении
    /// </summary>
    public virtual void MoveHorizontal(int direction)
    {
    }

    /// <summary>
    /// Двинуться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    /// <param name="_index">индекс дочернего элемента, из которого запрашивается движения</param>
    public virtual void MoveHorizontal(int direction, UIElementIndex _index)
    {
    }

    /// <summary>
    /// Двинуться в горизонтальном направлении
    /// </summary>
    public virtual void MoveVertical(int direction)
    {
    }

    /// <summary>
    /// Двинуться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    /// <param name="_index">индекс дочернего элемента, из которого запрашивается движения</param>
    public virtual void MoveVertical(int direction, UIElementIndex _index)
    {
    }

    /// <summary>
    /// Отмена
    /// </summary>
    public virtual void Cancel()
    {
    }

}

//Енам, описывающий состояние элемента UI
public enum UIElementStateEnum { inactive=0, active=1, clicked=2}

/// <summary>
/// Индекс элемента интерфейса
/// </summary>
[System.Serializable]
public struct UIElementIndex
{
    public int indexX, indexY;

    public UIElementIndex(int _indexX, int _indexY)
    {
        indexX = _indexX;
        indexY = _indexY;
    }

    public static bool operator ==(UIElementIndex e1, UIElementIndex e2)
    {
        return e1.indexX == e2.indexX && e1.indexY == e2.indexY;
    }

    public static bool operator !=(UIElementIndex e1, UIElementIndex e2)
    {
        return e1.indexX != e2.indexX || e1.indexY != e2.indexY;
    }


}