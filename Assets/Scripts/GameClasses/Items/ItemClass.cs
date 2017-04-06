using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, являющийся базовым для всех предметов в игре
/// </summary>
public class ItemClass : ScriptableObject
{
    public string itemName;
    [TextArea]
    public string itemTextName;//Название, которое используется при отображении в интерфейсе
    public string itemTextName1;//Название, которое используется при отображении в игре
    [TextArea]
    public string itemDescription;//Краткое описание предмета

    public Sprite itemImage;

}