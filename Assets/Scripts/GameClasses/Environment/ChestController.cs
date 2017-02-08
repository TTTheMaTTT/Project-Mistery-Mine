using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, реализующий поведение сундука
/// </summary>
public class ChestController : MonoBehaviour, IInteractive
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

    #endregion //fields

    #region parametres

    [SerializeField]
    [HideInInspector]
    protected int id;

    protected Color outlineColor = Color.yellow;

    #endregion //parametres

    #region IInteractive

    void Awake()
    {
        sRenderer = GetComponent<SpriteRenderer>();
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
        InterObjData cData = new InterObjData(id);
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
        DestroyImmediate(this);
    }

    #endregion //IHaveID

}
