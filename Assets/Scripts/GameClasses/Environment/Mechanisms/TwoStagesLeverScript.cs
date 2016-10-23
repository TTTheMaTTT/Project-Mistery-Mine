using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, реализующий поведение рычага с двумя режимами - активный и неактивный рычаг
/// </summary>
public class TwoStagesLeverScript : LeverScript, IMechanism
{
    public override void Interact()
    {
        foreach (GameObject obj in mechanisms)
        {
            IMechanism mech = obj.GetComponent<IMechanism>();
            if (mech != null)
                mech.ActivateMechanism();
        }
        activated = !activated;
        if (anim != null)
        {
            anim.Play(activated ? "Active" : "Inactive");
        }
    }

    public void ActivateMechanism()
    {
        if (activated)
        {
            foreach (GameObject obj in mechanisms)
            {
                IMechanism mech = obj.GetComponent<IMechanism>();
                if (mech != null)
                    mech.ActivateMechanism();
            }
            activated = false;
            if (anim != null)
            {
                anim.Play("Inactive");
            }
        }
    }
}
