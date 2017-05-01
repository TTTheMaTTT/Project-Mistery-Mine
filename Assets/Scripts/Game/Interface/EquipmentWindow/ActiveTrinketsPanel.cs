using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveTrinketsPanel : UIPanel
{

    #region fields

    protected List<UIElementScript> activeChildElements = new List<UIElementScript>();

    #endregion //fields

    /// <summary>
    /// Определить, какие из дочерних элементов впринципе могут быть активированы
    /// </summary>
    public void SetActiveChildElements()
    {
        activeChildElements = new List<UIElementScript>();
        for (int i = 0; i < childElements.Count; i++)
            if (childElements[i].gameObject.active)
                activeChildElements.Add(childElements[i]);
    }

    public override void SetActive()
    {
        SetActiveChildElements();
        base.SetActive();
    }

    /// <summary>
    /// Найти элемент с заданным индексом
    /// </summary>
    /// <param name="_index">Искомый индекс</param>
    protected override UIElementScript FindElement(UIElementIndex _index)
    {
        return activeChildElements.Find(x => x.uiIndex == _index);
    }

}
