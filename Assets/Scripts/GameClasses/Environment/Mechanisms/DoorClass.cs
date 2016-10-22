using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, представляющий собой дверь
/// </summary>
public class DoorClass : MonoBehaviour, IInteractive, IMechanism
{

    #region fields

    [SerializeField]protected string keyID;//Название ключа, что откроет эту дверь
    [SerializeField][TextArea]protected string closedDoorMessage = 
                                               "Для того чтоб открыть эту дверь тебе нужен ключ - найди его!";//Какое сообщение должно выводится при неудачной попытке открыть дверь
    protected Collider2D col;

    #endregion //fields

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Провести взаимодействие с дверью
    /// </summary>
    public void Interact()
    {
        HeroController player = SpecialFunctions.player.GetComponent<HeroController>();
        if (keyID == string.Empty)
            Open();
        else if (player.Bag.Find(x => x.itemName == keyID))
            Open();

    }

    /// <summary>
    /// Открыть дверь
    /// </summary>
    public void Open()
    {
        if (col != null)
            col.enabled = false;
    }

    /// <summary>
    /// Активировать механизм
    /// </summary>
    public void ActivateMechanism()
    {
        col.enabled = !col.enabled;
    }

}
