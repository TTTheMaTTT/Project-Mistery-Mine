using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Скрипт, реализующий поведение кнопки с тринкетом
/// </summary>
public class TrinketCell : UIElementScript, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    #region consts

    protected const float inactiveIntensity = 1f, activeIntensity = .5f, clickedIntensity = .3f;//Как будет подкрашиваться кнопка при различных уровнях взаимодействия с ней

    #endregion //consts

    #region fields

    public static TrinketCell activeTrinketCell;//Клетка тринкета, что выделена в данный момент
    public static TrinketClass draggedTrinket;//Подхваченный тринкет

    private Image trinketImage, buttonImage,cellImage;

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
                buttonImage.color = new Color(0f, 0.67f, 0.72f, .4f);
            }
            else
            {
                activeTrinketCell = null;
                buttonImage.color = new Color(0f, 0f, 0f, 0f);
            }
        }
    }

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
                    cellImage.color = new Color(inactiveIntensity, inactiveIntensity, inactiveIntensity, 1f);
                    break;
                case UIElementStateEnum.active:
                    cellImage.color = new Color(activeIntensity, activeIntensity, activeIntensity, 1f);
                    break;
                case UIElementStateEnum.clicked:
                    cellImage.color = new Color(clickedIntensity, clickedIntensity, clickedIntensity, 1f);
                    break;
                default:
                    cellImage.color = new Color(inactiveIntensity, inactiveIntensity, inactiveIntensity, 1f);
                    break;
            }
        }
    }

    private Vector3 startPosition;//Начальное положение ещё неподхваченного предмета
    private Transform startParentObject;

    #endregion //parametres

    public override void Initialize()
    {
        trinketImage = GetComponent<Image>();
        buttonImage = transform.parent.FindChild("Button").GetComponent<Image>();
        cellImage = transform.parent.GetComponent<Image>();
        SetActive(false);
    }

    public void SetActive(bool _activated)
    {
        activated = _activated;
        buttonImage.color = _activated ? new Color(1f, 1f, 0f, .4f) : new Color(0f, 0f, 0f, 0f);
    }

    public override void SetActive()
    {
        base.SetActive();
        ShowTrinketText();
    }

    /// <summary>
    /// Провзаимодействовать с ячейкой, как с элементом UI
    /// </summary>
    public override void Activate()
    {
        ChooseTrinket();
        SetActive();
        SpecialFunctions.PlaySound("Button");
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
            trinketNameText.text = trinket.itemMLTextName.GetText(SettingsScript.language);
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
        startParentObject = transform.parent;
        transform.SetParent(transform.parent.parent.parent);
        transform.SetAsLastSibling();
        trinketImage.raycastTarget = false;
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
        trinketImage.raycastTarget = true;
        transform.position = startPosition;
        transform.SetParent(startParentObject);
        transform.SetAsFirstSibling();
    }

}
