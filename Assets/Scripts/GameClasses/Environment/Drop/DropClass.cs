using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, характеризующий подбираемые предметы
/// </summary>
public class DropClass : MonoBehaviour, IInteractive, IHaveStory
{

    #region consts

    private const float groundRadius = .001f;
    private const float dropTime = .8f;//Сколько времени предмет "выпадает"

    #endregion //consts

    #region eventHandlers

    public EventHandler<EventArgs> DropIsGot;//Событие "Дроп был взят" 
    public EventHandler<StoryEventArgs> StoryDropIsGot;//Сюжетное событие "Дроп был взят"

    #endregion //eventHandlers

    #region fields

    public ItemClass item;
    protected SpriteRenderer sRenderer;

    #endregion //fields

    #region parametres

    public bool autoPick;//Будет ли дроп автоматически подбираться, когда будет в зоне доступа персонажа?
    public bool dropped = false;//Предмет можно подобрать только в том случае, если это поле true

    protected Color outlineColor = Color.yellow;

    #endregion //parametres

    protected virtual void Awake()
    {
        StartCoroutine(DropProcess());
    }

    /// <summary>
    /// Функция, что превращает обычный игровой объект в дроп
    /// </summary>
    public static void AddDrop(GameObject obj, DropClass drop)
    {
        obj.tag = "drop";
        Sprite sprite = drop.item.itemImage;
        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = sprite.textureRect.size/sprite.pixelsPerUnit;
        obj.AddComponent<Rigidbody2D>();
        obj.AddComponent<SpriteRenderer>().sprite = sprite;
        DropClass _drop=obj.AddComponent<DropClass>();
        _drop.item = drop.item;
    }

    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (autoPick && dropped)
            if (other.gameObject == SpecialFunctions.Player)
            {
                Interact();
            }
    }

    public virtual IEnumerator DropProcess()
    {
        dropped = false;
        sRenderer = GetComponent<SpriteRenderer>();
        if (sRenderer == null)
        {
            Transform visual = transform.FindChild("Visual");
            if (visual != null)
                sRenderer = visual.GetComponent<SpriteRenderer>();
        }
        yield return new WaitForSeconds(dropTime);
        dropped = true;
    }

    #region IInteractive

    /// <summary>
    /// Провзаимодействовать с дропом
    /// </summary>
    public virtual void Interact()
    {
        if (dropped)
        {
            SpecialFunctions.Player.GetComponent<HeroController>().SetItem(item);
            OnDropGet(new EventArgs());
            if (gameObject.layer == LayerMask.NameToLayer("hidden"))
                gameObject.layer=LayerMask.NameToLayer("drop");
            Destroy(gameObject);
            SpecialFunctions.statistics.ConsiderStatistics(this);
            SpecialFunctions.StartStoryEvent(this, StoryDropIsGot, new StoryEventArgs());
        }
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
            mpb.SetFloat("_OutlineWidth", .08f / ((Vector2)sRenderer.transform.lossyScale).magnitude);
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
    /// Вернуть id персонажа
    /// </summary>
    public int GetID()
    {
        return -1;
    }

    /// <summary>
    /// Выставить id объекту
    /// </summary>
    public void SetID(int _id)
    {
    }

    /// <summary>
    /// Настроить персонажа в соответствии с сохранёнными данными
    /// </summary>
    public void SetData(InterObjData _intObjData)
    {
    }

    /// <summary>
    /// Вернуть сохраняемые данные персонажа
    /// </summary>
    public InterObjData GetData()
    {
        return null;
    }

    #endregion //IHaveID

    #region storyActions

    /// <summary>
    /// Считать, что объект спрятан
    /// </summary>
    protected virtual void SetHidden(StoryAction _action)
    {
        gameObject.layer = _action.id1 == "hidden"?LayerMask.NameToLayer("hidden"):LayerMask.NameToLayer("drop");        
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
        return new List<string>() { "setHidden"};
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
        return new Dictionary<string, List<string>>() { { "", new List<string>() },
                                                        { "compareHistoryProgress",SpecialFunctions.statistics.HistoryBase.stories.ConvertAll(x=>x.storyName)} };
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

    #region events

    /// <summary>
    /// Событие "Дроп был взят"
    /// </summary>
    protected void OnDropGet(EventArgs e)
    {
        if (DropIsGot!=null)
        {
            DropIsGot(this, e);
        }
    }

    #endregion //events

}
