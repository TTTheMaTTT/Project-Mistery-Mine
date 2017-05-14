using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер, управляющий ифритом
/// </summary>
public class IfritController : BatController
{

    #region consts

    protected const float wetDamageCoof = .5f;//Коэффициент, на который домножается урон, когда персонаж находится в мокром состоянии

    protected const float groundLevelOffset = .5f;//Если рядом с призраком находится с землей, и он видит игрока, то призрак должен подняться над землёй

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

    protected float groundLevel = 0f;//Уровень земли, от которого движется ифрит
    bool groundLevelSetted = false;//Был ли установлен уровень земли, от которого собирается отталкиваться ифрит

    #endregion //parametres

    protected override void Start()
    {
        base.Start();
        StartCoroutine("StartBurningProcess");
        anim.AddSoundSource();
        anim.PlaySoundWithIndex("Fire", 0);
        anim.MakeSoundSourceCycle(true, 2);
    }

    /// <summary>
    /// Двигаться наверх
    /// </summary>
    protected virtual void MoveUp()
    {
        Vector2 targetVelocity = Vector2.up * speed * speedCoof;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);
    }

    /// <summary>
    /// Процесс совершения атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        Animate(new AnimationEventArgs("attack", "", Mathf.RoundToInt(100f * (attackParametres.preAttackTime))));
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
            missileHitBox.AttackerInfo = new AttackerClass(gameObject,AttackTypeEnum.range);
        }
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);

        yield return new WaitForSeconds(attackRate);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
        groundLevelSetted = false;
    }

    /// <summary>
    /// Завершить атаку
    /// </summary>
    protected override void StopAttack()
    {
        base.StopAttack();
        groundLevelSetted = false;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    /// <param name="hitData">Данные урона</param>
    public override void TakeDamage(HitParametres hitData)
    {
        if (hitData.attackPower == 0 && hitData.damageType == DamageType.Fire)//Ифрит не подвергается действия лавы или огня
            return;
        base.TakeDamage(hitData);
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    /// <param name="hitData">Данные урона</param>
    /// <param name="ignoreInvul">Игнорирует ли прошедшая атака инвул персонажа</param>
    public override void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
        if (hitData.attackPower == 0 && hitData.damageType == DamageType.Fire)//Ифрит не подвергается действия лавы или огня
            return;
        base.TakeDamage(hitData, ignoreInvul);
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
            Animate(new AnimationEventArgs("stopBurning"));
            Animate(new AnimationEventArgs("startBurning"));
            return;
        }
        if (GetBuff("WetProcess") != null)
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
        Animate(new AnimationEventArgs("spawnEffect", "SteamCloud", 0));
        Animate(new AnimationEventArgs("stopBurning"));
        Animate(new AnimationEventArgs("startWet"));
        yield return new WaitForSeconds(_time);
        attackParametres.damage /= wetDamageCoof;
        Animate(new AnimationEventArgs("stopWet"));
        Animate(new AnimationEventArgs("stopBurning"));
        Animate(new AnimationEventArgs("startBurning"));
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
        Animate(new AnimationEventArgs("stopBurning"));
        Animate(new AnimationEventArgs("startBurning"));
    }

    /// <summary>
    /// Процесс, инициирющий поджог ифрита в начале игры
    /// </summary>
    /// <returns></returns>
    IEnumerator StartBurningProcess()
    {
        yield return new WaitForSeconds(.5f);
        if (GetBuff("wetProcess") == null && GetBuff("WetProcess") == null)
        {
            Animate(new AnimationEventArgs("stopBurning"));
            Animate(new AnimationEventArgs("startBurning"));
        }
    }

    #endregion //damageEffects

    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        StopCoroutine("PipProcess");
    }

    protected override void BecomePatrolling()
    {
        base.BecomeAgressive();
        StopCoroutine("PipProcess");
    }

    //Функция, реализующая агрессивное состояние ИИ
    protected override void AgressiveBehavior()
    {
        Vector2 pos = transform.position;

        if (groundLevelSetted)
        {
            if (pos.y - groundLevel < groundLevelOffset)
                MoveUp();
            else
            {
                groundLevelSetted = false;
                StopMoving();
            }
            Animate(new AnimationEventArgs("fly"));
            return;
        }
        if (mainTarget.exists && employment>2)
        {
            if (currentTarget.exists)
            {
                Vector2 targetPosition = currentTarget;
                if (waypoints == null)
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
                            if (Physics2D.Raycast(pos, Vector2.right * (int)orientation, minDistance, LayerMask.GetMask(gLName)))
                            {
                                groundLevel = pos.y;
                                groundLevelSetted = true;
                            }
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
                            if (Physics2D.Raycast(pos, Vector2.right * (int)orientation, minDistance, LayerMask.GetMask(gLName)))
                            {
                                groundLevel = pos.y;
                                groundLevelSetted = true;
                            }
                        }
                    }
                    else
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
                }
                else
                {
                    if (!currentTarget.exists)
                        currentTarget = new ETarget(waypoints[0].cellPosition);

                    targetPosition = currentTarget;
                    pos = transform.position;
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
                    if (currentTarget != mainTarget && Vector2.SqrMagnitude(currentTarget - pos) < batSize * batSize)
                    {
                        waypoints.RemoveAt(0);
                        if (waypoints.Count == 0)
                        {
                            waypoints = null;
                            currentTarget.Exists = false;
                            //Достигли конца маршрута
                            if (Vector3.Distance(beginPosition, transform.position) < batSize)
                            {
                                transform.position = beginPosition;
                                Animate(new AnimationEventArgs("idle"));
                                BecomeCalm();
                            }
                            else
                                GoHome();//Никого в конце маршрута не оказалось, значит, возвращаемся домой
                        }
                        else
                        {
                            //Продолжаем следование
                            currentTarget = new ETarget(waypoints[0].cellPosition);
                        }
                    }
                }
            }
        }
        Animate(new AnimationEventArgs("fly"));
    }

    /// <summary>
    /// Перейти в оптимизированный режим работы
    /// </summary>
    protected override void ChangeBehaviorToOptimized()
    {
        base.ChangeBehaviorToOptimized();
        Animate(new AnimationEventArgs("stopBurning"));
    }

    /// <summary>
    /// Перейти в активный режим работы
    /// </summary>
    protected override void ChangeBehaviorToActive()
    {
        base.ChangeBehaviorToActive();
        Animate(new AnimationEventArgs("stopBurning"));
        Animate(new AnimationEventArgs("startBurning"));
    }

}
