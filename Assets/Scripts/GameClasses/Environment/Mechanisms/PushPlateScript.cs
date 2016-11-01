using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, реализующий нажимную плиту и взаимоействие с ней
/// Считаем, что плита нажата, когда на ней стоит игрок и отжата, когда игрок с неё слез
/// </summary>
public class PushPlateScript : LeverScript
{

    protected override void Initialize()
    {
        activated = false;
        base.Initialize();
    }

    /// <summary>
    /// Игрок нажал на плиту
    /// </summary>
    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!activated)
        {
            if (other.gameObject == SpecialFunctions.player)
            {
                activated = true;
                if (anim != null)
                    anim.Play("Active");
                foreach (GameObject obj in mechanisms)
                {
                    IMechanism mech = obj.GetComponent<IMechanism>();
                    if (mech != null)
                        mech.ActivateMechanism();
                }
            }
        }
    }

    public virtual void OnTriggerExit2D(Collider2D other)
    {
        if (activated)
        {
            if (other.gameObject == SpecialFunctions.player)
            {
                activated = false;
                if (anim!=null)
                    anim.Play("Inactive");
            }
        }
    }

}
