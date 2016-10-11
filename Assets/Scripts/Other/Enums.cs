using UnityEngine;
using System.Collections;

//Список всех енамов

/// <summary>
/// Енам, ответственный за ориентацию персонажа
/// </summary>
public enum OrientationEnum {left=-1, right=1 }

/// <summary>
/// Ориентация персонажа относительно поверхности земли
/// </summary>
public enum GroundStateEnum {grounded = 0, crouching = 1, inAir=2 }