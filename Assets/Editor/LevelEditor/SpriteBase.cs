using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// База данных спрайтов, используемых в обычном режиме рисования
/// </summary>
public class SpriteBase : ScriptableObject
{

    #region fields

    public List<Sprite> sprites = new List<Sprite>();

    #endregion //fields

}
