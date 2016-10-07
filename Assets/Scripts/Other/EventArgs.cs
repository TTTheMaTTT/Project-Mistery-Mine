using UnityEngine;
using System.Collections;
using System;

//Здесь будут описаны все аргументы функций, связанных с ивентами

/// <summary>
/// Событийные данные о запрашиваемом стиле анимирования
/// </summary>
public class AnimationEventArgs : EventArgs
{
    private string animationType;
    private string id;
    private int argument;

    public AnimationEventArgs(string _animationType)
    {
        animationType = _animationType;
        id = "";
        argument = 0;
    }

    public AnimationEventArgs(string _animationType, string _id, int _argument)
    {
        animationType = _animationType;
        id = _id;
        argument = _argument;
    }

    public string AnimationType{get { return animationType; }set { animationType = value; }}
    public string ID { get { return id; } }
    public int Argument { get {return argument; } }
}

/// <summary>
/// Данные о событии, связанном с изменением уровня хп
/// </summary>
public class HealthEventArgs : EventArgs
{
    private float hp;

    public float HP { get { return hp; } }

    public HealthEventArgs(float _hp)
    {
        hp = _hp;
    }

}

/// <summary>
/// Данные о событии, связанном с изменениями в инвентаре
/// </summary>
public class EquipmentEventArgs : EventArgs
{
    private ItemClass item;

    public ItemClass Item { get { return item; } }

    public EquipmentEventArgs(ItemClass _item)
    {
        item = _item;
    }

}

/// <summary>
/// Событийные данные, используемые для осуществления сюжетных событий
/// </summary>
public class StoryEventArgs : EventArgs
{

    private string id;
    private int argument;

    public StoryEventArgs(string _id, int _argument)
    {
        id = _id;
        argument = _argument;
    }

    public StoryEventArgs()
    {
    }

    public string ID { get { return id; } }
    public int Argument { get { return argument; } }

}
