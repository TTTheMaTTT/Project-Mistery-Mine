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
    public List<TrinketClass> trinkets;//Список тринкетов
    public List<ItemClass> items;//Список предметов, используемых в игре
    public List<GameObject> drops;//Список оигровых объектов, представляющих собой дроп

    public List<ItemCollection> collections;//Игровые коллекции, которые можно пополнить на данном уровне

    public GameObject customDrop;//Префаб, который может содержать в себе любой дроп

    #endregion //fields

    public List<string> ItemNames
    {
        get
        {
            List<string> _itemNames = new List<string>();
            foreach (ItemClass _item in items)
                _itemNames.Add(_item.itemName);
            return _itemNames;
        }
    }

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
    public string collectionName;
    public MultiLanguageText collectionTextName;//Имя коллекции
    public string settingName;//Название сеттинга, которому соответствует данная коллекция
    public List<CollectorsItem> collection = new List<CollectorsItem>();//Предметы, что содержит данная коллекция
    public int itemsFoundCount = 0;//Сколько предметов было найдено

    public ItemCollection(ItemCollection _collection)
    {
        collectionName = _collection.collectionName;
        collectionTextName = _collection.collectionTextName;
        settingName = _collection.settingName;
        collection = new List<CollectorsItem>();
        itemsFoundCount = _collection.itemsFoundCount;
        foreach (CollectorsItem _item in _collection.collection)
            collection.Add(new CollectorsItem(_item));
    }
}