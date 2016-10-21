using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// База данных по видам препятствий, используемых в редакторе уровней
/// </summary>
public class ObstacleBase : ScriptableObject
{

    #region fields

    public List<GameObject> obstacles=new List<GameObject>();//Игровые объекты, что используются для создания препятствий

    #endregion //fields

}
