using UnityEngine;
using System.Collections;

/// <summary>
/// Базовый визуализатор для летающих персонажей
/// </summary>
public class FlyVisual : CharacterVisual
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
        visualFunctions.Add("idle", Idle);
        visualFunctions.Add("fly", Fly);
        visualFunctions.Add("attack", Attack);
        visualFunctions.Add("appear", Appear);
    }

    /// <summary>
    /// Анимировать появление
    /// </summary>
    protected virtual void Appear(string id, int argument)
    {
        StartVisualRoutine(8, 1.6f);
        anim.Play("Appear");
    }

    /// <summary>
    /// Отобразить стояние
    /// </summary>
    protected virtual void Idle(string id, int argument)
    {
        if (employment <= 6)
        {
            return;
        }
        anim.Play("Idle");
    }

    /// <summary>
    /// Отобразить полёт
    /// </summary>
    protected virtual void Fly(string id, int argument)
    {
        if (employment <= 7)
            return;
        anim.Play("Fly");
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
        anim.Play("Fly");
        if (id == "")
            StartCoroutine(AttackProcess("Attack"));
        else
            StartCoroutine(AttackProcess(id));
        StartVisualRoutine(5, argument != 0 ? argument / 100f : attackTime);
    }

    IEnumerator AttackProcess(string attackName)
    {
        yield return new WaitForSeconds(.05f);
        anim.Play(attackName);
    }

}
