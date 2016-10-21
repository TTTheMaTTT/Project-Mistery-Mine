using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, реализующий рычаг и взаимодействие с ним
/// </summary>
public class LeverScript : MonoBehaviour, IInteractive
{

    #region fields

    [SerializeField]protected List<IMechanism> mechanisms = new List<IMechanism>();//Список механизмов, активируемых рычагом
    protected Animator anim;

    #endregion //fields

    #region parametres

    [SerializeField]protected bool activated;

    #endregion //parametres

    void Awake ()
    {
        anim = GetComponent<Animator>();
        if (anim!=null)
        {
            anim.Play(activated ? "Active" : "Inactive");
        }
	}

    /// <summary>
    /// Взаимодействие с рычагом
    /// </summary>
    public void Interact()
    {
        foreach (IMechanism mech in mechanisms)
            mech.ActivateMechanism();
        activated = !activated;
        if (anim != null)
        {
            anim.Play(activated ? "Active" : "Inactive");
        }
    }
	
}
