using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// База данных по различным видам лестниц и лиан, по которым можно лезть
/// </summary>
public class LadderBase : ScriptableObject
{
    #region fields

    public List<GameObject> ladders;//База данных объектов, которые будут восприниматься редактором, как части лестниц

    #endregion //fields

}