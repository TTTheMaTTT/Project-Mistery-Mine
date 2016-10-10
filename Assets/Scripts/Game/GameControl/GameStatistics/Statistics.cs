using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Класс, реализующий статистические данные (подсчёт некой игровой сущности), которая могла бы легко редактироваться
/// </summary>
public class Statistics : ScriptableObject
{
    [HideInInspector]
    public string statisticName = "New Statistics", datapath = "DataPath";//Наименовании статистики и её местоположение

    [NonSerialized]
    public int value;//Сам подсчёт характеристики

    public virtual void Compare(UnityEngine.Object obj)
    {

    }

    public virtual Type GetObjType { get { return null; } }//Какой тип объектов используется для подсчёта

}