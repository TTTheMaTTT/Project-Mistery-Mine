using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Статистика, учитывающая подбор дропа
/// </summary>
public class DropStatistics : Statistics
{
    public string dropName;

    public override Type GetObjType { get { return typeof(DropClass); } }

    public override void Compare(UnityEngine.Object obj )
    {
        DropClass drop = (DropClass)obj;
        if (drop.gameObject.name.Contains(dropName))
            value++;
    }
}
