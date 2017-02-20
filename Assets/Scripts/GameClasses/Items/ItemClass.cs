using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, являющийся базовым для всех предметов в игре
/// </summary>
public class ItemClass : ScriptableObject
{
    public string itemName;
    public string itemTextName;//Название, которое используется при отображении в игре

    public Sprite itemImage;

}