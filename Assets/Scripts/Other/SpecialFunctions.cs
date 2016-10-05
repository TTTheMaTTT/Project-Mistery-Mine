using UnityEngine;
using System.Collections;

/// <summary>
/// Функция, хранящая в себе методы общего характера
/// </summary>
public static class SpecialFunctions
{
    public static GameObject GetPlayer()
    {
        return GameObject.FindGameObjectWithTag("player");
    }
}
