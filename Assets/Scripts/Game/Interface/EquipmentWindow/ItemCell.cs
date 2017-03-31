using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт, реализующий ячейку с предметом (обычным предметом, который исопользуется для квестов, либо это ключ от двери)
/// </summary>
public class ItemCell : MonoBehaviour
{

    #region fields

    public Image itemImage;

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

    public void Initialize()
    {
        itemImage = GetComponent<Image>();
    }

    /// <summary>
    /// Отобразить текст с названием предмета
    /// </summary>
    public void ShowItemText()
    {
        if (item != null)
            itemNameText.text = item.itemTextName;
    }

}
