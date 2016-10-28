using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Статистика взаимодействия с механизмами
/// </summary>
public class MechanismInteractionStatistics : Statistics
{
    public string mechName;

    public override Type GetObjType { get { return typeof(LeverScript); } }

    public override void Compare(UnityEngine.Object obj)
    {
        LeverScript mech = (LeverScript)obj;
        if (mech.gameObject.name.Contains(mechName))
            value++;
    }
}
