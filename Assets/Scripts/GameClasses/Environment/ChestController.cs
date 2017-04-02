using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, реализующий поведение сундука
/// </summary>
public class ChestController : MonoBehaviour, IInteractive, IHaveStory
{

    #region consts

    private const float pushForceY = 50f, pushForceX = 25f;//С какой силой выбрасывается содержимое сундука при его открытии?

    #endregion //consts

    #region eventHandlers

    public EventHandler<EventArgs> ChestOpenEvent;//Событие о том, что был открыт этот сундук

    #endregion //eventHandlers

    #region fields

    public List<DropClass> content = new List<DropClass>();

    protected SpriteRenderer sRenderer;
    protected AudioSource aSource;

    #endregion //fields

    #region parametres

    [SerializeField]
    protected int id;

    protected Color outlineColor = Color.yellow;

    #endregion //parametres

    #region IInteractive

    void Awake()
    {
        sRenderer = GetComponent<SpriteRenderer>();
        aSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Как происходит взаимодействие с сундуком
    /// </summary>
    public void Interact()
    {
        foreach (DropClass drop in content)
        {
            GameObject _drop = Instantiate(drop.gameObject, transform.position + Vector3.up * .05f, transform.rotation) as GameObject;
            if (_drop.GetComponent<Rigidbody2D>() != null)
            {
                _drop.GetComponent<Rigidbody2D>().AddForce(new Vector2(UnityEngine.Random.RandomRange(-pushForceX, pushForceX), pushForceY));
            }
            /*GameObject obj = new GameObject(drop.item.itemName);
            obj.transform.position = transform.position;
            DropClass.AddDrop(obj, drop);
            Rigidbody2D rigid = obj.GetComponent<Rigidbody2D>();
            rigid.AddForce(new Vector2(Random.RandomRange(-pushForceX, pushForceX), pushForceY));*/
        }
        gameObject.tag = "Untagged";
        SpecialFunctions.statistics.ConsiderStatistics(this);
        Animator anim = GetComponent<Animator>();
        SetOutline(false);
        if (anim != null)
            anim.Play("Opened");
        SpecialFunctions.PlaySound(aSource);
        if (gameObject.layer == LayerMask.NameToLayer("hidden"))
            gameObject.layer = LayerMask.NameToLayer("hidden");
        OnChestOpened(new EventArgs());
        DestroyImmediate(this);
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

    #region events

    /// <summary>
    /// Событие "сундук был открыт"
    /// </summary>
    protected virtual void OnChestOpened(EventArgs e)
    {
        EventHandler<EventArgs> handler = ChestOpenEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

    #region IHaveID

    /// <summary>
    /// Вернуть id
    /// </summary>
    public int GetID()
    {
        return id;
    }

    /// <summary>
    /// Выставить id объекту
    /// </summary>
    public void SetID(int _id)
    {
        id = _id;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif //UNITY_EDITOR
    }

    /// <summary>
    /// Загрузить данные о сундуке
    /// </summary>
    public void SetData(InterObjData _intObjData)
    {
    }

    /// <summary>
    /// Сохранить данные о сундуке
    /// </summary>
    public InterObjData GetData()
    {
        InterObjData cData = new InterObjData(id,gameObject.name, transform.position);
        return cData;
    }

    /// <summary>
    /// Сразу открыть сундук без вываливания содержимого
    /// </summary>
    public void DestroyClosedChest()
    {
        gameObject.tag = "Untagged";
        SpecialFunctions.statistics.ConsiderStatistics(this);
        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.Play("Opened");
        if (gameObject.layer == LayerMask.NameToLayer("hidden"))
            gameObject.layer = LayerMask.NameToLayer("Default");
        DestroyImmediate(this);
    }

    #endregion //IHaveID

    #region storyActions

    /// <summary>
    /// Считать, что объект спрятан
    /// </summary>
    protected virtual void SetHidden(StoryAction _action)
    {
        gameObject.layer = _action.id1 == "hidden" ? LayerMask.NameToLayer("hidden") : LayerMask.NameToLayer("Default");
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
        return new List<string>() { "setHidden" };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() { { "setHidden", new List<string>() { "hidden" } } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() { { "setHidden", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь id-шников, связанных с конкретной функцией проверки условия сюжетного события
    /// </summary>
    public Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>() { { "", new List<string>() }};
    }

    /// <summary>
    /// Возвращает ссылку на сюжетное действие, соответствующее данному имени
    /// </summary>
    public StoryAction.StoryActionDelegate GetStoryAction(string s)
    {
        if (s == "setHidden")
            return SetHidden;
        return NullFunction;
    }

    #endregion //IHaveStory

}
