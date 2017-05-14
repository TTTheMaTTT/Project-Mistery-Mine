﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, представляющий собой дверь
/// </summary>
public class DoorClass : MonoBehaviour, IInteractive, IMechanism, IHaveStory
{

    #region fields

    [SerializeField]protected string keyID;//Название ключа, что откроет эту дверь
    [SerializeField]protected MultiLanguageText closedDoorMessage = new MultiLanguageText("Для того чтобы открыть эту дверь тебе нужен ключ - найди его!",
                                                                                          "You need the key to open that door",
                                                                                           "Для того, щоб відчинити ці двері, тобі потрібен ключ -- знайди його!",
                                                                                           "Potrzebujesz klucza żeby otworzyć te drzwi",
                                                                                           "Pour ouvrir cette porte tu as besoin d'une clé - trouve-le!"),
                                                          openedDoorMessage = new MultiLanguageText("Дверь открыта",
                                                                                                    "The door is opened",
                                                                                                    "Двері відкрито",
                                                                                                    "Drzwi są otwarte",
                                                                                                    "La porte est ouverte");//Какое сообщение должно выводится при различных попытках открыть дверь

    protected Collider2D col;
    protected Animator anim;
    protected SpriteRenderer sRenderer;
    protected AnimatedSoundManager soundManager;

    #endregion //fields

    #region parametres

    [SerializeField]
    [HideInInspector]
    protected int id;

    protected Color outlineColor = Color.yellow;

    #endregion //parametres

    protected virtual void Awake()
    {
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        sRenderer = GetComponent<SpriteRenderer>();
        soundManager = GetComponent<AnimatedSoundManager>();
    }

    /// <summary>
    /// Открыть дверь
    /// </summary>
    public virtual void Open()
    {
        if (col != null)
            col.enabled = false;
        SpecialFunctions.SetText(1.5f, openedDoorMessage);
        if (anim != null)
        {
            anim.Play("Opened");
        }
        SetOutline(false);
        if (soundManager) soundManager.PlaySound("Open");
    }

    /// <summary>
    /// Активировать механизм
    /// </summary>
    public virtual void ActivateMechanism()
    {
        Open();
    }

    #region IInteractive

    /// <summary>
    /// Провести взаимодействие с дверью
    /// </summary>
    public virtual void Interact()
    {
        HeroController player = SpecialFunctions.Player.GetComponent<HeroController>();
        if (keyID == string.Empty)
            Open();
        else if (player.Equipment.bag.Find(x => x.itemName == keyID))
            Open();
        else
            SpecialFunctions.SetText(2.5f, closedDoorMessage);
    }

    /// <summary>
    /// Отрисовать контур, если происзодит взаимодействие (или убрать этот контур)
    /// </summary>
    public virtual void SetOutline(bool _outline)
    {
        if (sRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            sRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_Outline", _outline ? 1f : 0);
            mpb.SetColor("_OutlineColor", outlineColor);
            mpb.SetFloat("_OutlineWidth", .08f / ((Vector2)transform.lossyScale).magnitude);
            sRenderer.SetPropertyBlock(mpb);
        }
    }

    /// <summary>
    /// Можно ли провзаимодействовать с объектом в данный момент?
    /// </summary>
    public virtual bool IsInteractive()
    {
        return true;
    }

    #endregion //IInteractive

    #region IHaveID

    /// <summary>
    /// Вернуть id
    /// </summary>
    public virtual int GetID()
    {
        return id;
    }

    /// <summary>
    /// Выставить id объекту
    /// </summary>
    public virtual void SetID(int _id)
    {
        id = _id;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif //UNITY_EDITOR
    }

    /// <summary>
    /// Загрузить данные о двери 
    /// </summary>
    public virtual void SetData(InterObjData _intObjData)
    {
        DoorData dData = (DoorData)_intObjData;
        if (dData != null)
        {
            if (dData.opened)
            {
                if (col != null)
                    col.enabled = false;
                if (anim != null)
                {
                    anim.Play("Opened");
                }
            }
        }
    }

    /// <summary>
    /// Сохранить данные о двери
    /// </summary>
    public virtual InterObjData GetData()
    {
        DoorData dData = new DoorData(id, !col.enabled, gameObject.name);
        return dData;
    }

    #endregion //IHaveID

    #region storyActions

    /// <summary>
    /// Считать, что объект спрятан
    /// </summary>
    protected virtual void OpenDoor(StoryAction _action)
    {
        Open();
    }

    /// <summary>
    /// Функция-пустышка
    /// </summary>
    public void NullFunction(StoryAction _action)
    { }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить скрипт
    /// </summary>
    /// <returns></returns>
    public List<string> actionNames()
    {
        return new List<string>() { "openDoor" };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() { { "openDoor", new List<string>() } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() { { "openDoor", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь id-шников, связанных с конкретной функцией проверки условия сюжетного события
    /// </summary>
    public Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>() { { "", new List<string>() } };
    }

    /// <summary>
    /// Возвращает ссылку на сюжетное действие, соответствующее данному имени
    /// </summary>
    public StoryAction.StoryActionDelegate GetStoryAction(string s)
    {
        if (s == "openDoor")
            return OpenDoor;
        return NullFunction;
    }

    #endregion //IHaveStory

}
