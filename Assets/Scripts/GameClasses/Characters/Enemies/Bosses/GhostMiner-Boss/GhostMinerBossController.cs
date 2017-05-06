using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Контроллер босса призраков-шахтёров
/// </summary>
public class GhostMinerBossController : BossController
{

    #region consts

    protected const float phase1Health = 500f, phase2Health=250f;//С каких значений здоровья начинается вторая и третья фаза босс-битвы?

    protected const float diveDownOffset = 1.9f;//Насколько глубоко босс уходит под землю?

    #endregion //consts

    #region fields

    [SerializeField]protected GameObject coal;//уголь, которым стреляет босс
    protected Collider2D col;//Коллайдер персонажа
    protected HitBoxController selfHitBox;//Хитбокс, который атакует персонажа при соприкосновении с пауком. Этот хитбокс всегда активен и не перемещается

    protected GameObject ghostPosition1, ghostPosition2;//Позиции, с которых призрак совершает горизонтальные атаки

    #endregion //fields

    #region parametres

    public override float Health{get{return base.Health;}set{if (health < phase2Health)bossPhase = 2;else if (health < phase1Health)bossPhase = 1;base.Health = value;}}

    [SerializeField]protected HitParametres touchAttackParametres;//Параметры атаки при соприкосновении
    [SerializeField]protected float attackRate = 1.4f;//Минимальное время, которое должно пройти между 2-мя атаками

    #region coalThrowParametres

    [SerializeField]protected Vector2 coalThrowOffset=new Vector2(.2f,.1f);//Насколько сдвинут прицел босса
    [SerializeField]protected float coalSpeed = .1f;//Скорость снаряда
    [SerializeField]protected float critPossibility = .25f;//Вероятность критической атаки
    [SerializeField]protected float preCritAttackTime = 1.5f;
    [SerializeField]protected float preFastAttackTime=.3f;
    [SerializeField]protected float endFastAttackTime = .5f;//Тайминги атак
    [SerializeField]protected HitParametres coalAttackParametres;//Параметры атаки углём
    [SerializeField]protected float coalThrowCooldown = 2f;

    protected bool coalThrowPossible = true;
    protected bool crit;

    #endregion //coalThrowParametres

    #region coalAreaAttack

    [SerializeField]protected Vector2 coalAreaAttackOffset=new Vector2(.2f, -.3f);//Точка относительно персонажа, откуда бросаются угли при АОЕ атаке
    [SerializeField]protected int coalCount = 5;//Количество бросаемых углей
    [SerializeField]protected float throwAngle = 70f;//Под каким углом бросаются угли
    [SerializeField]protected float throwAngleDeviation = 10f;//Какое максимальное отклонение от throwAngle может быть у брошенного угля
    [SerializeField]protected float areaAttackPreTime = .6f, areaAttackEndTime = .9f;//Тайминги
    [SerializeField]protected float startThrowSpeed = .6f;//Начальная скорость броска
    [SerializeField]protected float coalGravityScale = .7f;//Насколько сильно уголь притягивается к земле?
    [SerializeField]protected float coalAreaAttackCooldown = 4f;

    protected bool coalAreaAttackPossible = true;

    #endregion //coalAreaAttack

    #region diveAttack

    protected bool diving = false;
    protected int divingPhase = 0;
    protected float divingHealth = 500f;//Если здоровье персонажа опустится ниже этого значения, то босс начнёт совершать особую атаку
    protected bool canDive = false;//Может ли персонаж совершать эту атаку?
    protected float divingPrecision = .05f;//Точность определения местоположения противника, когда босс находится под ним

    [SerializeField]protected float diveOutSpeed = 2f;//Скорость персонажа, когда он совершает особую атаку
    [SerializeField]protected float diveHorizontalSpeed=2f;//Скорость персонажа, когда он совершает горизонталную особую атаку
    [SerializeField]protected HitParametres diveHorizontalAttackParametres;//Параметры горизонтальной атаки
    [SerializeField]protected float divingWaitTime = 1.4f;//Время между заныриванием и совершением атаки
    [SerializeField]protected float diveAttackCooldown;

    protected int currentHorizontalAttackPosition=0;

    #endregion //diveAttack

    protected override float attackDistance { get { return 2.5f; } }//Какое максимальное расстояние, при котором персонаж атакует издалека
    protected override float waitingNearDistance {get{return .8f;}}//Какое расстояние считается границей между зонами ближнего и дальнего боя. 
                                                                   //Босс стремится оказаться в зоне дальнего боя, но всё равно имеет атаки и для ближнего
    protected virtual float coalAreaAttackFarDistance { get { return 1.2f; } }//Максимальное расстояние до цели, при котором босс ещё будет совершать массированный выброс угля

    protected int attackTimes = 0;//Сколько раз подряд уже проводилась текущая атака

    #endregion //parametres

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        Animate(new AnimationEventArgs("groundMove"));
    }

    protected override void Initialize()
    {
        bossPhase = 0;
        col = GetComponent<Collider2D>();

        selfHitBox = transform.FindChild("SelfHitBox").GetComponent<HitBoxController>();
        if (selfHitBox != null)
        {
            selfHitBox.SetEnemies(enemies);
            selfHitBox.SetHitBox(touchAttackParametres);
            //selfHitBox.Immobile = true;//На всякий случай
        }
        ghostPosition1 = GameObject.Find("GhostPosition1");
        ghostPosition2 = GameObject.Find("GhostPosition2");

        base.Initialize();

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionIn = NullAreaFunction;
            areaTrigger.triggerFunctionOut = NullAreaFunction;
            areaTrigger.InitializeAreaTrigger();
        }

        rigid.gravityScale = 0f;
    }

    #region movement

    /// <summary>
    /// Функция непосредственного полёта к текущей цели
    /// </summary>
    protected virtual void Move()
    {
        if (!currentTarget.exists)
            return;
        Vector2 targetVelocity = (currentTarget-(Vector2)transform.position).normalized*speed* speedCoof;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if ((int)orientation*targetVelocity.x<0f) 
            Turn();       
    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = new Vector2((int)_orientation * speed * speedCoof, rigid.velocity.y);
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
            Turn(_orientation);
    }

    /// <summary>
    /// Остановиться
    /// </summary>
    protected override void StopMoving()
    {
        rigid.velocity = new Vector2(0f, 0f);
    }


    #endregion //movement

    /// <summary>
    /// Анализировать окружающую обстановку
    /// </summary>
    protected override void Analyse()
    {
        if (behavior!=BehaviorEnum.agressive)
        {
            if (Vector3.Distance(SpecialFunctions.Player.transform.position, transform.position) <= sightRadius)
            {
                MainTarget = new ETarget(SpecialFunctions.player.transform);
                BecomeAgressive();
            }
        }
    }

    #region bossActions

    /// <summary>
    /// Выбрать одну из возможных атак и соврершить её
    /// </summary>
    void ChooseAttack()
    {
        if (!mainTarget.exists)
            return;

        List<string> possibleActions = new List<string>();
        Vector2 targetDistance = mainTarget - (Vector2)transform.position;
        float sDistance = targetDistance.sqrMagnitude;

        //Проверим, какие атаки вообще можно совершить в данный момент
        if (coalThrowPossible && sDistance > waitingNearDistance * waitingNearDistance && sDistance < attackDistance * attackDistance)
            possibleActions.Add("coalThrow");
        if (coalAreaAttackPossible && sDistance < coalAreaAttackFarDistance * coalAreaAttackFarDistance)
            possibleActions.Add("coalAreaAttack");
        if (canDive && health <= divingHealth)
        {
            possibleActions.Add("diveOutAttack");
            possibleActions.Add("diveHorizontalAttack");
        }

        if (possibleActions.Count == 0)
            return;

        if (targetDistance.x * (int)orientation < 0f)
            Turn();
        string nextAction = possibleActions[Random.Range(0, possibleActions.Count)];
        switch (nextAction)
        {
            case "coalThrow":
                {
                    Attack();
                    break;
                }
            case "coalAreaAttack":
                {
                    AreaAttack();
                    break;
                }
            case "diveOutAttack":
                {
                    if (Random.Range(0f,1f)>.6f)
                        SetDiving(true, false);
                    break;
                }
            case "diveHorizontalAttack":
                {
                    if (Random.Range(0f, 1f) > .6f)
                        SetDiving(true, true);
                    break;
                }
            default:
                break;
        }

    }

    /// <summary>
    /// Действия, совершаемые боссом в обычном режиме
    /// </summary>
    protected void UsualBossAction()
    {
        Vector2 pos = transform.position;
        if (employment > 3)
        {
            if (Mathf.Abs(pos.y - beginPosition.y) > divingPrecision)
            {
                col.isTrigger = true;
                rigid.velocity = new Vector2(rigid.velocity.x, speed * speedCoof * Mathf.Sign(beginPosition.y - pos.y));
            }
            else
            {
                col.isTrigger = false;
                transform.position = new Vector2(pos.x, beginPosition.y);
            }
            float sqDistance = Vector3.SqrMagnitude(currentTarget - pos);
            if (sqDistance > attackDistance * attackDistance)
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.X - pos.x)));
            else if (sqDistance < waitingNearDistance * waitingNearDistance)
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(pos.x - currentTarget.X)));//Если текущая цель слишком рядом, то босс пытается убежать 
            if (employment > 8)
                ChooseAttack();
        }
    }

    /// <summary>
    /// Действия, совершаемые боссом в режиме выныривания 
    /// </summary>
    protected void DivingOutAction()
    {
        //Здесь описаны все фазы совершения особой атаки босса
        Vector2 targetPosition = currentTarget;
        Vector2 targetDistance = currentTarget - (Vector2)transform.position;
        Vector2 targetDirection = targetDistance.normalized;

        switch (divingPhase)
        {
            case 1:
                {
                    //Сначала босс опускается под землю
                    rigid.velocity = targetDirection * speed * speedCoof * 2;
                    if ( targetDistance.y > -divingPrecision)
                    {
                        Vector3 pos = transform.position;
                        transform.position = new Vector3(pos.x, targetPosition.y, pos.z);
                        divingPhase++;
                        currentTarget = mainTarget;
                        rigid.velocity = Vector2.zero;
                        StartCoroutine("DivingWaitProcess", attackTimes == 0 ? divingWaitTime : divingWaitTime / 2f);
                    }
                    break;
                }
            case 2:
                {
                    break;
                }
            case 3:
                {
                    transform.position = new Vector2(targetPosition.x, transform.position.y);
                    divingPhase++;
                    currentTarget = new ETarget(targetPosition.x, beginPosition.y);
                    break;
                }
            case 4:
                {
                    //И выходит из-под земли с атакой
                    rigid.velocity = Vector2.up * diveOutSpeed * speedCoof;
                    Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDirection.x)));
                    if (targetDistance.y < divingPrecision)
                    {
                        transform.position = new Vector3(transform.position.x, targetPosition.y);
                        divingPhase++;
                        StartCoroutine("DivingOutAttackProcess");
                    }
                    break;
                }
            case 5:
                {
                    if (employment > 8)
                    {
                        employment = maxEmployment;
                        attackTimes++;
                        if (attackTimes < 2 && bossPhase >= 2)
                        {
                            divingPhase = 1;
                            currentTarget = new ETarget(transform.position + Vector3.down * diveDownOffset);
                        }
                        else
                            SetDiving(false, false);
                        rigid.velocity = Vector2.zero;
                    }
                    break;
                }
            default:
                {
                    break;
                }
        }
    }

    /// <summary>
    /// Действия, совершаемые боссом при горизонтальной атаке
    /// </summary>
    protected void DivingHorizontalAction()
    {
        //Здесь описаны все фазы совершения особой атаки босса (горизонтальная версия)
        Vector2 targetDistance = currentTarget - (Vector2)transform.position;
        Vector2 targetDirection = targetDistance.normalized;
        Vector2 targetPosition = currentTarget;
        switch (divingPhase)
        {
            case 1:
                {
                    //Сначала босс опускается под землю
                    rigid.velocity = targetDirection * speed * speedCoof * 2;
                    if (targetDistance.y > -divingPrecision)
                    {
                        Vector3 pos = transform.position;
                        transform.position = new Vector3(pos.x, targetPosition.y, pos.z);
                        divingPhase++;
                        currentHorizontalAttackPosition = Random.Range(0f, 1f) > .5f ? 1 : 2;
                        currentTarget = new ETarget(currentHorizontalAttackPosition==1 ? ghostPosition1.transform : ghostPosition2.transform);
                        rigid.velocity = Vector2.zero;
                        StartCoroutine("DivingWaitProcess", divingWaitTime);
                    }
                    break;
                }
            case 2:
                {
                    break;
                }
            case 3:
                {
                    transform.position = new Vector2(targetPosition.x, transform.position.y);
                    Turn(currentHorizontalAttackPosition == 1 ? OrientationEnum.right : OrientationEnum.left);
                    divingPhase++;
                    break;
                }
            case 4:
                {
                    //И выходит из-под земли с атакой
                    rigid.velocity = Vector2.up * diveOutSpeed * speedCoof;
                    if (targetDistance.y < divingPrecision)
                    {
                        transform.position = new Vector3(transform.position.x, targetPosition.y);
                        currentTarget = new ETarget(currentHorizontalAttackPosition==1 ? ghostPosition2.transform : ghostPosition1.transform);
                        divingPhase++;
                    }
                    break;
                }
            case 5:
                {
                    hitBox.SetHitBox(diveHorizontalAttackParametres);
                    hitBox.IgnoreInvul = true;
                    Animate(new AnimationEventArgs("horizontalAttack"));
                    divingPhase++;
                    break;
                }
            case 6:
                {
                    rigid.velocity = Vector2.right * (int)orientation * speedCoof * diveHorizontalSpeed;
                    if (targetDistance.x * (int)orientation < divingPrecision)
                    {
                        hitBox.ResetHitBox();
                        hitBox.IgnoreInvul = false;
                        Animate(new AnimationEventArgs("stop"));
                        attackTimes++;
                        if (attackTimes < 2 && bossPhase >= 2)
                        {
                            transform.position = currentTarget;
                            divingPhase = 4;
                            currentHorizontalAttackPosition = 3 - currentHorizontalAttackPosition;
                            Turn(currentHorizontalAttackPosition == 1 ? OrientationEnum.right : OrientationEnum.left);
                            currentTarget = new ETarget(currentHorizontalAttackPosition==1?ghostPosition2.transform: ghostPosition1.transform);
                        }
                        else
                            SetDiving(false, false);
                        rigid.velocity = Vector2.zero;
                    }
                    break;
                }
            default:
                {
                    break;
                }
        }
    }

    /// <summary>
    /// Процесс, в котором призрак ждёт под землёй
    /// </summary>
    protected IEnumerator DivingWaitProcess(float _time)
    {
        yield return new WaitForSeconds(_time);
        divingPhase++;
    }

    /// <summary>
    /// Процесс атаки при выходе из земли
    /// </summary>
    /// <returns></returns>
    protected IEnumerator DivingOutAttackProcess()
    {
        StopMoving();
        Animate(new AnimationEventArgs("attack", "SpecialAttack", Mathf.RoundToInt(100 * (attackParametres.wholeAttackTime))));

        employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.preAttackTime);
        hitBox.SetHitBox(attackParametres);
        hitBox.AttackDirection = Vector2.right * (int)orientation;
        yield return new WaitForSeconds(attackParametres.actTime+attackParametres.endAttackTime);
        employment = Mathf.Clamp(employment + 8, 0, maxEmployment);
    }

    IEnumerator DivingCooldownProcess()
    {
        canDive = false;
        yield return new WaitForSeconds(diveAttackCooldown);
        canDive = true;
    }

    /// <summary>
    /// Установить (или отменить) боссу режим особой атаки
    /// </summary>
    protected void SetDiving(bool yes, bool horizontal)
    {
        employment = maxEmployment;
        if (yes)
        {
            StopAttack();
            diving = true;

            col.enabled = false;
            divingPhase = 1;
            selfHitBox.ResetHitBox();
            currentTarget =  new ETarget(transform.position + Vector3.down*(horizontal?0.7f:1.0f)*diveDownOffset);
            if (!horizontal)
            {
                hitBox.IgnoreInvul = true;
                bossAction = DivingOutAction;
            }
            else
            {
                hitBox.IgnoreInvul = false;
                bossAction = DivingHorizontalAction;
            }
        }
        else
        {
            attackTimes = 0;
            diving = false;
            col.enabled = true;
            divingPhase = 0;
            currentTarget = mainTarget;
            bossAction = UsualBossAction;
            selfHitBox.SetHitBox(touchAttackParametres);
            hitBox.ResetHitBox();
            StartCoroutine(DivingCooldownProcess());
            divingHealth = Mathf.Ceil(health / 100f - 1f) * 100f;
        }
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        float a = (Random.Range(0f, 100f)) / 100f;
        crit =  (a<= critPossibility);
        StartCoroutine("AttackProcess");
    }

    /// <summary>
    /// Остановить атаку
    /// </summary>
    protected override void StopAttack()
    {
        employment = maxEmployment;
        Animate(new AnimationEventArgs("stopAttack"));
        StopCoroutine("AttackProcess");
        StopCoroutine("AreaAttackProcess");
        StopCoroutine("AttackRateProcess");
        StopCoroutine("DivingWaitProcess");
        StopCoroutine("DivingOutAttackProcess");
        SetDiving(false, false);
        if (selfHitBox!=null)
            selfHitBox.SetHitBox(touchAttackParametres);
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        StopMoving();
        string _attackName = attackTimes > 0 ? "FastAttack" : crit ? "CritAttack" : "Attack";
        float _preTime = attackTimes > 0 ? preFastAttackTime : crit ? preCritAttackTime : coalAttackParametres.preAttackTime;
        float _endTime = attackTimes > 0 ? endFastAttackTime : coalAttackParametres.endAttackTime;
        Animate(new AnimationEventArgs("attack", _attackName, Mathf.RoundToInt(100 * (_preTime + _endTime))));

        if (attackTimes<=0)
            employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        yield return new WaitForSeconds(_preTime);

        Vector2 targetDistance = mainTarget - (Vector2)transform.position;
        Vector2 direction = targetDistance.x * (int)orientation >= 0f? (targetDistance - new Vector2(coalThrowOffset.x * (int)orientation, coalThrowOffset.y)).normalized: (int)orientation*Vector2.right;
        GameObject newCoal = Instantiate(coal, transform.position + new Vector3(coalThrowOffset.x * (int)orientation, coalThrowOffset.y, 0f),Quaternion.identity) as GameObject;
        Rigidbody2D coalRigid = newCoal.GetComponent<Rigidbody2D>();
        coalRigid.velocity = direction * coalSpeed;
        coalRigid.gravityScale = 0f;
        HitBoxController coalHitBox = coalRigid.GetComponentInChildren<HitBoxController>();
        if (coalHitBox != null)
        {
            coalHitBox.SetEnemies(enemies);
            coalHitBox.SetHitBox(new HitParametres(coalAttackParametres));
            coalHitBox.allyHitBox = false;
            //coalHitBox.SetHitBox(new HitClass(crit? damage*1.5f:damage, -1f, coalHitSize, Vector2.zero, coalForce));
        }
        yield return new WaitForSeconds(_endTime);
        attackTimes++;
        if (bossPhase >= 1 && attackTimes<3)
            StartCoroutine("AttackProcess");
        else
        {
            attackTimes = 0;
            employment = Mathf.Clamp(employment + 5, 0, maxEmployment);

            StartCoroutine(CoalThrowCooldownProcess());
            yield return new WaitForSeconds(attackRate);
            employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
        }
    }

    /// <summary>
    /// Процесс кулдауна броска угля
    /// </summary>
    protected virtual IEnumerator CoalThrowCooldownProcess()
    {
        coalThrowPossible = false;
        yield return new WaitForSeconds(coalThrowCooldown);
        coalThrowPossible = true;
    }

    /// <summary>
    /// Совершить атаку по области
    /// </summary>
    protected virtual void AreaAttack()
    {
        StartCoroutine("AreaAttackProcess");
    }

    /// <summary>
    /// Процесс атаки по области
    /// </summary>
    protected IEnumerator AreaAttackProcess()
    {
        StopMoving();
        string _attackName = attackTimes == 0 ? "AreaAttack" : "AreaSecondAttack";
        float _preAttackTime = attackTimes == 0 ? areaAttackPreTime : preFastAttackTime;
        Animate(new AnimationEventArgs("attack",_attackName , Mathf.RoundToInt(100 * (_preAttackTime + areaAttackEndTime))));

        if (attackTimes <= 0)
            employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        yield return new WaitForSeconds(_preAttackTime);

        Vector2 targetDistance = mainTarget - (Vector2)transform.position;

        for (int i = 0; i < coalCount; i++)
        {
            float angle = (throwAngle + Random.Range(-throwAngleDeviation, throwAngleDeviation))/180f*Mathf.PI;
            Vector2 direction = new Vector2((int)orientation*Mathf.Cos(angle), Mathf.Sin(angle));
            GameObject newCoal = Instantiate(coal, transform.position + new Vector3(coalAreaAttackOffset.x * (int)orientation, coalAreaAttackOffset.y, 0f), Quaternion.identity) as GameObject;
            Rigidbody2D coalRigid = newCoal.GetComponent<Rigidbody2D>();
            coalRigid.velocity = direction * startThrowSpeed;
            coalRigid.gravityScale = coalGravityScale;
            BulletScript bullet = newCoal.GetComponent<BulletScript>();
            bullet.AttackParametres = coalAttackParametres;
            HitBoxController coalHitBox = coalRigid.GetComponentInChildren<HitBoxController>();
            if (coalHitBox != null)
            {
                coalHitBox.SetEnemies(enemies);
                coalHitBox.SetHitBox(new HitParametres(coalAttackParametres));
                coalHitBox.allyHitBox = false;
                //coalHitBox.SetHitBox(new HitClass(crit? damage*1.5f:damage, -1f, coalHitSize, Vector2.zero, coalForce));
            }
            bullet.GroundDetect = false;
        }
        yield return new WaitForSeconds(areaAttackEndTime);
        attackTimes++;
        if (bossPhase >= 2 && attackTimes < 2)
            StartCoroutine("AreaAttackProcess");
        else
        {
            attackTimes = 0;
            employment = Mathf.Clamp(employment + 5, 0, maxEmployment);

            StartCoroutine(CoalThrowCooldownProcess());
            yield return new WaitForSeconds(attackRate);
            employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
        }
    }

    /// <summary>
    /// Процесс, в котором персонаж не может совершать атаку по области
    /// </summary>
    /// <returns></returns>
    IEnumerator CoalAreaAttackCooldownProcess()
    {
        coalAreaAttackPossible = false;
        yield return new WaitForSeconds(coalAreaAttackCooldown);
        coalAreaAttackPossible = true;
    }

    /// <summary>
    /// Процесс, при котором персонаж не совершает атаки
    /// </summary>
    /// <returns></returns>
    IEnumerator AttackRateProcess()
    {
        employment = maxEmployment-3;
        yield return new WaitForSeconds(attackRate);
        employment = maxEmployment;
    }

    #endregion //bossActions

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(HitParametres hitData)
    {
        if (hitData.damageType != DamageType.Physical)
        {
            if (((DamageType)vulnerability & hitData.damageType) == hitData.damageType)
                hitData.damage *= 1.25f;
            else if (hitData.damageType == attackParametres.damageType)
                hitData.damage *= .9f;//Если урон совпадает с типом атаки персонажа, то он ослабевается (бить огонь огнём - не самая гениальная затея)
        }
        Health = Mathf.Clamp(Health - hitData.damage, 0f, maxHealth);
        if (health <= 0f)
            Death();
        GhostMinerBossVisual bAnim = (GhostMinerBossVisual)anim;
        if (bAnim != null)
            bAnim.Blink();
    }

    #region behaviourActions

    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        attackTimes = 0;
        coalThrowPossible = true;
        coalAreaAttackPossible = true;
        canDive = true;
        diving = false;
        StopCoroutine("AttackRateProcess");
    }

    /// <summary>
    /// Стать агрессивным
    /// </summary>
    protected override void BecomeAgressive()
    {
        transform.position = new Vector2(transform.position.x, beginPosition.y);
        base.BecomeAgressive();
        bossAction = UsualBossAction;
        
    }

    /// <summary>
    /// Перейти в состояние патруля
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        MainTarget = ETarget.zero;
    }

    /// <summary>
    /// Выдвинуться к указанной точке
    /// </summary>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        currentTarget = new ETarget(targetPosition);
        BecomePatrolling();
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        bossAction.Invoke();
    }

    /// <summary>
    /// Поведение патрулирования
    /// </summary>
    protected override void PatrolBehavior()
    {
        if (employment <= 5 || !currentTarget.Exists)
            return;
        Vector2 targetDistance = currentTarget - (Vector2)transform.position;
        if (targetDistance.sqrMagnitude > minDistance * minDistance)
            Move();
        else
        {
            if (Vector2.SqrMagnitude(transform.position - beginPosition) < minDistance)
                BecomeCalm();
            else
                GoHome();
        }

    }

    /// <summary>
    /// Сменить главную цель
    /// </summary>
    protected override void ChangeMainTarget()
    {
    }

    #endregion //behaviourActions

    #region effects

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeStunned(float _time)
    { }

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeBurning(float _time)
    { }

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeCold(float _time)
    { }

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeWet(float _time)
    { }

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomePoisoned(float _time)
    { }

    /// <summary>
    /// На призрака не действуют особые эффекты урона
    /// </summary>
    protected override void BecomeFrozen(float _time)
    { }

    #endregion //effects

}

/// <summary>
/// Редактор сущности шахтёров
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(GhostMinerBossController))]
public class GhostMinerBossControllerEditor : AIControllerEditor
{

    GhostMinerBossController minerBoss;
    SerializedObject serMinerBoss;

    SerializedProperty bossName,
    maxHP,
    health,
    balance,
    speed,
    acceleration;

    bool coalThrowParametresShow = false;
    SerializedProperty coal,
    coalThrowOffset,
    coalSpeed,
    critPossibility,
    preCritAttackTime,
    preFastAttackTime,
    coalAttackParametres,
    coalThrowCooldown;

    bool coalAreaAttackParametresShow = false;
    SerializedProperty coalAreaAttackOffset,
    coalCount,
    throwAngle,
    areaAttackPreTime, 
    areaAttackEndTime,
    throwAngleDeviation,
    startThrowSpeed,
    coalGravityScale,
    coalAreaAttackCooldown;

    bool diveAttackParametresShow;
    SerializedProperty diveOutSpeed,
    diveHorizontalSpeed,
    diveOutAttackParametres,
    diveHorizontalAttackParametres,
    divingWaitTime,
    diveAttackCooldown;

    bool touchAttackParametresShow;
    SerializedProperty touchAttackParametres,
    attackRate,
    loyalty,
    drop,
    vulnerability;

    public override void OnEnable()
    {
        minerBoss=(GhostMinerBossController)target;
        serMinerBoss=new SerializedObject(minerBoss);

        bossName=serMinerBoss.FindProperty("bossName");
        maxHP = serMinerBoss.FindProperty("maxHealth");
        health = serMinerBoss.FindProperty("health");
        balance = serMinerBoss.FindProperty("balance");
        speed = serMinerBoss.FindProperty("speed");
        acceleration = serMinerBoss.FindProperty("acceleration");

        coal = serMinerBoss.FindProperty("coal");
        coalThrowOffset = serMinerBoss.FindProperty("coalThrowOffset");
        coalSpeed = serMinerBoss.FindProperty("coalSpeed");
        critPossibility = serMinerBoss.FindProperty("critPossibility");
        preCritAttackTime = serMinerBoss.FindProperty("preCritAttackTime");
        preFastAttackTime = serMinerBoss.FindProperty("preFastAttackTime");
        coalAttackParametres = serMinerBoss.FindProperty("coalAttackParametres");
        coalThrowCooldown = serMinerBoss.FindProperty("coalThrowCooldown");

        coalAreaAttackOffset = serMinerBoss.FindProperty("coalAreaAttackOffset");
        coalCount = serMinerBoss.FindProperty("coalCount");
        throwAngle = serMinerBoss.FindProperty("throwAngle");
        throwAngleDeviation = serMinerBoss.FindProperty("throwAngleDeviation");
        startThrowSpeed = serMinerBoss.FindProperty("startThrowSpeed");
        areaAttackPreTime = serMinerBoss.FindProperty("areaAttackPreTime");
        areaAttackEndTime = serMinerBoss.FindProperty("areaAttackEndTime");
        coalGravityScale = serMinerBoss.FindProperty("coalGravityScale");
        coalAreaAttackCooldown = serMinerBoss.FindProperty("coalAreaAttackCooldown");

        diveOutSpeed = serMinerBoss.FindProperty("diveOutSpeed");
        diveHorizontalSpeed = serMinerBoss.FindProperty("diveHorizontalSpeed");
        diveOutAttackParametres = serMinerBoss.FindProperty("attackParametres");
        diveHorizontalAttackParametres = serMinerBoss.FindProperty("diveHorizontalAttackParametres");
        divingWaitTime = serMinerBoss.FindProperty("divingWaitTime");
        diveAttackCooldown = serMinerBoss.FindProperty("diveAttackCooldown");

        touchAttackParametres = serMinerBoss.FindProperty("touchAttackParametres");

        attackRate = serMinerBoss.FindProperty("attackRate");
        loyalty = serMinerBoss.FindProperty("loyalty");
        drop = serMinerBoss.FindProperty("drop");
        vulnerability = serMinerBoss.FindProperty("vulnerability");
    }

    public override void OnInspectorGUI()
    {

        EditorGUILayout.LabelField("General Parametres");

        EditorGUILayout.PropertyField(bossName,true);
        maxHP.floatValue = EditorGUILayout.FloatField("Max Health", maxHP.floatValue);
        EditorGUILayout.PropertyField(health);
        balance.intValue = EditorGUILayout.IntField("Balance", balance.intValue);
        speed.floatValue = EditorGUILayout.FloatField("Speed", speed.floatValue);
        acceleration.floatValue = EditorGUILayout.FloatField("Acceleration", acceleration.floatValue);

        EditorGUILayout.Space();

        coalThrowParametresShow = EditorGUILayout.Foldout(coalThrowParametresShow, "Coal Throw Parametres");
        if (coalThrowParametresShow)
        {
            coal.objectReferenceValue = EditorGUILayout.ObjectField("Coal", coal.objectReferenceValue, typeof(GameObject));
            EditorGUILayout.PropertyField(coalThrowOffset);
            EditorGUILayout.PropertyField(coalSpeed);
            EditorGUILayout.PropertyField(critPossibility);
            EditorGUILayout.PropertyField(preCritAttackTime);
            EditorGUILayout.PropertyField(preFastAttackTime);
            EditorGUILayout.PropertyField(coalAttackParametres,true);
            EditorGUILayout.PropertyField(coalThrowCooldown);
        }

        EditorGUILayout.Space();
        coalAreaAttackParametresShow = EditorGUILayout.Foldout(coalAreaAttackParametresShow, "Coal Area Attack");
        if (coalAreaAttackParametresShow)
        {
            EditorGUILayout.PropertyField(coalAreaAttackOffset);
            EditorGUILayout.PropertyField(coalCount);
            EditorGUILayout.PropertyField(throwAngle);
            EditorGUILayout.PropertyField(throwAngleDeviation);
            EditorGUILayout.PropertyField(startThrowSpeed);
            EditorGUILayout.PropertyField(areaAttackPreTime);
            EditorGUILayout.PropertyField(areaAttackEndTime);
            EditorGUILayout.PropertyField(coalGravityScale);
            EditorGUILayout.PropertyField(coalAreaAttackCooldown);
        }

        EditorGUILayout.Space();
        diveAttackParametresShow = EditorGUILayout.Foldout(diveAttackParametresShow, "Dive Attack");
        if (diveAttackParametresShow)
        {
            EditorGUILayout.PropertyField(diveOutSpeed);
            EditorGUILayout.PropertyField(diveHorizontalSpeed);
            EditorGUILayout.PropertyField(diveOutAttackParametres, true);
            EditorGUILayout.PropertyField(diveHorizontalAttackParametres,true);
            EditorGUILayout.PropertyField(divingWaitTime);
            EditorGUILayout.PropertyField(diveAttackCooldown);
        }

        EditorGUILayout.Space();
        touchAttackParametresShow = EditorGUILayout.Foldout(touchAttackParametresShow, "Touch Attack");
        if (touchAttackParametresShow)
            EditorGUILayout.PropertyField(touchAttackParametres,true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Other");
        EditorGUILayout.PropertyField(attackRate);
        EditorGUILayout.PropertyField(loyalty);
        EditorGUILayout.PropertyField(drop, true);
        minerBoss.Vulnerability = (byte)(DamageType)EditorGUILayout.EnumMaskPopup(new GUIContent("vulnerability"), (DamageType)minerBoss.Vulnerability);

        serMinerBoss.ApplyModifiedProperties();
    }

}
#endif //UNITY_EDITOR
