using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanel : UIElementScript
{

    #region fields

    public UIElementScript parentElement;//Панель, что содержит данную панель
    public List<UIElementScript> childElements=new List<UIElementScript>();//Какие элементы UI являются дочерними по отношению к данному

    #endregion //fields

    #region parametres

    protected UIElementIndex currentIndex = new UIElementIndex(-1, -1);//Текущий индекс, на котором находится указатель панели

    #endregion //parametres

    public override void Initialize()
    {
        base.Initialize();
        foreach (UIElementScript childElement in childElements)
            if (childElement is UIPanel)
                ((UIPanel)childElement).parentElement = this;
    }

    /// <summary>
    /// Активировать объект интерфейса
    /// </summary>
    public override void SetActive()
    {
        activePanel = this;
        UIElementScript element = FindElement(new UIElementIndex(0, 0));
        if (element != null)
        {
            currentIndex = new UIElementIndex(0, 0);
            element.SetActive();
        }
    }

    /// <summary>
    /// Отмена
    /// </summary>
    public override void Cancel()
    {
        if (parentElement != null)
            parentElement.Cancel();
    }



    /// <summary>
    /// Переместиться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">знак движения</param>
    public override void MoveHorizontal(int direction)
    {
        UIElementScript element = FindElement(new UIElementIndex(currentIndex.indexX+direction, currentIndex.indexY));
        if (element != null)
        {
            currentIndex = new UIElementIndex(currentIndex.indexX + direction, currentIndex.indexY);
            element.SetActive();
        }
        else if (parentElement != null)
            parentElement.MoveHorizontal(direction, uiIndex);
        else
        {
            element = FindElement(new UIElementIndex(currentIndex.indexX, currentIndex.indexY));
            if (element != null)
            {
                currentIndex = new UIElementIndex(currentIndex.indexX, currentIndex.indexY);
                element.SetActive();
            }
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
            activePanel = this;
            currentIndex = new UIElementIndex(_index.indexX + direction, _index.indexY);
            element.SetActive();
        }
        else if (parentElement != null)
            parentElement.MoveHorizontal(direction, uiIndex);
        else
        {
            element = FindElement(new UIElementIndex(currentIndex.indexX, currentIndex.indexY));
            if (element != null)
            {
                currentIndex = new UIElementIndex(currentIndex.indexX, currentIndex.indexY);
                element.SetActive();
            }
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
        else if (parentElement != null)
            parentElement.MoveVertical(direction, uiIndex);
        else
        {
            element = FindElement(new UIElementIndex(currentIndex.indexX, currentIndex.indexY));
            if (element != null)
            {
                currentIndex = new UIElementIndex(currentIndex.indexX, currentIndex.indexY);
                element.SetActive();
            }
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
            activePanel = this;
            currentIndex = new UIElementIndex(_index.indexX, _index.indexY + direction);
            element.SetActive();
        }
        else if (parentElement != null)
            parentElement.MoveVertical(direction, uiIndex);
        else
        {
            element = FindElement(new UIElementIndex(currentIndex.indexX, currentIndex.indexY));
            if (element != null)
            {
                currentIndex = new UIElementIndex(currentIndex.indexX, currentIndex.indexY);
                element.SetActive();
            }
        }
    }

    /// <summary>
    /// Найти элемент с заданным индексом
    /// </summary>
    /// <param name="_index">Искомый индекс</param>
    protected virtual UIElementScript FindElement(UIElementIndex _index)
    {
        return childElements.Find(x => x.uiIndex == _index);
    }

}
