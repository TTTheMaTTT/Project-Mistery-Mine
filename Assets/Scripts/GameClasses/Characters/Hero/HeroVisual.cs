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

    #endregion //consts

    #region parametres

    protected bool dead = false;

    #endregion //parametres

    protected override void Initialize()
    {
        base.Initialize();
        dead = false;
    }

    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("shoot", Shoot);
        visualFunctions.Add("holdAttack", HoldAttack);
        visualFunctions.Add("releaseAttack", ReleaseAttack);
        visualFunctions.Add("flip", Flip);
        visualFunctions.Add("fall", Fall);
        visualFunctions.Add("waterSplash", WaterSplash);
        visualFunctions.Add("battleCry", BattleCry);
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
        anim.Play("Idle");
        if (id == string.Empty)
            anim.Play("Attack");
        else
            anim.Play(id + "Attack");
        StartVisualRoutine(5, argument/10f);
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
        anim.Play("Idle");
        if (id == string.Empty)
            anim.Play("Shoot");
        else
            anim.Play(id + "Shoot");
        StartVisualRoutine(5, argument/10f);
    }

    /// <summary>
    /// Анимировать задержку атаки
    /// </summary>
    protected virtual void HoldAttack(string id, int argument)
    {
        if (employment < 8)
        {
            return;
        }
        anim.Play("Idle");
        if (id == string.Empty)
            anim.Play("Attack");
        else
            anim.Play(id + "Hold");
        employment = Mathf.Clamp(employment - 5, 0, maxEmployment);
    }

    /// <summary>
    /// Анимировать высвобождение атаки после задержки
    /// </summary>
    protected virtual void ReleaseAttack(string id, int argument)
    {
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);
        if (id!="")
        {
            anim.Play("Idle");
            anim.Play(id + "Release");
            StartVisualRoutine(5, argument / 10f);
        }
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
        StartVisualRoutine(5, flipTime);
    }

    /// <summary>
    /// Анимировать получение урона
    /// </summary>
    protected override void Hitted(string id, int argument)
    {
        /*if (argument==0 && employment > 0)//Считаем, что employment равен нулю только при заморозке, когда персонаж не может быть анимируем
        {
            StopAllCoroutines();
            employment = maxEmployment;
            anim.Play("Hitted");
            StartVisualRoutine(5, hittedTime);
        }*/
        if (effectSystem != null)
            effectSystem.ResetEffects();
    }

    /// <summary>
    /// Начать процесс мигания при инвуле
    /// </summary>
    public virtual void InvulBlink()
    {
        StartCoroutine(InvulProcess());
    }
     
    /// <summary>
    /// Процесс мигания
    /// </summary>
    protected virtual IEnumerator InvulProcess()
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
    /// Отобразить смерть
    /// </summary>
    protected override void Death(string id, int argument)
    {
        StopAllCoroutines();
        StopStun(id, argument);
        StopBurning(id, argument);
        StopPoison(id, argument);
        StopCold(id, argument);
        StopWet(id, argument);
        StopFrozen(id, argument);
        if (effectSystem != null)
            effectSystem.ResetEffects();
        employment = 0;
        if (id == "fire")
        {
            effectSystem.SpawnEffect("AshDrop");
            sRenderer.enabled = false;
            this.enabled = false;
        }
        else
        {
            if (dead)
                return;
            PlayAdditionalSound("Death");
            anim.Play("Death2");
        }
        dead = true;
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
        PlaySound("Fall");
    }

    protected virtual void BattleCry(string id, int argument)
    {
        if (effectSystem != null)
        {
            effectSystem.SpawnEffect("BattleCry");
        }
    }

}