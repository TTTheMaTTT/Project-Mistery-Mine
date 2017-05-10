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
/// Событийные данные об изменения громкости звуков
/// </summary>
public class SoundChangesEventArgs : EventArgs
{
    private float soundVolume;
    
    public float SoundVolume { get { return soundVolume; } }

    public SoundChangesEventArgs(float _soundVolume)
    {
        soundVolume = _soundVolume;
    }

}

/// <summary>
/// Событийные данные об изменения языка игры
/// </summary>
public class LanguageChangeEventArgs : EventArgs
{
    private LanguageEnum language;

    public LanguageEnum Language { get { return language; } }

    public LanguageChangeEventArgs(LanguageEnum _language)
    {
        language = _language;
    }

}

/// <summary>
/// Данные о событии, связанном с изменением уровня хп
/// </summary>
public class HealthEventArgs : EventArgs
{
    private float hp;
    private float hpDelta;
    private float maxHP;

    public float HP { get { return hp; } }
    public float HPDelta { get { return hpDelta; } }
    public float MaxHP { get { return maxHP; } }

    public HealthEventArgs(float _hp, float _hpDelta=0f, float _maxHP=12f)
    {
        hp = _hp;
        hpDelta = _hpDelta;
        maxHP = _maxHP;
    }

}

/// <summary>
/// Данные о событии, связанном с баффом, дебаффом или эффектом
/// </summary>
public class BuffEventArgs : EventArgs
{
    private BuffClass buff;
    private bool battleEffect = true;

    public BuffClass Buff { get { return buff; } }
    public bool BattleEffect { get { return battleEffect; } }

    public BuffEventArgs(BuffClass _buff, bool _bEffect)
    {
        buff = _buff;
        battleEffect = _bEffect;
    }
}

/// <summary>
/// Данные о событии, связанном с изменением уровня хп босса
/// </summary>
public class BossHealthEventArgs : EventArgs
{
    private float hp;
    private float maxHP;
    private MultiLanguageText bossName;

    public float HP { get { return hp; } }
    public float MaxHP { get { return maxHP; } }
    public MultiLanguageText BossName { get { return bossName; } }

    public BossHealthEventArgs(float _hp, float _maxHP, MultiLanguageText _bossName)
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

    private GameObject target;//По какому персонажу был нанесён урон

    public float HPDif { get { return hpDif; } }
    public GameObject Target { get { return target; } }

    public HitEventArgs(float _hpDif)
    {
        hpDif = _hpDif;
        target = null;
    }

    public HitEventArgs(float _hpDif, GameObject _target)
    {
        hpDif = _hpDif;
        target = _target;
    }


}

/// <summary>
/// Данные о событии "Был услышан враг (или какой-то другой отслеживаемый объект)"
/// </summary>
public class HearingEventArgs : EventArgs
{
    private GameObject target;//Кого услышали

    public GameObject Target { get { return target; } }

    public HearingEventArgs(GameObject _target)
    {
        target = _target;
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
    private WeaponClass currentWeapon;
    private ItemClass item;
    private ItemClass removedItem;

    public WeaponClass CurrentWeapon { get { return currentWeapon; } }
    public ItemClass Item { get { return item; } }
    public ItemClass RemovedItem { get { return removedItem; } }

    public EquipmentEventArgs(WeaponClass _weapon, ItemClass _item, ItemClass _removeItem=null)
    {
        currentWeapon = _weapon;
        item = _item;
        removedItem = _removeItem;
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

/// <summary>
/// Событийные данные, используемые для событий, связанных со сменой стороны конфликта
/// </summary>
public class LoyaltyEventArgs : EventArgs
{
    private LoyaltyEnum loyalty;

    public LoyaltyEventArgs(LoyaltyEnum _loyalty)
    {
        loyalty = _loyalty;
    }

    public LoyaltyEnum Loyalty { get { return loyalty; } }

}

/// <summary>
/// Событийные данные о начавшемся, либо законченном диалоге
/// </summary>
public class DialogEventArgs : EventArgs
{
    private bool begin = true;//Если true - диалог начался, иначе - закончился
    private bool stopGameProcess = true;//Если true, то данный диалог завершил процесс игры

    public bool Begin {get{return begin;}}
    public bool StopGameProcess { get { return stopGameProcess; } }

    public DialogEventArgs(bool _begin, bool _stopGameProcess)
    {
        begin = _begin;
        stopGameProcess = _stopGameProcess;
    }

}