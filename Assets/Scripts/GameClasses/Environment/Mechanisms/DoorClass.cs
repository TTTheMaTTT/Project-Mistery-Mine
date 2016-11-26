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

    #region parametres

    [SerializeField]
    [HideInInspector]
    protected int id;

    #endregion //parametres

    void Awake()
    {
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Провести взаимодействие с дверью
    /// </summary>
    public virtual void Interact()
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
    public virtual void ActivateMechanism()
    {
        Open();
    }

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
                Open();
            }
        }
    }

    /// <summary>
    /// Сохранить данные о двери
    /// </summary>
    public virtual InterObjData GetData()
    {
        DoorData dData = new DoorData(id, !col.enabled);
        return dData;
    }

}
