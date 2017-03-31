using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Скрипт, реализующий поведение кнопки с тринкетом
/// </summary>
public class TrinketCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    #region fields

    public static TrinketCell activeTrinketCell;//Клетка тринкета, что выделена в данный момент
    public static TrinketClass draggedTrinket;//Подхваченный тринкет

    private Image trinketImage, buttonImage;

    private TrinketClass trinket;//Какому предмету соответсвует данная ячейка
    public TrinketClass Trinket
    {
        get
        {
            return trinket;
        }
        set
        {
            trinket = value;
            buttonImage.color = new Color(0f, 0f, 0f, 0f);
            if (value != null)
            {
                trinketImage.sprite = value.itemImage;
                trinketImage.enabled = true;
                trinketImage.color = Color.white;
            }
            else
            {
                trinketImage.sprite = null;
                trinketImage.enabled = false;
            }
        }
    }

    public static Text trinketNameText;
    public static Camera cam;

    #endregion //fields

    #region parametres

    private bool activated;//Включена ли кнопка, соответствующая ячейке?
    public bool Activated
    {
        set
        {
            if (activeTrinketCell)
                activeTrinketCell.SetActive(false);
            activated = value;
            if (value)
            {
                activeTrinketCell = this;
                buttonImage.color = new Color(1f, 1f, 0f, .4f);
            }
            else
            {
                activeTrinketCell = null;
                buttonImage.color = new Color(0f, 0f, 0f, 0f);
            }
        }
    }

    private Vector3 startPosition;//Начальное положение ещё неподхваченного предмета

    #endregion //parametres

    public void Initialize()
    {
        trinketImage = GetComponent<Image>();
        buttonImage = transform.parent.FindChild("Button").GetComponent<Image>();
        SetActive(false);
    }

    public void SetActive(bool _activated)
    {
        activated = _activated;
        buttonImage.color = _activated ? new Color(1f, 1f, 0f, .4f) : new Color(0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Выбор ячейки
    /// </summary>
    public void ChooseTrinket()
    {
        if (trinket == null)
            return;
        Activated = !activated;
        if (activated && ActiveTrinketCell.activatedTrinketCell)
            ActiveTrinketCell.activatedTrinketCell.Trinket = trinket;
    }

    /// <summary>
    /// Отобразить текст с названием предмета
    /// </summary>
    public void ShowTrinketText()
    {
        if (trinket != null)
            trinketNameText.text = trinket.itemTextName;
    }

    /// <summary>
    /// Действия при начале захвата объекта
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (trinket == null)
            return;
        draggedTrinket = trinket;
        startPosition = transform.position;
        cam = SpecialFunctions.CamController.GetComponent<Camera>();
    }

    /// <summary>
    /// Функция, вызываемая при удержании объекта
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (trinket == null)
            return;
        //transform.position = eventData.position;
        Vector2 mousePos = Input.mousePosition;
        Vector3 mouseWorldPos = cam.ScreenPointToRay(mousePos).origin;
        mouseWorldPos.z = startPosition.z;
        transform.position = mouseWorldPos;
    }

    /// <summary>
    /// Окончание удержания объекта
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        draggedTrinket = null;
        transform.position = startPosition;
    }

}
