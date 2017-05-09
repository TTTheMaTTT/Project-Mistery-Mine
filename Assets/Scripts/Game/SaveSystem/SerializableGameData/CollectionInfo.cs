using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Информация о собранных коллекциях
/// </summary>
[XmlType("Collection Info")]
public class CollectionInfo
{
    [XmlElement("Collection Name")]
    public string collectionName="";

    [XmlElement("Found Items Count")]
    public int foundItemsCount=0;

    [XmlArray("Which Items Were Found?")]
    [XmlArrayItem("Was Item With This Index Found?")]
    public List<bool> itemsFound = new List<bool>();

    public CollectionInfo()
    {
    }

    public CollectionInfo(ItemCollection _collection)
    {
        collectionName = _collection.collectionName;
        itemsFound = new List<bool>();
        for (int i = 0; i < _collection.collection.Count; i++)
            itemsFound.Add(_collection.collection[i].itemFound);
        foundItemsCount = _collection.itemsFoundCount;
    }

}
