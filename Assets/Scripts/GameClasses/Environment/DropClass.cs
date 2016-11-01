using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, характеризующий подбираемые предметы
/// </summary>
public class DropClass : MonoBehaviour, IInteractive
{

    #region consts

    private const float groundRadius = .001f;

    #endregion //consts

    #region fields

    public ItemClass item;



    #endregion //fields

    #region parametres

    public bool autoPick;//Будет ли дроп автоматически подбираться, когда будет в зоне доступа персонажа?

    #endregion //parametres

    void Awake()
    {
    }

    /// <summary>
    /// Провзаимодействовать с дропом
    /// </summary>
    public void Interact()
    {
        SpecialFunctions.player.GetComponent<HeroController>().SetItem(item);
        Destroy(gameObject);
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
        if (autoPick)
        if (other.gameObject == SpecialFunctions.player)
        {
            Interact();
        }
    }

}
