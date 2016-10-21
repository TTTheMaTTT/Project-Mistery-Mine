using UnityEngine;
using System.Collections;

/// <summary>
/// Базовый вижуал нелетающих персонажей
/// </summary>
public class GroundMoveVisual : CharacterVisual
{
    #region consts

    protected const float minSpeed = .2f;

    protected const float attackTime = .5f;

    #endregion //consts

    #region fields

    protected Rigidbody2D rigid;

    #endregion //fields

    protected override void Initialize()
    {
        base.Initialize();
        rigid = GetComponentInParent<Rigidbody2D>();
    }

    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("groundMove", GroundMove);
        visualFunctions.Add("moveForward", MoveForward);
        visualFunctions.Add("attack", Attack);
    }

    /// <summary>
    /// Функция, отвечающая за перемещение персонажа на земле
    /// </summary>
    protected virtual void GroundMove(string id, int argument)
    {
        if (employment <= 6)
        {
            return;
        }
        if (Mathf.Abs(rigid.velocity.x) > minSpeed)
        {
            anim.Play("Run");
        }
        else
        {
            anim.Play("Idle");
        }
    }

    protected virtual void MoveForward(string id, int argument)
    {
        anim.Play("Run");
    }

    /// <summary>
    /// Анимировать атаку
    /// </summary>
    protected virtual void Attack(string id, int argument)
    {
        if (employment < 8)
        {
            return;
        }
        anim.Play("Attack");
        StartCoroutine(VisualRoutine(5, .5f));
    }

}
