using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт, управляющий призраком-миньоном
/// </summary>
public class GhostMinionController : GhostController
{

    #region consts

    protected float appearTime = 1.6f;//Время появления персонажа

    #endregion //consts

    #region parametres

    public override int ID
    {
        get
        {
            return base.ID;
        }

        set
        {
#if !UNITY_EDITOR
            StartCoroutine("AppearProcess");
#endif //!UNITY_EDITOR
            base.ID = value;
        }
    }
    protected bool appearing = false;

    #endregion //parametres

    protected override void FixedUpdate()
    {
        if (!immobile)
            base.FixedUpdate();
        if (!appearing)
            Animate(new AnimationEventArgs("fly"));

    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Процесс появления призрака
    /// </summary>
    /// <returns></returns>
    protected IEnumerator AppearProcess()
    {
        immobile = true;
        appearing = true;
        if (selfHitBox != null)
            selfHitBox.ResetHitBox();
        yield return new WaitForSeconds(.05f);
        Animate(new AnimationEventArgs("appear"));
        yield return new WaitForSeconds(appearTime);
        immobile = false;
        appearing = false;
        if (selfHitBox != null)
            selfHitBox.SetHitBox(attackParametres);
    }

    #region behaviorActions

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        if (!mainTarget.exists || employment<=5)
            return;
        Vector2 targetPosition = mainTarget;
        Vector2 pos = transform.position;
        Vector2 direction = targetPosition - pos;
        float sqDistance = direction.sqrMagnitude;
        if (sqDistance > .001f)
        {
            col.isTrigger = true;
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(direction.x)));
        }
        else StopMoving();
  
    }

    #endregion //behaviorActions

}
