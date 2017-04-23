using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт слота с ингридиентами
/// </summary>
public class IngredientSlotScript : UIElementScript
{

    #region const

    protected const float inactiveIntensity = 1f, activeIntensity = .8f, clickedIntensity = .6f;//Как будет подкрашиваться кнопка при различных уровнях взаимодействия с ней

    #endregion const

    #region fields

    public static IngredientSlotScript activeIngredientSlot;

    private Image ingredientImage, buttonImage, cellImage;
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
                buttonImage.color = new Color(0f, .67f, .72f, .4f);
            }
            else
            {
                activeIngredientSlot = null;
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

    #endregion //parametres

    public void Initialize(AlchemyWindow _aWindow)
    {
        aWindow = _aWindow;
        Transform imageTrans = transform.parent.FindChild("IngredientImage");
        ingredientImage = imageTrans.GetComponent<Image>();
        dragHandler = imageTrans.GetComponent<IngredientDragHandler>();
        dragHandler.Ingredient = ingredient;
        button = GetComponent<Button>();
        buttonImage = transform.parent.FindChild("Image").GetComponent<Image>();
        GetComponent<Button>().enabled = false;
        cellImage = transform.parent.GetComponent<Image>();
        SetActive(false);
    }

    /// <summary>
    /// Активировать данную кнопку
    /// </summary>
    public override void Activate()
    {
        base.Activate();
        ChooseIngredient();
        SetActive();
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
