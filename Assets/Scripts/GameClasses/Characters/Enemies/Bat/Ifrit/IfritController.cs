﻿using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер, управляющий ифритом
/// </summary>
public class IfritController : BatController
{

    #region consts

    protected const float wetDamageCoof = .5f;//Коэффициент, на который домножается урон, когда персонаж находится в мокром состоянии

    #endregion //consts

    #region fields

    [SerializeField]
    protected GameObject missile;//Снаряды стрелка

    #endregion //fields

    #region parametres

    protected virtual Vector2 shootPosition { get { return new Vector2(0.062f, -0.021f); } }//Откуда стреляет персонаж
    protected virtual float attackRate { get { return 3f; } }//Сколько секунд проходит между атаками

    public override bool Waiting { get { return base.Waiting; } set { base.Waiting = value; StopCoroutine("AttackProcess"); Animate(new AnimationEventArgs("stop")); } }

    [SerializeField]
    protected float missileSpeed = 3f;//Скорость снаряда после выстрела

    #endregion //parametres


    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        Animate(new AnimationEventArgs("attack", "", Mathf.RoundToInt(10 * (attackParametres.preAttackTime))));
        StartCoroutine("AttackProcess");
    }

    /// <summary>
    /// Процесс совершения атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.preAttackTime);

        Vector2 pos = transform.position;
        Vector2 _shootPosition = pos + new Vector2(shootPosition.x * (int)orientation, shootPosition.y);
        Vector2 direction = (currentTarget - pos).x * (int)orientation >= 0f ? (currentTarget - _shootPosition).normalized : (int)orientation * Vector2.right;
        GameObject newMissile = Instantiate(missile, _shootPosition, Quaternion.identity) as GameObject;
        Rigidbody2D missileRigid = newMissile.GetComponent<Rigidbody2D>();
        missileRigid.velocity = direction * missileSpeed;
        HitBoxController missileHitBox = missileRigid.GetComponentInChildren<HitBoxController>();
        if (missileHitBox != null)
        {
            missileHitBox.SetEnemies(enemies);
            missileHitBox.SetHitBox(new HitParametres(attackParametres));
            missileHitBox.allyHitBox = loyalty == LoyaltyEnum.ally;
            missileHitBox.Attacker = gameObject;
        }
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);

        yield return new WaitForSeconds(attackRate);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    public override void TakeDamage(float damage, DamageType _dType, bool _microstun = true)
    {
        base.TakeDamage(damage, _dType, _microstun);
        if (_microstun)
            Animate(new AnimationEventArgs("stop"));
    }

    public override void TakeDamage(float damage, DamageType _dType, bool ignoreInvul, bool _microstun)
    {
        base.TakeDamage(damage, _dType, ignoreInvul, _microstun);
        if (_microstun)
            Animate(new AnimationEventArgs("stop"));
    }

    /// <summary>
    /// Подготовить данные для ведения деятельности в следующей модели поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        Animate(new AnimationEventArgs("stop"));
        StopCoroutine("AttackProcess");
    }


    #region damageEffects

    /// <summary>
    /// Ифрита нельзя поджечь... можно только высушить
    /// </summary>
    protected override void BecomeBurning(float _time)
    {
        if (GetBuff("FrozenProcess") != null)
        {
            //Если персонажа подожгли, когда он был заморожен, то он отмараживается и не получает никакого урона от огня, так как считаем, что всё тепло ушло на разморозку
            StopFrozen();
            return;
        }
        if (GetBuff("FrozenWet") != null)
        {
            //Если персонажа подожгли, когда он был промокшим, то он высыхает
            StopWet();
            return;
        }
    }

    /// <summary>
    /// Процесс промокшести
    /// </summary>
    /// <param name="_time">Длительность процесса</param>
    /// <returns></returns>
    protected override IEnumerator WetProcess(float _time)
    {
        AddBuff(new BuffClass("WetProcess", Time.fixedTime, _time));
        attackParametres.damage *= wetDamageCoof;
        Animate(new AnimationEventArgs("startWet"));
        yield return new WaitForSeconds(_time);
        speed /= wetDamageCoof;
        Animate(new AnimationEventArgs("stopWet"));
        RemoveBuff("WetProcess");
    }

    /// <summary>
    /// Высушиться
    /// </summary>
    protected override void StopWet()
    {
        if (GetBuff("WetProcess") == null)
            return;
        StopCoroutine("WetProcess");
        attackParametres.damage /= wetDamageCoof;
        RemoveBuff("WetProcess");
        Animate(new AnimationEventArgs("stopWet"));
    }

    #endregion //damageEffects

    //Функция, реализующая агрессивное состояние ИИ
    protected override void AgressiveBehavior()
    {
        if (mainTarget.exists && employment>2)
        {
            if (currentTarget.exists)
            {
                Vector2 targetPosition = currentTarget;
                Vector2 pos = transform.position;
                if (currentTarget == mainTarget)
                {
                    float sqDistance = Vector2.SqrMagnitude(targetPosition - pos);
                    if (sqDistance < waitingNearDistance * waitingNearDistance)
                    {
                        if (waiting)
                            MoveAway((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(pos.x - targetPosition.x)));
                        else if (employment > 8)
                        {
                            if ((targetPosition - pos).x * (int)orientation < 0f)
                                Turn();
                            StopMoving();
                            Attack();
                        }
                    }
                    else if (sqDistance < waitingFarDistance * waitingFarDistance)
                    {
                        StopMoving();
                        if ((targetPosition - pos).x * (int)orientation < 0f)
                            Turn();
                        if (!waiting && employment > 8)
                        {
                            StopMoving();
                            Attack();
                        }
                    }
                    else
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
                }
                else
                {
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
                    if (currentTarget != mainTarget && Vector2.SqrMagnitude(targetPosition - pos) < batSize * batSize)
                    {
                        currentTarget = FindPath();
                    }
                }
            }
        }
        Animate(new AnimationEventArgs("fly"));
    }

}