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
                                               "Для того чтобы открыть эту дверь тебе нужен ключ - найди его!",
                                               openedDoorMessage =
                                               "Дверь открыта";//Какое сообщение должно выводится при различных попытках открыть дверь

    protected Collider2D col;
    protected Animator anim;


    #endregion //fields

    void Awake()
    {
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
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
        else
            SpecialFunctions.SetText(closedDoorMessage, 2.5f);

    }

    /// <summary>
    /// Открыть дверь
    /// </summary>
    public virtual void Open()
    {
        if (col != null)
            col.enabled = false;
        SpecialFunctions.SetText(openedDoorMessage, 1.5f);
        if (anim != null)
        {
            anim.Play("Opened");
        }
    }

    /// <summary>
    /// Активировать механизм
    /// </summary>
    public void ActivateMechanism()
    {
        col.enabled = !col.enabled;
    }

}
