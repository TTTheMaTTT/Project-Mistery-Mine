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
        visualFunctions.Add("ladderMove", LadderMove);
        visualFunctions.Add("setLadderMove", SetLadderMove);
        visualFunctions.Add("shoot", Shoot);
        visualFunctions.Add("flip", Flip);
        visualFunctions.Add("fall", Fall);
        visualFunctions.Add("waterSplash", WaterSplash);
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
            anim.speed=1f;
        }
        else
        {
            anim.speed=0f;
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
            anim.speed=1f;
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

    protected override void GroundMove(string id, int argument)
    {
        if (id == "crouching")
        {
            if (employment <= 6)
            {
                return;
            }
            if (Mathf.Abs(rigid.velocity.x) > minSpeed)
            {
                anim.Play("CrouchMove");
            }
            else
            {
                anim.Play("Crouch");
            }
        }
        else
        {
            base.GroundMove(id, argument);
        }
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
            anim.Play(id + "Attack");
        StartCoroutine(VisualRoutine(5, argument/10f));
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
        if (id == string.Empty)
            anim.Play("Shoot");
        else
            anim.Play(id + "Shoot");
        StartCoroutine(VisualRoutine(5, argument/10f));
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

    /// <summary>
    /// Отобразить брызги воды
    /// </summary>
    protected virtual void WaterSplash(string id, int argument)
    {
        if (effectSystem != null)
            effectSystem.SpawnEffect("water splash");
    }

    /// <summary>
    /// Отобразить падение
    /// </summary>
    protected virtual void Fall(string id, int argument)
    {
        if (effectSystem != null)
        {
            effectSystem.SpawnEffect("dust");
            effectSystem.FallEffect();
        }
    }

}