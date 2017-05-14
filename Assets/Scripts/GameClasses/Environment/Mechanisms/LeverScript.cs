using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, реализующий рычаг и взаимодействие с ним
/// </summary>
public class LeverScript : MonoBehaviour, IInteractive
{

    #region eventHandlers

    public EventHandler<EventArgs> LeverActionEvent;

    #endregion //eventHandlers

    #region fields

    [SerializeField]protected List<GameObject> mechanisms = new List<GameObject>();//Список механизмов, активируемых рычагом
    protected Animator anim;

    protected bool once = false;//Взаимодействовал ли уже игрок с рычагом
    protected SpriteRenderer sRenderer;
    protected AnimatedSoundManager soundManager;

    #endregion //fields

    #region parametres

    [SerializeField]protected bool activated;//Активирован ли рычаг?
    public bool Activated { get { return activated; } }

    [SerializeField]
    protected int id;

    protected Color outlineColor = Color.yellow;

    #endregion //parametres

    public virtual void Awake ()
    {
        Initialize();
	}

    protected virtual void Initialize()
    {
        anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Play(activated ? "Active" : "Inactive");
        }
        sRenderer = GetComponent<SpriteRenderer>();
        once = false;
        soundManager = GetComponent<AnimatedSoundManager>();
    }

    #region events

    /// <summary>
    /// Запустить событие при взаимодействии с рычагом
    /// </summary>
    public void StartStoryEvent(EventArgs e)
    {
        EventHandler<EventArgs> handler = LeverActionEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

    #region IInteractive

    /// <summary>
    /// Взаимодействие с рычагом
    /// </summary>
    public virtual void Interact()
    {
        if (!activated)
        {
            foreach (GameObject obj in mechanisms)
            {
                if (obj == null)
                    continue;
                IMechanism mech = obj.GetComponent<IMechanism>();
                if (mech != null)
                    mech.ActivateMechanism();
            }
            activated = true;
            if (anim != null)
            {
                anim.Play(activated ? "Active" : "Inactive");
            }
            if (!once)
            {
                once = true;
                SpecialFunctions.statistics.ConsiderStatistics(this);
            }
            if (soundManager) soundManager.PlaySound("Activate");
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
            mpb.SetFloat("_OutlineWidth", .08f / ((Vector2)transform.lossyScale).magnitude);
            sRenderer.SetPropertyBlock(mpb);
        }
    }

    /// <summary>
    /// Можно ли провзаимодействовать с объектом в данный момент?
    /// </summary>
    public virtual bool IsInteractive()
    {
        return !once;
    }

    #endregion //IInteractive

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
    /// Загрузить данные о механизме
    /// </summary>
    public virtual void SetData(InterObjData _intObjData)
    {
        if (!(_intObjData is MechData))
            return;
        MechData mData = (MechData)_intObjData;
        if (mData != null)
        {
            activated = mData.activated;
            if (anim != null)
            {
                anim.Play(activated ? "Active" : "Inactive");
            }
        }
    }

    /// <summary>
    /// Сохранить данные о механизме
    /// </summary>
    public virtual InterObjData GetData()
    {
        MechData mData = new MechData(id, activated, transform.position, gameObject.name);
        return mData;
    }

    #endregion //IHaveID

}
