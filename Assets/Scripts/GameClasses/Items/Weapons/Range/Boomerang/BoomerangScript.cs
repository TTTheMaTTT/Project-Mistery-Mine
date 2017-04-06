using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, управляющий бумерангом
/// </summary>
public class BoomerangScript : MonoBehaviour
{

    #region consts

    protected const float eps = .008f;
    protected const float groundRadius = .01f;
    protected const string gLName = "ground";

    #endregion //consts

    #region fields

    protected Rigidbody2D rigid;
    protected HitBox hitBox;//Чем атакует бумеранг

    protected HeroController hero;

    #endregion //fields

    #region parametres

    protected int phase;
    protected Vector3 currentTarget;//К какой позиции летит бумеранг

    protected float speed, acceleration;//Скорость и ускорение бумеранга

    #endregion //parametres

    protected virtual void FixedUpdate()
    {
        if (phase == 1)
        {
            if (Physics2D.OverlapCircle(transform.position, groundRadius, LayerMask.GetMask(gLName)))
                ChangePhase();
        }
        else if (phase == 2)
        {
            SetTarget(hero.transform.position);
        }
        rigid.velocity = Vector3.Lerp(rigid.velocity, (currentTarget - transform.position).normalized * speed, acceleration * Time.fixedDeltaTime);
        if (Vector3.SqrMagnitude(currentTarget - transform.position) < eps * eps)
            ChangePhase();
    }

    public virtual void SetBoomerang(float _speed, float _acceleration)
    {
        rigid = GetComponent<Rigidbody2D>();
        hitBox = GetComponentInChildren<HitBox>();
        hitBox.AttackEventHandler += HandleAttackProcess;
        speed = _speed;
        acceleration = _acceleration;
        rigid.velocity = (currentTarget - transform.position).normalized * speed;
        rigid.gravityScale = 0f;
        phase = 1;
        hero = SpecialFunctions.Player.GetComponent<HeroController>();
    }

    /// <summary>
    /// Установить новую цель для бумеранга
    /// </summary>
    public virtual void SetTarget(Vector3 newTarget)
    {
        currentTarget = newTarget;
    }

    public virtual void SetHitBox(HitParametres _hit, List<string> enemies, AttackerClass attacker)
    {
        hitBox.SetEnemies(enemies);
        hitBox.SetHitBox(_hit);
        hitBox.AttackerInfo = attacker;
    }

    protected virtual void ChangePhase()
    {
        if (phase == 1)//Если бумеранг всё ещё летит к своей цели
        {
            hitBox.AttackEventHandler -= HandleAttackProcess;
            hitBox.ResetHitBox();
            currentTarget = hero.transform.position;
            phase++;
        }
        else if (phase == 2)//Если бумеранг летит обратно к персонажу
        {
            WeaponClass weapon = hero.CurrentWeapon;
            if (weapon != null ? weapon is BoomerangClass : false)
            {
                ((BoomerangClass)weapon).ReloadWeapon();
            }
            Destroy(gameObject);
        }
    }

    #region events

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected void HandleAttackProcess(object sender, HitEventArgs e)
    {
        ChangePhase();
    }

    #endregion //events

}