using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, хранящий информацию о баффе (дебаффе), действующем на персонажа
/// </summary>
public class BuffClass
{
    public string buffName;//Название баффа
    public float beginTime, duration;//Когда началось действие баффа и какова его продолжительность

    public int argument;//Параметры, позволяющие
    public string id;// уточнить действие баффа.

    public BuffClass(string _bName, float _bTime, float _duration, int _argument=0, string _id="")
    {
        buffName = _bName;
        beginTime = _bTime;
        duration = _duration;
        argument = _argument;
        id = _id;
    }

}
