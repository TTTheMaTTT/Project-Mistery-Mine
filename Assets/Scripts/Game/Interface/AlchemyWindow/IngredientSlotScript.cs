using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт слота с ингридиентами
/// </summary>
public class IngredientSlotScript : MonoBehaviour
{

    #region fields

    public static IngredientSlotScript activeIngredientSlot;

    private Image ingredientImage, buttonImage;
    private IngredientDragHandler dragHandler;
    private Button button;
    private AlchemyWindow aWindow;

    protected ItemClass ingredient;//Какому ингредиенту (предмету) соответствует данный слот
    public ItemClass Ingredient
    {
        get
        {
            return ingredient;
        }
        set
        {
            ingredient = value;
            buttonImage.color = new Color(0f, 0f, 0f, 0f);
            dragHandler.Ingredient = value;
            if (value != null)
            {
                dragHandler.enabled = true;
                ingredientImage.sprite = value.itemImage;
                ingredientImage.color = Color.white;
                button.enabled = true;
            }
            else
            {
                dragHandler.enabled = false;
                ingredientImage.sprite = null;
                ingredientImage.color = new Color(0f, 0f, 0f, 0f);
                button.enabled = false;
            }
        }
    }

    #endregion //fields

    #region parametres

    private bool activated;
    public bool Activated
    {
        set
        {
            if (activeIngredientSlot)
                activeIngredientSlot.SetActive(false);
            activated = value;
            if (value)
            {
                activeIngredientSlot = this;
                buttonImage.color = new Color(1f, 1f, 0f, .4f);
            }
            else
            {
                activeIngredientSlot = null;
                buttonImage.color = new Color(0f, 0f, 0f, 0f);
            }
        }
    }

    #endregion //parametres

    public void Initialize(AlchemyWindow _aWindow)
    {
        aWindow = _aWindow;
        Transform imageTrans = transform.parent.FindChild("IngredientImage");
        ingredientImage = imageTrans.GetComponent<Image>();
        dragHandler = imageTrans.GetComponent<IngredientDragHandler>();
        dragHandler.Ingredient = ingredient;
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        SetActive(false);
    }

    /// <summary>
    /// Выбрать ингредиент
    /// </summary>
    public void ChooseIngredient()
    {
        Activated = !activated;
        if (activated && MixtureSlotScript.activeMixtureSlot)
            MixtureSlotScript.activeMixtureSlot.Ingredient = ingredient;
    }

    /// <summary>
    /// Сделать слот активным/неактивным
    /// </summary>
    /// <param name="_activated"></param>
    public void SetActive(bool _activated)
    {
        activated = _activated;
        buttonImage.color = _activated ? new Color(1f, 1f, 0f, .4f) : new Color(0f,0f,0f,0f);
    }

}
