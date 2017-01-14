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
/// Данные о событии, связанном с изменением уровня хп босса
/// </summary>
public class BossHealthEventArgs : EventArgs
{
    private float hp;
    private float maxHP;
    private string bossName;

    public float HP { get { return hp; } }
    public float MaxHP { get { return maxHP; } }
    public string BossName { get { return bossName; } }

    public BossHealthEventArgs(float _hp, float _maxHP, string _bossName)
    {
        hp = _hp;
        maxHP = _maxHP;
        bossName = _bossName;
    }

}

/// <summary>
/// Данные о событии, связанном с нанесением урона
/// </summary>
public class HitEventArgs : EventArgs
{
    private float hpDif;//Насколько изменилось хп персонажа

    public float HPDif { get { return hpDif; } }

    public HitEventArgs(float _hpDif)
    {
        hpDif = _hpDif;
    }
}

/// <summary>
/// Данные о событии, связанном с задыханием персонажа
/// </summary>
public class SuffocateEventArgs : EventArgs
{
    private int airSupply;

    public int AirSupply { get { return airSupply; } }

    public SuffocateEventArgs(int _airSupply)
    {
        airSupply = _airSupply;
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

/// <summary>
/// Событийные данные, используемые для событий, связанных со сменой моделей поведения
/// </summary>
public class BehaviorEventArgs : EventArgs
{
    private BehaviorEnum behaviour;

    public BehaviorEventArgs(BehaviorEnum _behaviour)
    {
        behaviour = _behaviour;
    }

    public BehaviorEnum Behaviour { get { return behaviour; } }

}