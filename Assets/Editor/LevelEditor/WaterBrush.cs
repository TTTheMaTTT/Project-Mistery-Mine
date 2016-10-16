using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Водная кисть, которая задаёт, какую воду рисовать
/// </summary>
public class WaterBrush : ScriptableObject
{
    #region fields

    public string brushName;
    public Sprite waterSprite, waterAngleSprite;//Наполнение воды
    public List<GameObject> waterObjects;

    #endregion //fields

    #region parametres

    private bool incomplete;//Способ указания того, что кисть ещё дорабатывается
    public bool Incomplete { get { return incomplete; } set { incomplete = value; } }

    #endregion //parametres

    public bool ContainsSprite(Sprite _sprite)
    {
        return (_sprite == waterSprite)||(_sprite == waterAngleSprite);
    }

}
