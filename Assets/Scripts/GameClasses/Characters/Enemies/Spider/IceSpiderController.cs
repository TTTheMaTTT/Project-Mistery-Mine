using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер ледяного паука
/// </summary>
public class IceSpiderController: SpiderController
{
    #region effects

    /// <summary>
    /// Нельзя охладить ледяного паука
    /// </summary>
    protected override void BecomeCold(float _time)
    { }

    /// <summary>
    /// Нельзя заморозить ледяного паука
    /// </summary>
    protected override void BecomeFrozen(float _time)
    { }

    #endregion //effects
}
