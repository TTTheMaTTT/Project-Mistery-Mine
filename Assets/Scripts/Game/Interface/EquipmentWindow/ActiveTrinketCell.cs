using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Класс, реализующий ячейку, в которую можно положить тринкет тем самым добавив этот тринкет герою
/// </summary>
public class ActiveTrinketCell : UIElementScript, IDropHandler
{

    #region consts

    protected const float inactiveIntensity = 1f, activeIntensity = .8f, clickedIntensity = .6f;//Как будет подкрашиваться кнопка при различных уровнях взаимодействия с ней

    #endregion //consts

    #region fields

    public static ActiveTrinketCell activatedTrinketCell;

    private Image trinketImage, buttonImage, cellImage;

    private TrinketClass trinket;
    public TrinketClass Trinket
    {
        get
        {
            return trinket;
        }
        set
        {
            if (value == trinket)
                return;
            if (trinket != null)
                eMenu.TakeOffTrinket(trinket);
            trinket = value;
            if (value != null)
                eMenu.PutOnTrinket(trinket);
            bool haveTrinket = trinket != null;
            trinketImage.color = haveTrinket?Color.white:new Color(1f,1f,1f,0f);
            trinketImage.sprite = haveTrinket ? trinket.itemImage : null;
            eMenu.ResetActiveSlots();
        }
    }

    public static EquipmentMenu eMenu;
    public static Text trinketNameText;

    #endregion //fields

    #region parametres

    private bool activated;
    public bool Activated
    {
        set
        {
            if (activatedTrinketCell)
                activatedTrinketCell.SetActive(false);
            activated = value;
            eMenu.ShowRemoveTrinketButton(trinket != null && activated);
            activatedTrinketCell = value ? this : null;
            buttonImage.color = value ? new Color(0f, .66f, .72f, .4f) : new Color(0f, 0f, 0f, 0f);
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

    #endregion //parametres

    public override void Initialize()
    {
        base.Initialize();
        trinketImage = GetComponent<Image>();
        buttonImage = transform.parent.FindChild("Button").GetComponent<Image>();
        cellImage = transform.parent.GetComponent<Image>();
    }

    /// <summary>
    /// Установить тринкет в ячейку
    /// </summary>
    public void SetTrinket(TrinketClass _trinket)
    {
        trinket = _trinket;
        bool haveTrinket = trinket != null;
        trinketImage.sprite = haveTrinket ? trinket.itemImage : null;
        trinketImage.color = haveTrinket?Color.white: new Color(1f,1f,1f,0f);
    }

    /// <summary>
    /// Включить/выключить ячейку
    /// </summary>
    public void SetActive(bool _activated)
    {
        activated = _activated;
        buttonImage.color = _activated? new Color(1f, 1f, 0f, .4f) : new Color(0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Выбрать ячейку
    /// </summary>
    public override void Activate()
    {
        if (trinket != null ? trinket is MutagenClass : false)
            return;
        Activated = !activated;
        if (activated && TrinketCell.activeTrinketCell)
            Trinket = TrinketCell.activeTrinketCell.Trinket;
        ElementState = UIElementStateEnum.active;
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
    /// Что происходит, когда на ячейку тринкета кидают тринкет
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (trinket != null ? trinket is MutagenClass : false)
            return;
        TrinketClass _trinket = TrinketCell.draggedTrinket;
        if (_trinket == null)
            return;

        Trinket = _trinket;
    }

}
