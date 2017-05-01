using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Визуальное отображение босса - таинственного незнакомца
/// </summary>
public class StrangerVisual : GroundMoveVisual
{

    /// <summary>
    /// Сформировать словари визуальных действий
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("teleportBegin", TeleportBegin);
        visualFunctions.Add("teleportEnd", TeleportEnd);
        visualFunctions.Add("teleportBlink", TeleportBlink);
    }

    /// <summary>
    /// Функция, отвечающая за перемещение персонажа на земле
    /// </summary>
    protected override void GroundMove(string id, int argument)
    {
        if (employment <= 6)
        {
            return;
        }
            anim.Play("Idle");
    }

    /// <summary>
    /// Анимировать начало телепорта
    /// </summary>
    protected virtual void TeleportBegin(string id, int argument)
    {
        anim.Play(id == "fast" ? "FastTeleportBegin" : id == "superFast"? "SuperFastTeleportBegin": "TeleportBegin");
        StartVisualRoutine(5, argument / 100f);
    }

    /// <summary>
    /// Анимировать конец телепорта
    /// </summary>
    protected virtual void TeleportEnd(string id, int argument)
    {
        anim.Play(id == "fast" ? "FastTeleportEnd" : "TeleportEnd");
        StartVisualRoutine(5, argument / 100f);
    }

    /// <summary>
    /// Анимировать телепортационное мигание
    /// </summary>
    protected virtual void TeleportBlink(string id, int argument)
    {
        anim.Play("TeleportBlink");
        anim.ForceStateNormalizedTime(0f);
        StartVisualRoutine(5, argument / 100f);
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
