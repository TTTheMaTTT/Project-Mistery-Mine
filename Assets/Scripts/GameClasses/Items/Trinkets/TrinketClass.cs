using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, особых предметов, которые накладывают на героя пассивные эффекты
/// </summary>
public class TrinketClass : ItemClass
{
    public List<TrinketEffectClass> trinketEffects;//Информация об эффектах, которые может производить предмет
}

/// <summary>
/// Класс, содержащий данные об эффекте тринкета
/// </summary>
[System.Serializable]
public class TrinketEffectClass
{

    public string effectName;//Название эффекта
    public TrinketEffectTypeEnum effectType;//Тип эффекта
    //public float effectTime=10f;//Время действия эффекта
    public float effectProbability=.1f;//Шанс действия эффекта

    public TrinketEffectClass(string _name, TrinketEffectTypeEnum _type, /*float _time,*/ float _probability)
    {
        effectName = _name;
        effectType = _type;
        //effectTime = _time;
        effectProbability = _probability;
    }

}