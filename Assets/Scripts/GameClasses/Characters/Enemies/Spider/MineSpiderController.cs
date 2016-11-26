using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, управляющий пауком шахты - агрессивной версией обычного паука
/// </summary>
public class MineSpiderController : SpiderController
{

    protected override void FixedUpdate()
    {
        if (!immobile)
        {
            if (agressive && mainTarget != null && employment > 2)
            {
                Vector3 targetPosition = mainTarget.transform.position;
                if (Vector2.Distance(targetPosition, transform.position) > attackDistance)
                {
                    if (!wallCheck.WallInFront() && (precipiceCheck.WallInFront()))
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
                    else if ((targetPosition - transform.position).x * (int)orientation < 0f)
                        Turn();
                }
                else
                {
                    if ((targetPosition - transform.position).x * (int)orientation < 0f)
                        Turn();
                    Attack();
                }
            }
            else if (!agressive)
            {

                if ((Vector2.Distance(waypoint, transform.position) < attackDistance) || (wallCheck.WallInFront() || !(precipiceCheck.WallInFront())))
                {
                    Turn((OrientationEnum)(-1 * (int)orientation));
                    Patrol();
                }
                else
                {
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(waypoint.x - transform.position.x)));
                }
            }
        }
        else if (moveOut)
        {
            MoveOut();
        }
        Animate(new AnimationEventArgs("groundMove"));
        Analyse();
    }

    /// <summary>
    /// Оценить окружающую обстановку
    /// </summary>
    protected override void Analyse()
    {
        if (!agressive)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position + (int)orientation * transform.right * sightOffset, (int)orientation * transform.right,
                                                                                                                            sightRadius, LayerMask.GetMask("character"));
            if (hit)
            {
                if (hit.collider.gameObject.tag == "player")
                {
                    BecomeAgressive();
                }
            }
        }
        else
            base.Analyse();
    }
}
