using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Слот, в котором находится ингредиент для смешивания
/// </summary>
public class MixtureSlotScript : MonoBehaviour, IDropHandler
{

    #region fields

    private Image ingredientImage, buttonImage;
    private AlchemyWindow aWindow;

    public static MixtureSlotScript activeMixtureSlot = null;
    private ItemClass ingredient;
    public ItemClass Ingredient
    {
        get
        {
            return ingredient;
        }
        set
        {
            ItemClass _ingredient = value;
            //Проверка на то, что в другом слоте для смешивания нет такого же ингредиента
            if (first ? (aWindow.MixIngredient2 != null && _ingredient!=null ? aWindow.MixIngredient2.itemName == _ingredient.itemName : false) :
                        (aWindow.MixIngredient1 != null && _ingredient != null ? aWindow.MixIngredient1.itemName == _ingredient.itemName : false))
            {
                AlchemyWindow.ResetActiveSlots();
                return;
            }
            ingredient = value;
            if (first)
                aWindow.MixIngredient1 = value;
            else
                aWindow.MixIngredient2 = value;
            if (value != null)
            {
                ingredientImage.sprite = value.itemImage;
                ingredientImage.color = Color.white;
            }
            else
            {
                ingredientImage.sprite = null;
                ingredientImage.color = new Color(0f, 0f, 0f, 0f);
            }
            AlchemyWindow.ResetActiveSlots();
        }
    }

    #endregion //fields

    #region parametres

    private bool activated;
    public bool Activated
    {
        set
        {
            if (activeMixtureSlot)
                activeMixtureSlot.SetActive(false);
            activated = value;
            if (value)
            {
                activeMixtureSlot = this;
                buttonImage.color = new Color(1f, 1f, 0f, .4f);
            }
            else
            {
                activeMixtureSlot = null;
                buttonImage.color = new Color(0f, 0f, 0f, 0f);
            }
        }
    }

    private bool first = false;

    #endregion //parametres

    public void Initialize(AlchemyWindow _aWindow)
    {
        first= transform.parent.gameObject.name.Contains("1");
        aWindow = _aWindow;
        ingredientImage = transform.parent.FindChild("IngredientImage").GetComponent<Image>();
        buttonImage = GetComponent<Image>();
        SetActive(false);
    }

    /// <summary>
    /// Установить слот активным/неактивным
    /// </summary>
    public void SetActive(bool _activated)
    {
        activated = _activated;
        buttonImage.color = _activated ? new Color(1f, 1f, 0f, .4f) : new Color(0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Включить/выключить режим выбора ингредиента
    /// </summary>
    public void SetActive()
    {
        Activated = !activated;
        if (activated && IngredientSlotScript.activeIngredientSlot)
        {
            Ingredient = IngredientSlotScript.activeIngredientSlot.Ingredient;
        }
    }

    /// <summary>
    /// Что происходит, когда на панель смешивания кидают ингредиент
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        ItemClass _ingredient = IngredientDragHandler.currentIngredient;
        if (_ingredient==null)
            return;
        //Проверка на то, что в другом слоте для смешивания нет такого же ингредиента
        if (first ? (aWindow.MixIngredient2!=null? aWindow.MixIngredient2.itemName == _ingredient.itemName:false) : 
                    (aWindow.MixIngredient1!=null? aWindow.MixIngredient1.itemName == _ingredient.itemName:false))
            return;

        Ingredient = _ingredient;
    }

}
