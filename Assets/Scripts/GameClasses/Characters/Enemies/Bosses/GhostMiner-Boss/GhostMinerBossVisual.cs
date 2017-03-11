using UnityEngine;
using System.Collections;

/// <summary>
/// Визуальное отображения босса призраков шахтёров
/// </summary>
public class GhostMinerBossVisual : GroundMoveVisual
{

    #region consts

    protected const int invulTimes = 3;

    #endregion //consts

    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("stopAttack", StopAttack);
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
    /// Анимировать атаку
    /// </summary>
    protected override void Attack(string id, int argument)
    {
        if (employment < 8)
        {
            return;
        }
        if (id == string.Empty)
            anim.Play("Attack");
        else
            anim.Play(id);
        StartVisualRoutine(5, 1f);
    }

    /// <summary>
    /// Остановить отображение атаки
    /// </summary>
    protected virtual void StopAttack(string id, int argument)
    {
        employment = maxEmployment;
        GroundMove("", 0);
    }

    /// <summary>
    /// Отобразить получение урона миганием персонажа
    /// </summary>
    public override void Blink(bool blink=true)
    {
        GetComponent<SpriteRenderer>().enabled = true;
        StopCoroutine("BlinkProcess");
        StartCoroutine("BlinkProcess");
    }

    /// <summary>
    /// Процесс мигания
    /// </summary>
    protected override IEnumerator BlinkProcess()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        for (int i = 0; i < invulTimes; i++)
        {
            yield return new WaitForSeconds(invulBlinkTime);
            sprite.enabled = false;
            yield return new WaitForSeconds(invulBlinkTime / 2);
            sprite.enabled = true;
        }
    }
}
