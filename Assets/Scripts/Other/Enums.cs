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
/// Модель поведения ИИ
/// </summary>
public enum BehaviourEnum {calm=0, agressive=1, patrol=2 }

/// <summary>
/// Режим перемещения камеры
/// </summary>
public enum CameraModEnum {position=0, move=1, player=2, playerMove=3}

/// <summary>
/// Енам, связанный с режимом редактора уровней
/// </summary>
public enum EditorModEnum {select=0, draw=1, drag=2, erase=3, map=4 }

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

/// <summary>
/// Енам, указывающий на тип клетки, указывающая, как моб по ней может перемещаться. 
/// (Для различных мобов тип usual подразумевает совсем разные способы перемещения, которые в то же время являются естественными для выбранного типа моба
/// </summary>
public enum NavCellTypeEnum {usual=0, ladder=1, movPlatform=2,jump=3 }

/// <summary>
/// Типа карты, предназначенный для определённых типов мобов (обычная карта гуманоида, карта полёта, карта ползания)
/// </summary>
public enum NavMapTypeEnum {usual=0, fly=1, crawl=2 }