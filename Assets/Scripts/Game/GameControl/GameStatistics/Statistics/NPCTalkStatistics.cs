using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Статистика, которая может вестись для подсчёта НПС, с которыми вёлся разговор
/// </summary>
public class NPCTalkStatistics : Statistics
{
    public string NPCName;

    public override Type GetObjType { get { return typeof(NPCController); } }

    public override void Compare(UnityEngine.Object obj)
    {
        NPCController npc = (NPCController)obj;
        if (npc.gameObject.name.Contains(NPCName))
            value++;
    }
}