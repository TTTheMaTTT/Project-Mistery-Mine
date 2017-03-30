using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер ледяного мертвеца
/// </summary>
public class FrozenDeadmanController : ShooterController
{

    /// <summary>
    /// Передвижение
    /// </summary>
    /// <param name="_orientation">Направление движения (влево/вправо)</param>
    protected override void Move(OrientationEnum _orientation)
    {
        bool wallInFront = wallCheck.WallInFront;
        Vector2 targetVelocity = wallInFront ? new Vector2(0f, rigid.velocity.y) : new Vector2((int)orientation * speed * speedCoof * (grounded? 1f:1.5f), rigid.velocity.y);
        rigid.velocity = wallInFront ? Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration) : targetVelocity;

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    #region effects

    /// <summary>
    /// Нельзя охладить ледяного мертвеца
    /// </summary>
    protected override void BecomeCold(float _time)
    {}

    /// <summary>
    /// Нельзя заморозить ледяного мертвеца
    /// </summary>
    protected override void BecomeFrozen(float _time)
    {}

    #endregion //effects

}
