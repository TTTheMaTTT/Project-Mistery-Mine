﻿using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Статистика, учитывающая открытие сундуков
/// </summary>
public class ChestOpenStatistics : Statistics
{
    public string chestName;

    public override Type GetObjType { get { return typeof(ChestController); } }

    public override void Compare(UnityEngine.Object obj)
    {
        ChestController chest = (ChestController)obj;
        if (chest.gameObject.name.Contains(chestName))
            value++;
    }
}
