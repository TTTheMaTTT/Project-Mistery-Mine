using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// База данных предметов
/// </summary>
public class ItemBaseClass : ScriptableObject
{
    #region fields

    public string databaseName;
    public List<WeaponClass> weapons;//Список оружий, используемых в игре
    public List<ItemClass> items;//Список предметов, используемых в игре
    public List<GameObject> drops;//Список оигровых объектов, представляющих собой дроп

    public GameObject customDrop;//Префаб, который может содержать в себе любой дроп

    #endregion //fields

}
