using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, в котором содержится информация о растениях, используемых в редакторе
/// </summary>
public class PlantBase : ScriptableObject
{
    #region fields

    public List<Sprite> plants;//База данных объектов, которые будут восприниматься редактором, как растения, которые можно располагать на поверхности земли

    #endregion //fields
}
