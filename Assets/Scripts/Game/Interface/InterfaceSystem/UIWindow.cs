using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Окно с интерфейсом
/// </summary>
public class UIWindow : UIPanel
{

    /// <summary>
    /// Сброс
    /// </summary>
    public override void Cancel()
    {
        gameObject.SetActive(false);
        if (parentElement!=null)
            parentElement.SetActive();
    }

    /// <summary>
    /// Переместиться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">знак движения</param>
    public override void MoveHorizontal(int direction)
    {
        UIElementScript element = FindElement(new UIElementIndex(currentIndex.indexX + direction, currentIndex.indexY));
        if (element != null)
        {
            currentIndex = new UIElementIndex(currentIndex.indexX + direction, currentIndex.indexY);
            element.SetActive();
        }
    }

    /// <summary>
    /// Переместиться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">знак движения</param>
    /// <param name="_index">индекс элемента, с которого происходит движение</param>
    public override void MoveHorizontal(int direction, UIElementIndex _index)
    {
        UIElementScript element = FindElement(new UIElementIndex(_index.indexX + direction, _index.indexY));
        if (element != null)
        {
            currentIndex = new UIElementIndex(_index.indexX + direction, _index.indexY);
            element.SetActive();
        }
    }

    /// <summary>
    /// Переместиться в вертикальном направлении
    /// </summary>
    /// <param name="direction">Знак направления - вниз - положительное направление</param>
    public override void MoveVertical(int direction)
    {
        UIElementScript element = FindElement(new UIElementIndex(currentIndex.indexX, currentIndex.indexY + direction));
        if (element != null)
        {
            currentIndex = new UIElementIndex(currentIndex.indexX, currentIndex.indexY + direction);
            element.SetActive();
        }
    }

    /// <summary>
    /// Переместиться в вертикальном направлении
    /// </summary>
    /// <param name="direction">Знак направления (вниз - положительное направление)</param>
    /// <param name="_index">индекс элемента, с которого продолжается движение</param>
    public override void MoveVertical(int direction, UIElementIndex _index)
    {
        UIElementScript element = FindElement(new UIElementIndex(_index.indexX, _index.indexY + direction));
        if (element != null)
        {
            currentIndex = new UIElementIndex(_index.indexX, _index.indexY + direction);
            element.SetActive();
        }
    }

}
