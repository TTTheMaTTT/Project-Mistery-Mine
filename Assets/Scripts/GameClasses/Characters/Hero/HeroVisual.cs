using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, отвечающий за анимирование главного героя
/// </summary>
public class HeroVisual : GroundMoveVisual
{

    #region consts

    private const float shootTime = .4f, flipTime = .4f;

    protected const int invulTimes = 3;
    protected const float invulBlinkTime = .2f;

    #endregion //consts

    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("airMove", AirMove);
        visualFunctions.Add("shoot", Shoot);
        visualFunctions.Add("flip", Flip);
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
    /// Анимировть выстрел
    /// </summary>
    protected virtual void Shoot(string id, int argument)
    {
        if (employment <8)
        {
            return;
        }
        anim.Play("Shoot");
        StartCoroutine(VisualRoutine(5, shootTime));
    }

    /// <summary>
    /// Анимировать кувырок
    /// </summary>
    protected virtual void Flip(string id, int argument)
    {
        if (employment < 8)
        {
            return;
        }
        anim.Play("Flip");
        StartCoroutine(VisualRoutine(5, flipTime));
    }

    /// <summary>
    /// Анимировать получение урона
    /// </summary>
    protected override void Hitted(string id, int argument)
    {
        StopAllCoroutines();
        employment = maxEmployment;
        anim.Play("Hitted");
        StartCoroutine(VisualRoutine(5, hittedTime));
    }
    
    public virtual void Blink()
    {
        StartCoroutine(BlinkProcess());
    }

    /// <summary>
    /// Процесс мигания
    /// </summary>
    protected virtual IEnumerator BlinkProcess()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        for (int i = 0; i < invulTimes; i++)
        {
            yield return new WaitForSeconds(invulBlinkTime);
            sprite.enabled = false;
            yield return new WaitForSeconds(invulBlinkTime/2);
            sprite.enabled = true;
        }
    }

}