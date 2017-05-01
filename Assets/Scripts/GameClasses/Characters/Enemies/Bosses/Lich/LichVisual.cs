using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Визуальное отображение лича
/// </summary>
public class LichVisual : FlyVisual
{

    /// <summary>
    /// Анимировать полёт
    /// </summary>
    protected override void Fly(string id, int argument)
    {
        if (employment <= 7)
            return;
        anim.Play(rigid.velocity.magnitude > minSpeed?"Fly":"Idle");
    }

    /// <summary>
    /// Анимировать смерть
    /// </summary>
    protected override void Death(string id, int argument)
    {
        anim.Play("Death");
        employment = 0;
    }

}
