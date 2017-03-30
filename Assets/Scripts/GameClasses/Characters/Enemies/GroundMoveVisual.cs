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
        visualFunctions.Add("attack", Attack);
        visualFunctions.Add("airMove", AirMove);
        visualFunctions.Add("ladderMove", LadderMove);
        visualFunctions.Add("setLadderMove", SetLadderMove);
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

    /// <summary>
    /// Функция, отвечающая за перемещение персонажа по лестнице
    /// </summary>
    protected virtual void LadderMove(string id, int argument)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("LadderMove"))
            anim.Play("LadderMove");
        if (Mathf.Abs(rigid.velocity.y) >= minSpeed)
        {
            anim.speed = 1f;
        }
        else
        {
            anim.speed = 0f;
        }
    }

    /// <summary>
    /// Перейти в режим визуализации перемещения по лестнице, или выйти из него
    /// </summary>
    protected virtual void SetLadderMove(string id, int argument)
    {
        if (argument == 1)
        {
            anim.Play("LadderMove");
        }
        else
        {
            anim.speed = 1f;
        }
    }

    /// <summary>
    /// Функция, отвечающая за перемещение персонажа в воздухе
    /// </summary>
    protected virtual void AirMove(string id, int argument)
    {
        if (employment <= 6)
        {
            return;
        }
        if (rigid.velocity.y >= 0)
        {
            anim.Play("Jump");
        }
        else
        {
            anim.Play("Fall");
        }
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
        anim.Play("Idle");
        if (id == "")
            anim.Play("Attack");
        else
            anim.Play(id);
        StartVisualRoutine(5, argument != 0? argument/10f : attackTime);
    }

}
