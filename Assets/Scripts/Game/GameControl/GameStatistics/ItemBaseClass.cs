﻿using UnityEngine;
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

    public List<ItemCollection> collections;//Игровые коллекции, которые можно пополнить на данном уровне

    public GameObject customDrop;//Префаб, который может содержать в себе любой дроп

    #endregion //fields

}

/// <summary>
/// Класс, содержащий данные о коллекционном предмете
/// </summary>
[System.Serializable]
public class CollectorsItem
{
    public ItemClass item;
    [SerializeField][HideInInspector]public bool itemFound;//Был ли найден предмет?

    public CollectorsItem(CollectorsItem cItem)
    {
        item = cItem.item;
        itemFound = false;
    }

    /// <summary>
    /// Установить, что предмет можно найти
    /// </summary>
    public void SetFound()
    {
        itemFound = true;
    }

}

/// <summary>
/// Коллекция предметов
/// </summary>
[System.Serializable]
public class ItemCollection
{
    public string collectionName;//Имя коллекции
    public string settingName;//Название сеттинга, которому соответствует данная коллекция
    public List<CollectorsItem> collection = new List<CollectorsItem>();//Предметы, что содержит данная коллекция

    public ItemCollection(ItemCollection _collection)
    {
        collectionName = _collection.collectionName;
        settingName = _collection.settingName;
        collection = new List<CollectorsItem>();
        foreach (CollectorsItem _item in _collection.collection)
            collection.Add(new CollectorsItem(_item));
    }
}