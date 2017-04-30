using UnityEngine;
using System.Collections;
using UnityEditor;

/// <summary>
/// Класс, являющийся базовым для всех предметов в игре
/// </summary>
public class ItemClass : ScriptableObject
{
    public string itemName;
    [TextArea]
    public string itemTextName;//Название, которое используется при отображении в интерфейсе
    public MultiLanguageText itemMLTextName;//Название, которое используется при отображении в интерфейсе
    public string itemTextName1;//Название, которое используется при отображении в игре
    public MultiLanguageText itemMLTextName1;//Название, которое используется при отображении в игре
    [TextArea]
    public string itemDescription;//Краткое описание предмета
    public MultiLanguageText itemMLDescription;//Краткое описание предмета

    public Sprite itemImage;

}

[CustomEditor(typeof(ItemClass), true)]
public class ItemEditor : Editor
{

    ItemClass item;

    public virtual void OnEnable()
    {
        item = (ItemClass)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        item.itemMLTextName.russian = item.itemTextName;
        item.itemMLTextName1.russian = item.itemTextName1;
        item.itemMLDescription.russian = item.itemDescription;
    }
}