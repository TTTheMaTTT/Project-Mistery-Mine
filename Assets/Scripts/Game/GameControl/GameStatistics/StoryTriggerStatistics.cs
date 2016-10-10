using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Статистика, которая может вестись для подсчёта посещённых мест
/// </summary>
public class StoryTriggerStatistics : Statistics
{
    public string sTriggerName;

    public override Type GetObjType { get { return typeof(StoryTrigger); } }

    public override void Compare(UnityEngine.Object obj)
    {
        StoryTrigger sTrigger = (StoryTrigger)obj;
        if (sTrigger.gameObject.name.Contains(sTriggerName))
            value++;
    }
}