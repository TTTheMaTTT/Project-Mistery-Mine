using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Статистика, которая может вестись для подсчёта смерти определённого монстра
/// </summary>
public class MonsterDeathStatistics : Statistics
{
    public string enemyName;

    public override Type GetObjType { get { return typeof(AIController); } }

    public override void Compare(UnityEngine.Object obj)
    {
        AIController monster = (AIController)obj;
        if (monster.gameObject.name.Contains(enemyName))
            value++;
    }

}