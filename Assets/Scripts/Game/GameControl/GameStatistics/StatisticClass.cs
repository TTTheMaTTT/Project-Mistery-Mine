using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Класс, реализующий статистические данные (подсчёт некой игровой сущности), которая могла бы легко редактироваться
/// </summary>
public abstract class Statistics: ScriptableObject
{
    [HideInInspector]
    public string statisticName="New Statistics", datapath="DataPath";//Наименовании статистики и её местоположение

    [NonSerialized]
    public int value;//Сам подсчёт характеристики

    public virtual void Compare(UnityEngine.Object obj)
    {

    }

    public virtual Type GetObjType { get { return null; } }//Какой тип объектов используется для подсчёта

}

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

/// <summary>
/// Статистика, которая может вестись для подсчёта посещённых мест
/// </summary>
public class StoryTyiggerStatistics : Statistics
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