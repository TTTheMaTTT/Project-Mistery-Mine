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

    #endregion //fields

    #region parametres

    [SerializeField]protected bool activated;

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
        once = false;
    }

    /// <summary>
    /// Взаимодействие с рычагом
    /// </summary>
    public virtual void Interact()
    {
        if (!activated)
        {
            foreach (GameObject obj in mechanisms)
            {
                IMechanism mech = obj.GetComponent<IMechanism>();
                if (mech!=null)
                    mech.ActivateMechanism();
            }
            activated = !activated;
            if (anim != null)
            {
                anim.Play(activated ? "Active" : "Inactive");
            }
            if (!once)
            {
                once = true;
                SpecialFunctions.statistics.ConsiderStatistics(this);
            }
        }
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

}
