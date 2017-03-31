using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Окно интерфейса, на котором происходят алхимические манипуляции
/// </summary>
public class AlchemyWindow : InterfaceWindow
{

    #region consts

    private const string unknownPotionText = "Неизвестное зелье";
    private const float drinkTime = 1f;

    #endregion //consts

    #region fields 

    private AlchemyLab lab;//Лаборатория, на которой происходит вся алхимия
    private List<PotionClass> potions=new List<PotionClass>();//Зелья, которые можно создать в данной лаборатории
    public List<PotionClass> Potions { set { potions = value; } }

    private List<IngredientSlotScript> ingredientSlots = new List<IngredientSlotScript>();//Ингредиенты
    private MixtureSlotScript mixSlot1, mixSlot2;//Слоты смешиваемых ингредиентов

    private Image potionImage;//Изображение зелья
    private Text potionNameText;//Текст с названием зелья
    public Sprite emptyFlaskImage;//Изображение пустой фляги с зельем

    private GameObject mixPotionButtonObject, drinkPotionButtonObject;//Кнопки, которые вызывают смешивание и выпивание зелья

    private PotionClass currentPotion;//Приготовленное зелье
    private PotionClass CurrentPotion
    {
        get
        {
            return currentPotion;
        }
        set
        {
            currentPotion = value;
            if (value!=null)
            {
                potionImage.sprite = value.potionImage;
                potionNameText.text = value.haveUsed? value.potionTextName:unknownPotionText;
                drinkPotionButtonObject.SetActive(true);
            }
            else
            {
                potionImage.sprite = emptyFlaskImage;
                potionNameText.text = "";
                drinkPotionButtonObject.SetActive(false);
            }
        }
    }

    #endregion //fields

    #region parametres

    private ItemClass mixIngredient1, mixIngredient2;//Смешиваемые ингредиенты
    public ItemClass MixIngredient1 { get { return mixIngredient1; } set { mixIngredient1 = value; mixPotionButtonObject.SetActive(mixIngredient1 != null && mixIngredient2 != null); } }
    public ItemClass MixIngredient2 { get { return mixIngredient2; } set { mixIngredient2 = value; mixPotionButtonObject.SetActive(mixIngredient1 != null && mixIngredient2 != null); } }

    #endregion //parametres

    protected override void Awake()
    {
        base.Awake();
        lab = FindObjectOfType<AlchemyLab>();
        potions = new List<PotionClass>();

        Transform panel = transform.FindChild("Panel");

        mixPotionButtonObject = panel.Find("MixButton").gameObject;
        drinkPotionButtonObject = panel.Find("DrinkButton").gameObject;

        ingredientSlots = new List<IngredientSlotScript>();
        Transform ingredientSlotsTrans = panel.FindChild("IngredientSlots");
        for (int i = 0; i < ingredientSlotsTrans.childCount; i++)
        {
            IngredientSlotScript iScript= ingredientSlotsTrans.GetChild(i).GetComponentInChildren<IngredientSlotScript>();
            iScript.Initialize(this);
            ingredientSlots.Add(ingredientSlotsTrans.GetChild(i).GetComponentInChildren<IngredientSlotScript>());
        }

        mixSlot1 = panel.FindChild("MixtureSlot1").GetComponentInChildren<MixtureSlotScript>(); mixSlot1.Initialize(this); mixSlot1.Ingredient = null;
        mixSlot2 = panel.FindChild("MixtureSlot2").GetComponentInChildren<MixtureSlotScript>(); mixSlot2.Initialize(this); mixSlot2.Ingredient = null;

        potionImage = panel.FindChild("PotionImage").GetComponent<Image>();
        potionNameText = potionImage.transform.FindChild("PotionName").GetComponent<Text>();

        MixIngredient1 = null;
        MixIngredient2 = null;
        CurrentPotion = null;
        ResetActiveSlots();
    }

    /// <summary>
    /// Открыть окно интерфейса
    /// </summary>
    public override void OpenWindow()
    {
        base.OpenWindow();
        EquipmentClass equip = SpecialFunctions.player.GetComponent<HeroController>().Equipment;
        ItemClass[] ingredients = new ItemClass[]{ equip.GetItem("Ambrosia"), equip.GetItem("RootkindPlant"), equip.GetItem("YellowLeaf"), equip.GetItem("EssorRoot")};
        if (ingredientSlots.Count != 4)
            return;
        for (int i = 0; i < ingredientSlots.Count; i++)
            ingredientSlots[i].Ingredient = ingredients[i];
    }

    /// <summary>
    /// Закрыть окно интерфейса
    /// </summary>
    public override void CloseWindow()
    {
        base.CloseWindow();

        mixSlot1.Ingredient = null;
        mixSlot2.Ingredient = null;
        MixIngredient1 = null;
        MixIngredient2 = null;
        CurrentPotion = null;
        ResetActiveSlots();
    }

    /// <summary>
    /// Сделать все активные слоты окна алхимии снова неактивными 
    /// </summary>
    public static void ResetActiveSlots()
    {
        if (MixtureSlotScript.activeMixtureSlot)
            MixtureSlotScript.activeMixtureSlot.Activated = false;
        if (IngredientSlotScript.activeIngredientSlot)
            IngredientSlotScript.activeIngredientSlot.Activated = false;
    }

    /// <summary>
    /// Смешать ингредиенты и получить новое зелье
    /// </summary>
    public void MixIngredients()
    {
        if (mixIngredient1 == null || mixIngredient2 == null || potions==null)
            return;
        PotionClass newPotion = potions.Find(x => x.CoincideIngredients(mixIngredient1.itemName, mixIngredient2.itemName));
        if (newPotion == null)
            return;
        CurrentPotion = newPotion;
    }

    /// <summary>
    /// Выпить приготовленное зелье
    /// </summary>
    public void DrinkPotion()
    {
        lab.NoInteract();
        if (currentPotion == null)
            return;
        StartCoroutine(DrinkProcess(currentPotion));
        CloseWindow();
    }

    IEnumerator DrinkProcess(PotionClass _potion)
    {
        yield return new WaitForSeconds(drinkTime);
        lab.DrinkPotion(_potion);
    }


}
