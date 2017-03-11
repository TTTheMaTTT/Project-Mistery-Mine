using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Класс, характеризующий подбираемые предметы
/// </summary>
public class DropClass : MonoBehaviour, IInteractive
{

    #region consts

    private const float groundRadius = .001f;
    private const float dropTime = .8f;//Сколько времени предмет "выпадает"

    #endregion //consts

    #region eventHandlers

    public EventHandler<EventArgs> DropIsGot;//Событие "Дроп был взят" 

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
            Destroy(gameObject);
            SpecialFunctions.statistics.ConsiderStatistics(this);
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
            sRenderer.SetPropertyBlock(mpb);
        }
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
