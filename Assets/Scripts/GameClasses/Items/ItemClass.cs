using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, являющийся базовым для всех предметов в игре
/// </summary>
public class ItemClass : ScriptableObject
{
    public string itemName;
    public MultiLanguageText itemMLTextName;//Название, которое используется при отображении в интерфейсе
    public MultiLanguageText itemMLTextName1;//Название, которое используется при отображении в игре
    public MultiLanguageText itemMLDescription;//Краткое описание предмета

    public Sprite itemImage;

}