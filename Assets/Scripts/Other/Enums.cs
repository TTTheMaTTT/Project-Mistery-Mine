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

/// <summary>
/// Режим перемещения камеры
/// </summary>
public enum CameraModEnum {position=0, move=1, player=2, playerMove=3}

/// <summary>
/// Енам, связанный с режимом редактора уровней
/// </summary>
public enum EditorModEnum {select=0, draw=1, drag=2, erase=3 }

/// <summary>
/// Енам, связанный с режимом рисования
/// </summary>
public enum DrawModEnum { ground = 0, plant = 1, water = 2, ladder=3, spikes=4,usual=5, lightObstacle=6 }

/// <summary>
/// Режим диалога
/// </summary>
public enum DialogModEnum {usual=0, random=1, one=2 }

/// <summary>
/// Енам, связанный с видами игровых препятствий
/// </summary>
public enum ObstacleEnum {plants=0, spikes=1 }