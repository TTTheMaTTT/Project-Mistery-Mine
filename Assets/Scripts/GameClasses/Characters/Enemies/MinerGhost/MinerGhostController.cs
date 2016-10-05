using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер призрака шахтёра
/// </summary>
public class MinerGhostController : SpiderController
{
    #region consts

    protected const float sightRadius = 5f, sightOffset=0.1f;

    #endregion //consts

    protected override void FixedUpdate()
    {
        if (agressive && target != null && employment > 2)
        {
            Vector3 targetPosition = target.transform.position;
            if (Vector2.Distance(targetPosition, transform.position) > attackDistance)
            {
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
            }
            else
            {
                Attack();
            }
        }

        else if (!agressive)
        {
            Analyse();
        }

        Animate(new AnimationEventArgs("groundMove"));

    }

    /// <summary>
    /// Оценить окружающую обстановку
    /// </summary>
    protected override void Analyse()
    {
        base.Analyse();
        RaycastHit2D hit = Physics2D.Raycast(transform.position + (int)orientation * transform.right * sightOffset, (int)orientation * transform.right, sightRadius, LayerMask.GetMask("character"));
        if (hit)
        {
            if (hit.collider.gameObject.tag == "player")
            {
                BecomeAgressive();
            }
        }
    }

}
