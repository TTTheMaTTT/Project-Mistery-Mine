using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Алхимический стол, за которым можно смешивать ингредиенты и создавать зелья
/// </summary>
public class AlchemyLab : MonoBehaviour, IInteractive
{

    #region consts

    private const string usedPotionText = "Это зелье больше на вас никак не действует";

    #endregion //consts

    #region fields

    private AlchemyWindow aWindow;//Окно интерфейса, на котором происходит вся алхимия
    private NPCController dialogInteractor; //Через этот объект будут вызываться эффекты зелий
    [SerializeField]private List<PotionClass> potions = new List<PotionClass>();//Какие виды зелий можно создать на столе
    private SpriteRenderer sRenderer;

    #endregion //fields

    #region parametres

    [SerializeField][HideInInspector]private int id;
    private Color outlineColor = Color.yellow;
    private bool noInteract = false;

    #endregion //parametres

    void Awake()
    {
        aWindow = FindObjectOfType<AlchemyWindow>();
        dialogInteractor = GetComponentInChildren<NPCController>();
        sRenderer = GetComponent<SpriteRenderer>();

        noInteract = false;
    }

    /// <summary>
    /// Выпить зелье
    /// </summary>
    public void DrinkPotion(PotionClass potion)
    {
        if (dialogInteractor == null)
            return;
        if (potion.haveUsed)
            SpecialFunctions.SetSecretText(4f, usedPotionText);
        else
            dialogInteractor.StartDialog(potion.potionName);
        potion.haveUsed = true;
    }

    #region IHaveID

    public int GetID()
    {
        return id;
    }

    public void SetID(int _id)
    {
        id = _id;
    }

    /// <summary>
    /// Настраивает объект в соответствии с тем, как он был сохранён
    /// </summary>
    public void SetData(InterObjData _intObjData)
    {
        if (!(_intObjData is AlchemyLabData))
            return;
        AlchemyLabData aData = (AlchemyLabData)_intObjData;
        for (int i = 0; i < aData.usageList.Count && i < potions.Count; i++)
            potions[i].haveUsed = aData.usageList[i];
    }

    /// <summary>
    /// Возвращает данные для сохранения
    /// </summary>
    public InterObjData GetData()
    {
        return new AlchemyLabData(id, gameObject.name, potions);
    }

    #endregion //IHaveID

    #region IInteractive

    /// <summary>
    /// Взаимодействовать с объектом
    /// </summary>
    public void Interact()
    {
        if (aWindow != null && !noInteract)
        {
            aWindow.Potions = potions;
            aWindow.OpenWindow();
        }
    }

    /// <summary>
    /// Запустить процесс невзаимодействия
    /// </summary>
    public void NoInteract()
    {
        StartCoroutine(NoInteractProcess());
    }

    /// <summary>
    /// Отрисовать контур объекта, если с ним возможно произвести взаимодействие
    /// </summary>
    public void SetOutline(bool _outline)
    {
        if (sRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            sRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_Outline", _outline ? 1f : 0);
            mpb.SetColor("_OutlineColor", outlineColor);
            sRenderer.SetPropertyBlock(mpb);
        }
    }

    /// <summary>
    /// Можно ли взаимодействовать с объектом в данный момент?
    /// </summary>
    /// <returns></returns>
    public bool IsInteractive()
    {
        return SpecialFunctions.battleField.enemiesCount == 0 && !noInteract;
    }

    #endregion //IInteractive

    IEnumerator NoInteractProcess()
    {
        noInteract = true;
        yield return new WaitForSeconds(2f);
        noInteract = false;
    }

}

/// <summary>
/// Класс, описывающий состав и вид зелья
/// </summary>
[System.Serializable]
public class PotionClass
{
    public string potionName;//Название зелья
    public string potionTextName;//Название зелья в текстах игры
    public string ingredient1, ingredient2;//Ингредиенты
    public Sprite potionImage;//Изображение зелья
    public bool haveUsed = false;//Было ли зелье уже использовано

    public PotionClass()
    {
        potionName = "potion";
        potionTextName = "potion";
        ingredient1 = "ingredient1";
        ingredient2 = "ingredient2";
        haveUsed = false;
        potionImage = null;
    }

    /// <summary>
    /// Совпадают ли заданные ингредиенты с ингредиентами данного зелья?
    /// </summary>
    public bool CoincideIngredients(string _ingredient1, string _ingredient2)
    {
        return ingredient1 == _ingredient1 && ingredient2 == _ingredient2 || ingredient1 == _ingredient2 && ingredient2 == _ingredient1;
    }

}