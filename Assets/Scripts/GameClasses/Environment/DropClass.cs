using UnityEngine;
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

    #region fields

    public ItemClass item;

    #endregion //fields

    #region parametres

    public bool autoPick;//Будет ли дроп автоматически подбираться, когда будет в зоне доступа персонажа?
    public bool dropped = false;//Предмет можно подобрать только в том случае, если это поле true

    #endregion //parametres

    protected virtual void Awake()
    {
        StartCoroutine(DropProcess());
    }

    /// <summary>
    /// Провзаимодействовать с дропом
    /// </summary>
    public void Interact()
    {
        if (dropped)
        {
            SpecialFunctions.player.GetComponent<HeroController>().SetItem(item, false);
            Destroy(gameObject);
            SpecialFunctions.statistics.ConsiderStatistics(this);
        }
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
            if (other.gameObject == SpecialFunctions.player)
            {
                Interact();
            }
    }

    public virtual IEnumerator DropProcess()
    {
        dropped = false;
        yield return new WaitForSeconds(dropTime);
        dropped = true;
    }

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

}
