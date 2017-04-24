using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Контроллер, управляющий боссом - таинственным незнакомцем
/// </summary>
public class StrangerController : BossController
{

    #region enums

    protected enum TeleportAttackType { usual = 0, trick = 1, trickInit=2, doubleTeleport = 3, teleportChaos = 4 }

    #endregion //enums

    #region consts

    protected const float groundRadius = .02f;

    #endregion //consts

    #region fields

    [SerializeField] protected GameObject magicWave;//Магическая волна, которую может пускать незнакомец
    [SerializeField] protected List<GameObject> barriers;//Барьеры, что ставит незнакомец при начале битвы, чтобы цель не смогла убежать

    //Список кулдаунов
    protected override List<Timer> Timers
    {
        get
        {
            return new List<Timer> { new Timer("attackCooldown",attackRate),
                                     new Timer("teleportAttackCooldown", teleportAttackCooldown),
                                     new Timer("areaAttackCooldown", areaAttackCooldown),
                                     new Timer("trickCooldown", trickCooldown)};

        }
    }

    #endregion //fields

    #region parametres

    [SerializeField] protected float phase2Health=200f;//С какого значения здоровья происходит переход на вторую фазу боя
    [SerializeField]protected float attackRate;//Минимальное время между совершениями двух различных атак
    protected override float waitingNearDistance { get { return .4f; } }//Если враг находится ближе этого расстояния, то незнакомец пытается телепортироваться
    protected override float sightRadius{get{return 2.8f;}}
    protected override float attackDistance { get { return 3.5f; } }//На каком расстоянии должен стоять ИИ, чтобы решить атаковать

    #region teleportAttack

    [SerializeField] protected float preTeleportTime = .66f, fastPreTeleportTime = .33f, endTeleportTime = .68f, fastEndTeleportTime = .4f, teleportBlinkPeriod = .35f, superFastPreTeleportTime=.1f;//Времена различных телепортов
    [SerializeField] protected HitParametres teleportAttackParametres;//Параметры атаки после телепорта
    [SerializeField]protected float fastPreTeleportAttackTime=.15f;//Время быстрого начала телепортационной атаки 
    [SerializeField] protected float teleportNearDistance = .15f, teleportFarDistance = .5f;//Расстояния, на которые переносится незнакомец относительно персонажа
    [SerializeField] protected float teleportTrickTime = 2f, teleportChaosTime = 3f;//Время, которое проводит незнакомец, пытаясь обмануть героя, и когда происходит телепортационный хаос
    [SerializeField]protected float teleportChaosDistance = 2;//Максимальное расстояние на котором находится незнакомец от цели при телепортационном хаосе
    [SerializeField] protected int phase1MaxTeleportAttackTimes = 1, phase2MaxTeleportAttackTimes = 3;
    protected int teleportAttackTimes;//Количество атак после телепорта
    [SerializeField] protected float teleportAttackCooldown = 4f;//Кулдаун телепортационной атаки
    [SerializeField] protected float trickCooldown = 10f;//Кулдаун уловки
    protected bool tricky = false;//Пытается ли незнакомец провести телепортационную уловку
    protected bool inTeleport = false;//телепортируется ли персонаж в данный момент
    [SerializeField]protected float afterAttackTime = 1.5f;//Сколько времени незнакомец не двигается, перед тем, как произвести какое-нибудь действие
    protected IEnumerator teleportProcess=null;

    #endregion //teleportAttack

    #region areaAttack

    [SerializeField]protected Vector2 shootOffset1 = new Vector2(0.06085682f, 0.02423363f), shootOffset2 = new Vector2(-0.07814312f, 0.02223364f);//Относительные смещения, откуда исходят магические волны
    [SerializeField]protected float magicWaveSpeed = 2f;//Скорость магической волны
    [SerializeField]protected HitParametres areaAttackParametres;//Параметры атаки по области
    [SerializeField]protected HitParametres magicWaveAttackParametres;//Параметры волны, создаваемой при атаки по области
    [SerializeField]protected float areaAttackCooldown = 10f;//Кулдаун на атаку по области

    #endregion //areaAttack

    #endregion //parametres

    protected override void Initialize()
    {
        base.Initialize();

        bossPhase = 0;
        tricky = false;
        inTeleport = false;

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            areaTrigger.InitializeAreaTrigger();
        }

        //BecomeCalm();
    }

    /// <summary>
    /// Анализировать окружающую обстановку
    /// </summary>
    protected override void Analyse()
    {
        if (behavior != BehaviorEnum.agressive && loyalty == LoyaltyEnum.enemy)
        {
            if (Vector3.Distance(SpecialFunctions.Player.transform.position, transform.position) <= sightRadius)
            {
                MainTarget = new ETarget(SpecialFunctions.player.transform);
                BecomeAgressive();
            }
        }
    }

    #region movement

    /// <summary>
    /// Совершить телепорт относительно цели
    /// </summary>
    /// <param name="_near">Телепортироваться близко к цели или далеко</param>
    /// <param name="_behindBack">Учесть ориентацию цели и телепортироваться за спину</param>
    /// <param name="_forward">Расстояния, преодолеваемое телепортом, меньше расстояния до цели</param>
    protected virtual void Teleport(bool _near = true, bool _behindBack = false, bool _forward = false, bool _inFront=false)
    {
        if (!currentTarget.exists)
            return;
        Vector2 nextPoint = transform.position;//Определим, в какую точку должен переместится персонаж
        Vector2 targetPosition = currentTarget;
        targetPosition = new Vector2(targetPosition.x, beginPosition.y);
        float direction = Mathf.Sign(targetPosition.x - transform.position.x);
        float teleportDistance = _near ? teleportNearDistance : teleportFarDistance;
        if (_behindBack && currentTarget.transform != null)
            nextPoint = targetPosition + Vector2.right * teleportDistance * (currentTarget.transform.lossyScale.x > 0f ? -1f : 1f);
        else if (_inFront && currentTarget.transform != null)
            nextPoint = targetPosition + Vector2.right * teleportDistance * (currentTarget.transform.lossyScale.x > 0f ? 1f : -1f);
        else
            nextPoint = targetPosition + Vector2.right * teleportDistance * (_forward ? -1f : 1f) * direction;
        nextPoint = CheckTeleportPoint(nextPoint);

        transform.position = nextPoint;
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.X - nextPoint.x)));
    }

    /// <summary>
    /// Проверить, возможен ли переход в указанную точку, и если невозможен, то найти ближайшую точку, куда можно переместиться
    /// </summary>
    protected Vector2 CheckTeleportPoint(Vector2 _nextPoint)
    {
        Vector2 direction = (beginPosition - _nextPoint);
        float distance = direction.sqrMagnitude;
        direction = direction.normalized;
        while (Physics2D.OverlapCircle(_nextPoint, groundRadius, LayerMask.GetMask(gLName)) && distance > 0f)
        {
            _nextPoint += direction * groundRadius;
            distance -= groundRadius;
        }
        return _nextPoint;
    }

    /// <summary>
    /// Произвести обчыный телепорт
    /// </summary>
    /// <param name="_fast">Произойдёт быстрый телепорт (true) или медленный (false)</param>
    /// <param name="_near">Телепортироваться близко к цели (true) или далеко (false)</param>
    /// <param name="_behindBack">Учесть ориентацию цели и телепортироваться за спину(true)</param>
    /// <param name="_forward">Учесть относительное положение цели и телепортироваться на позицию за этим положением (false) или перед (true)</param>
    protected IEnumerator TeleportProcess(bool _fast = true, bool _near = true, bool _behindBack = false, bool _forward = false)
    {
        float _preTime = _fast ? fastPreTeleportTime:preTeleportTime, _endTime=_fast? fastEndTeleportTime: endTeleportTime;
        rigid.isKinematic = true;
        employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        inTeleport = true;
        Animate(new AnimationEventArgs("teleportBegin", _fast ? "fast" : "", Mathf.RoundToInt(100f * _preTime)));
        Vector2 nextPoint = transform.position;//Определим, в какую точку должен переместится персонаж
        Vector2 targetPosition = currentTarget;
        targetPosition = new Vector2(targetPosition.x, beginPosition.y);
        float direction = Mathf.Sign(targetPosition.x - transform.position.x);
        float teleportDistance = _near ? teleportNearDistance : teleportFarDistance;
        if (_behindBack && currentTarget.transform != null)
            nextPoint = targetPosition + Vector2.right * teleportDistance * (currentTarget.transform.lossyScale.x > 0f ? -1f : 1f);
        else
            nextPoint = targetPosition + Vector2.right * teleportDistance * (_forward ? -1f : 1f) * direction;
        nextPoint = CheckTeleportPoint(nextPoint);
        yield return new WaitForSeconds(_preTime);
        transform.position = nextPoint;
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.X - nextPoint.x)));
        Animate(new AnimationEventArgs("teleportEnd", _fast ? "fast" : "", Mathf.RoundToInt(100f * _endTime)));
        yield return new WaitForSeconds(_endTime);
        rigid.isKinematic = false;
        inTeleport = false;
        employment = Mathf.Clamp(employment + 8, 0, maxEmployment);
    }

    /// <summary>
    /// Телепортироваться в начальную позицию
    /// </summary>
    protected IEnumerator TeleportToPositionProcess(Vector2 _position)
    {
        rigid.isKinematic = true;
        employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        inTeleport = true;
        Animate(new AnimationEventArgs("teleportBegin", "", Mathf.RoundToInt(100f * preTeleportTime)));
        yield return new WaitForSeconds(preTeleportTime);
        transform.position = CheckTeleportPoint(_position);
        Animate(new AnimationEventArgs("teleportEnd", "", Mathf.RoundToInt(100f * endTeleportTime)));
        yield return new WaitForSeconds(endTeleportTime);
        rigid.isKinematic = false;
        inTeleport = false;
        employment = Mathf.Clamp(employment + 8, 0, maxEmployment);
    }

    /// <summary>
    /// Прекратить перемещение
    /// </summary>
    protected override void StopMoving()
    {
        rigid.velocity = new Vector2(0f, rigid.velocity.y);
    }

    #endregion //movement

    #region attacks

    /// <summary>
    /// Выбрать одну из возможных атак и совершать её
    /// </summary>
    protected void ChooseAttack()
    {

        if (!mainTarget.exists)
            return;

        List<string> possibleActions = new List<string>();
        Vector2 targetDistance = mainTarget - (Vector2)transform.position;
        float sDistance = targetDistance.sqrMagnitude;

        //Проверим, какие атаки вообще можно совершить в данный момент
        if (!IsTimerActive("areaAttackCooldown"))
            possibleActions.Add("areaAttack");
        if (!IsTimerActive("teleportAttackCooldown"))
        {
            if (bossPhase >= 1)
                possibleActions.Add("teleportChaos");
            if (!IsTimerActive("trickCooldown"))
                possibleActions.Add("trick");
            possibleActions.Add("usualTeleportAttack");
            possibleActions.Add("doubleTeleportAttack");
        }

        if (possibleActions.Count == 0)
            return;

        if (targetDistance.x * (int)orientation < 0f)
            Turn();
        string nextAction = possibleActions[Random.Range(0, possibleActions.Count)];

        switch (nextAction)
        {
            case "usualTeleportAttack":  
                StartCoroutine("TeleportAttackProcess", TeleportAttackType.usual);
                break; 
            case "doubleTeleportAttack":
                StartCoroutine("TeleportAttackProcess", TeleportAttackType.doubleTeleport);
                break;
            case "trick":
                StartCoroutine("TeleportAttackProcess", TeleportAttackType.trickInit);
                break;
            case "teleportChaos":
                StartCoroutine("TeleportAttackProcess", TeleportAttackType.teleportChaos);
                break;
            case "areaAttack":
                StartCoroutine("AreaAttackProcess");
                break;
            default:
                break;
        }

    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.X - transform.position.x)));
        Animate(new AnimationEventArgs("attack", "Attack", Mathf.RoundToInt(100f * attackParametres.wholeAttackTime)));
        employment = Mathf.Clamp(employment - 6, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.preAttackTime);
        hitBox.SetHitBox(new HitParametres(attackParametres));
        hitBox.AttackDirection = Vector2.right * (int)orientation;
        yield return new WaitForSeconds(attackParametres.actTime + attackParametres.endAttackTime);
        Animate(new AnimationEventArgs("stop"));
        Animate(new AnimationEventArgs("attack", "AfterAttackRest", Mathf.RoundToInt(100f * afterAttackTime)));
        yield return new WaitForSeconds(afterAttackTime);
        StartTimer("teleportAttackCooldown");
        StartTimer("attackCooldown");
        attackName = "";
        employment = Mathf.Clamp(employment + 6, 0, maxEmployment);
    }

    /// <summary>
    /// Процесс совершения атаки с телепортом
    /// </summary>
    protected IEnumerator TeleportAttackProcess(TeleportAttackType _attackType)
    {
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.X - transform.position.x)));
        Animate(new AnimationEventArgs("teleportBegin", _attackType==TeleportAttackType.trick? "superFast":"fast", Mathf.RoundToInt(100f * fastPreTeleportTime)));
        attackName = "teleportAttack";
        string animationName = _attackType == TeleportAttackType.trick ? "FastTeleportAttackEnd":"TeleportAttackEnd";
        float teleportBeginTime = _attackType == TeleportAttackType.trick ? superFastPreTeleportTime : fastPreTeleportTime;
        float teleportAttackPreTime = _attackType == TeleportAttackType.trick ? fastPreTeleportAttackTime : teleportAttackParametres.preAttackTime;
        employment = Mathf.Clamp(employment - 6, 0, maxEmployment);
        inTeleport = true;
        rigid.isKinematic = true;

        yield return new WaitForSeconds(teleportBeginTime);
        Teleport(true, true, false);
        Animate(new AnimationEventArgs("stop"));

        switch (_attackType)
        {
            case TeleportAttackType.usual:
                teleportAttackTimes = Random.Range(1, (bossPhase >= 1 ? phase2MaxTeleportAttackTimes : phase1MaxTeleportAttackTimes) + 1);
                for (int i = 0; i < teleportAttackTimes - 1; i++)
                {
                    inTeleport = false;
                    rigid.isKinematic = false;
                    Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.X - transform.position.x)));
                    Animate(new AnimationEventArgs("attack", "TeleportAttackEnd", Mathf.RoundToInt(100f * teleportAttackParametres.wholeAttackTime)));
                    yield return new WaitForSeconds(teleportAttackParametres.preAttackTime);
                    hitBox.SetHitBox(new HitParametres(teleportAttackParametres));
                    hitBox.AttackDirection = Vector2.right * (int)orientation;
                    yield return new WaitForSeconds(teleportAttackParametres.actTime + teleportAttackParametres.endAttackTime);
                    inTeleport = true;
                    rigid.isKinematic = true;
                    Animate(new AnimationEventArgs("stop"));
                    Animate(new AnimationEventArgs("teleportBegin", "fast", Mathf.RoundToInt(100f * fastPreTeleportTime)));
                    yield return new WaitForSeconds(fastPreTeleportTime);
                    Teleport(true, true, false);
                    Animate(new AnimationEventArgs("stop"));
                }
                break;
            case TeleportAttackType.doubleTeleport:
                Animate(new AnimationEventArgs("teleportEnd", "fast", Mathf.RoundToInt(100f * fastEndTeleportTime)));
                yield return new WaitForSeconds(fastEndTeleportTime);
                //Animate(new AnimationEventArgs("stop"));
                Animate(new AnimationEventArgs("teleportBegin", "fast", Mathf.RoundToInt(100f * fastPreTeleportTime)));
                yield return new WaitForSeconds(fastPreTeleportTime);
                Teleport(true, false, false,true);
                Animate(new AnimationEventArgs("stop")); 
                break;
            case TeleportAttackType.teleportChaos:
                Vector3 _startPosition = transform.position;
                float totalChaosTime = Random.Range(teleportChaosTime / 2f, teleportChaosTime);
                float currentChaosTime = 0f;
                float angle = Random.Range(-.5f, .5f) * Mathf.PI;
                float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
                Vector3 currentDirection = new Vector2(cos, sin);
                while (currentChaosTime < totalChaosTime)
                {
                    Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.X - transform.position.x)));
                    Animate(new AnimationEventArgs("teleportBlink", "", Mathf.RoundToInt(100f*teleportBlinkPeriod)));
                    yield return new WaitForSeconds(teleportBlinkPeriod);
                    transform.position += currentDirection * Random.Range(.5f, 1f) * teleportChaosDistance;
                    currentChaosTime += teleportBlinkPeriod;
                    if (Vector2.SqrMagnitude(transform.position - _startPosition) > teleportChaosDistance * teleportChaosDistance)
                        currentDirection = -(transform.position - _startPosition).normalized;
                    else
                    {
                        angle = Random.Range(.5f, 1.5f) * Mathf.PI;
                        sin = Mathf.Sin(angle);
                        cos = Mathf.Cos(angle);
                        currentDirection = new Vector2(currentDirection.x * cos - currentDirection.y * sin, currentDirection.x * sin + currentDirection.y * cos);
                    }
                }
                Teleport(true, Random.Range(0f, 1f) > .5f, false,true);
                break;
            default:
                break;
        }

        Animate(new AnimationEventArgs("stop"));
        inTeleport = false;
        rigid.isKinematic = false;
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.X - transform.position.x)));
        Animate(new AnimationEventArgs("attack", animationName, Mathf.RoundToInt(100f * teleportAttackPreTime+teleportAttackParametres.withoutPrepareAttackTime)));
        yield return new WaitForSeconds(teleportAttackPreTime);
        hitBox.SetHitBox(new HitParametres(teleportAttackParametres));
        hitBox.AttackDirection = Vector2.right * (int)orientation;
        yield return new WaitForSeconds(teleportAttackParametres.withoutPrepareAttackTime);
        if (bossPhase >= 1 && _attackType != TeleportAttackType.trickInit)
        {
            employment = Mathf.Clamp(employment + 6, 0, maxEmployment);
            Attack();//В конце произвести ещё одну атаку
        }
        else if (_attackType != TeleportAttackType.trickInit)
        {
            Animate(new AnimationEventArgs("stop"));
            Animate(new AnimationEventArgs("attack", "AfterAttackRest", Mathf.RoundToInt(100f * afterAttackTime)));
            yield return new WaitForSeconds(afterAttackTime);
            employment = Mathf.Clamp(employment + 6, 0, maxEmployment);
            StartTimer("teleportAttackCooldown");
            StartTimer("attackCooldown");
            attackName = "";
        }
        else
        {
            Animate(new AnimationEventArgs("stop"));
            employment = Mathf.Clamp(employment + 6, 0, maxEmployment);
            StartCoroutine("TrickProcess");
        }
    }

    /// <summary>
    /// Провести обманный манёвр, который заключается в том, что незнакомец будет ожидать атаку противника, чтобы в момент атаки совершить быстрый телепорт за спину противника и совершить контрудар
    /// </summary>
    protected IEnumerator TrickProcess()
    {
        attackName = "trick";
        tricky=true;
        employment = Mathf.Clamp(employment - 6, 0, maxEmployment);
        Animate(new AnimationEventArgs("attack", "Trick", Mathf.RoundToInt(100f * teleportTrickTime)));
        yield return new WaitForSeconds(teleportTrickTime);
        employment = Mathf.Clamp(employment + 6, 0, maxEmployment);
        tricky = false;
        attackName = "";
    }

    /// <summary>
    /// Процесс совершения атаки по области
    /// </summary>
    protected IEnumerator AreaAttackProcess()
    {
        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.X - transform.position.x)));
        Animate(new AnimationEventArgs("attack", "AreaAttack", Mathf.RoundToInt(100f * areaAttackParametres.wholeAttackTime)));
        employment = Mathf.Clamp(employment - 6, 0, maxEmployment);
        attackName = "areaAttack";
        yield return new WaitForSeconds(areaAttackParametres.preAttackTime);
        hitBox.SetHitBox(new HitParametres(areaAttackParametres));
        Vector2 attackDirection = Vector2.right * (int)orientation;
        hitBox.AttackDirection = attackDirection;

        GameObject newMissile = Instantiate(magicWave, transform.position + new Vector3(shootOffset1.x * (int)orientation, shootOffset1.y, 0f), Quaternion.identity) as GameObject;
        newMissile.transform.localScale = new Vector3((int)orientation, 1f, 1f);
        Rigidbody2D missileRigid = newMissile.GetComponent<Rigidbody2D>();
        missileRigid.velocity =   magicWaveSpeed* attackDirection;
        missileRigid.gravityScale = 0f;
        HitBoxController missileHitBox = missileRigid.GetComponentInChildren<HitBoxController>();
        if (missileHitBox != null)
        {
            missileHitBox.SetEnemies(enemies);
            missileHitBox.SetHitBox(new HitParametres(magicWaveAttackParametres));
            missileHitBox.IgnoreInvul = true;
            missileHitBox.allyHitBox = false;
        }

        if (bossPhase >= 1)
        {
            //При второй фазе босса создастся ещё одна волна
            newMissile = Instantiate(magicWave, transform.position + new Vector3(shootOffset2.x * (int)orientation, shootOffset2.y, 0f), Quaternion.identity) as GameObject;
            newMissile.transform.localScale = new Vector3(-(int)orientation, 1f, 1f);
            missileRigid = newMissile.GetComponent<Rigidbody2D>();
            missileRigid.velocity = magicWaveSpeed * -attackDirection;
            missileRigid.gravityScale = 0f;
            missileHitBox = missileRigid.GetComponentInChildren<HitBoxController>();
            if (missileHitBox != null)
            {
                missileHitBox.SetEnemies(enemies);
                missileHitBox.SetHitBox(new HitParametres(magicWaveAttackParametres));
                missileHitBox.IgnoreInvul = true;
                missileHitBox.allyHitBox = false;
            }
        }

        yield return new WaitForSeconds(areaAttackParametres.actTime + areaAttackParametres.endAttackTime);
        Animate(new AnimationEventArgs("stop"));
        Animate(new AnimationEventArgs("attack", "AfterAttackRest", Mathf.RoundToInt(100f * afterAttackTime)));
        yield return new WaitForSeconds(afterAttackTime);
        StartTimer("areaAttackCooldown");
        StartTimer("attackCooldown");
        attackName = "";
        employment = Mathf.Clamp(employment + 6, 0, maxEmployment);
    }

    /// <summary>
    /// Прекратить процесс атаки
    /// </summary>
    protected override void StopAttack()
    {
        if (!optimized)
            rigid.isKinematic = false;
        base.StopAttack();
        tricky = false;
        inTeleport = false;
        employment = maxEmployment;
        if (teleportProcess!=null)
            StopCoroutine(teleportProcess);
        StopCoroutine("TeleportAttackProcess");
        StopCoroutine("AreaAttackProcess");
        StopCoroutine("TrickProcess");
        if (attackName != "")
        {
            StartTimer("attackCooldown");
            StartTimer(attackName + "Cooldown");
            attackName = "";
        }
    }

    /// <summary>
    /// Функция, вызываемая при получении урона, оповещающая о субъекте нападения
    /// </summary>
    /// <param name="attackerInfo">Кто атаковал персонажа</param>
    public override void TakeAttackerInformation(AttackerClass attackerInfo)
    {
        if (attackerInfo != null)
        {
            if (mainTarget.transform != attackerInfo.attacker.transform)
            {
                MainTarget = new ETarget(attackerInfo.attacker.transform);
                if (behavior != BehaviorEnum.agressive)
                    BecomeAgressive();
            }
            else if (tricky)
            {
                StopAttack();
                StartTimer("trickCooldown");
                StartCoroutine("TeleportAttackProcess", TeleportAttackType.trick);
            }
        }
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    /// <param name="hitData">Параметры урона</param>
    public override void TakeDamage(HitParametres hitData)
    {
        if (inTeleport)
            return;

        if (hitData.damageType != DamageType.Physical)
        {
            if (((DamageType)vulnerability & hitData.damageType) == hitData.damageType)
                hitData.damage *= 1.25f;
            else if (hitData.damageType == attackParametres.damageType)
                hitData.damage *= .9f;//Если урон совпадает с типом атаки персонажа, то он ослабевается (бить огонь огнём - не самая гениальная затея)
        }
        Health = Mathf.Clamp(Health - hitData.damage, 0f, maxHealth);
        if (health <= 0f)
        {
            Death();
            return;
        }
        else if (health <= phase2Health)
            bossPhase = 1;

        if ((hitData.damageType != DamageType.Physical) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
            TakeDamageEffect(hitData.damageType);
        bool stunned = GetBuff("StunnedProcess") != null;
        bool frozen = GetBuff("FrozenProcess") != null;
        if (hitData.attackPower > balance || frozen || stunned)
        {
            StopMoving();
            balance = usualBalance;
            if (!frozen && !stunned)
            {
                StopCoroutine("Microstun");
                StartCoroutine("Microstun");
            }
            StopAttack();
            employment = maxEmployment;
            if (behavior == BehaviorEnum.patrol)
                BecomeAgressive();
        }
        Animate(new AnimationEventArgs("hitted", "", hitData.attackPower > balance ? 0 : 1));
    }

    /// <summary>
    /// Функция получения урона, который может игнорировать состояние инвула
    /// </summary>
    /// <param name="hitData">Параметры урона</param>
    /// <param name="ignoreInvul">Игнорирует ли урон инвул (true)</param>
    public override void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
        if (inTeleport)
            return;

        if (hitData.damageType != DamageType.Physical)
        {
            if (((DamageType)vulnerability & hitData.damageType) == hitData.damageType)
                hitData.damage *= 1.25f;
            else if (hitData.damageType == attackParametres.damageType)
                hitData.damage *= .9f;//Если урон совпадает с типом атаки персонажа, то он ослабевается (бить огонь огнём - не самая гениальная затея)
        }
        Health = Mathf.Clamp(Health - hitData.damage, 0f, maxHealth);
        if (health <= 0f)
        {
            Death();
            return;
        }
        else if (health <= phase2Health)
            bossPhase = 1;

        if ((hitData.damageType != DamageType.Physical) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
            TakeDamageEffect(hitData.damageType);
        bool stunned = GetBuff("StunnedProcess") != null;
        bool frozen = GetBuff("FrozenProcess") != null;
        if (hitData.attackPower > balance || frozen || stunned)
        {
            StopMoving();
            balance = usualBalance;
            if (!frozen && !stunned)
            {
                StopCoroutine("Microstun");
                StartCoroutine("Microstun");
            }
            StopAttack();
            employment = maxEmployment;
            if (behavior == BehaviorEnum.patrol)
                BecomeAgressive();
        }
        Animate(new AnimationEventArgs("hitted", "", hitData.attackPower > balance ? 0 : 1));
    }

    /// <summary>
    /// Функция смерти
    /// </summary>
    protected override void Death()
    {
        DisconnectFromUI();
        foreach (GameObject drop1 in drop)
        {
            GameObject _drop = Instantiate(drop1, transform.position, transform.rotation) as GameObject;
        }
        SpecialFunctions.StartStoryEvent(this, CharacterDeathEvent, new StoryEventArgs());
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            BuffClass buff = buffs[i];
            StopCustomBuff(new BuffData(buff));
        }
        immobile = true;
        StopAttack();
        SpecialFunctions.statistics.ConsiderStatistics(this);
        Animate(new AnimationEventArgs("death"));
        if (targetCharacter != null)
            targetCharacter.CharacterDeathEvent -= HandleTargetDeathEvent;
        rigid.gravityScale = 1f;
        Destroy(this);
        Destroy(areaTrigger.gameObject);
        foreach (GameObject barrier in barriers)
            barrier.SetActive(false);
    }

    #region damageEffects

    /// <summary>
    /// Оглушить незнакомца невозможно
    /// </summary>
    protected override void BecomeStunned(float _time)
    {
    }

    /// <summary>
    /// Поджечь
    /// </summary>
    protected override void BecomeBurning(float _time)
    {
        base.BecomeBurning(_time);
        if (GetBuff("BurningProcess") != null)
            return;
        if (GetBuff("WetProcess") != null)
            return;//Нельзя мокрого персонажа
        StopCold();//Согреться
        if (GetBuff("FrozenProcess") != null)
        {
            StopFrozen();//Если персонажа подожгли, когда он был заморожен, то он отмараживается и не получает никакого урона от огня, так как считаем, что всё тепло ушло на разморозку
            return;
        }
        StartCoroutine("BurningProcess", _time == 0 ? burningTime : _time);
    }

    #endregion //damageEffects

    #endregion attacks

    #region behaviorActions

    /// <summary>
    /// Обновить информацию, выжную для моделей поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        ResetTimers();
    }

    /// <summary>
    /// Стать агрессивным
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        currentTarget = mainTarget;
        foreach (GameObject barrier in barriers)
            barrier.SetActive(true);
    }

    /// <summary>
    /// Стать спокойным
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        foreach (GameObject barrier in barriers)
            barrier.SetActive(false);
    }

    /// <summary>
    /// Перейти в патрулирующее состояние
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        MainTarget = ETarget.zero;
        StartCoroutine("TeleportToPositionProcess", (Vector2)currentTarget);
        foreach (GameObject barrier in barriers)
            barrier.SetActive(false);
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
        if (mainTarget.exists && employment > 5)
        {
            StopMoving();
            Vector2 targetPosition = mainTarget;
            Vector2 pos = transform.position;
            Vector2 targetDirection = targetPosition - pos;
            float sqDistance = targetDirection.sqrMagnitude;
            if (targetDirection.x * (int)orientation < 0f)
                Turn();

            if (sqDistance < waitingNearDistance*waitingNearDistance)
            {
                IEnumerator teleportProcess = TeleportProcess(false, false, false, false);
                StartCoroutine(teleportProcess);
                return;
            }

            if (sqDistance < attackDistance * attackDistance)
                if (employment > 8)
                {
                    if ((int)orientation * targetDirection.x < 0f)
                        Turn();
                    ChooseAttack();
                }
        }
        Animate(new AnimationEventArgs("groundMove"));
    }

    /// <summary>
    /// Поведение патрулирования
    /// </summary>
    protected override void PatrolBehavior()
    {
        if (!currentTarget.Exists)
            return;
        if (employment>5)
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

    #endregion //behaviorActions

    #region optimization

    /// <summary>
    /// Функция анализа в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
    }

    /// <summary>
    /// Перейти в оптимизированную версию
    /// </summary>
    protected override void ChangeBehaviorToOptimized()
    {
        transform.position = beginPosition;
        BecomeCalm();
    }

    #endregion //optimization

}

/// <summary>
/// Редактор незнакомца
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(StrangerController))]
public class StrangerControllerEditor : AIControllerEditor
{

    StrangerController stranger;
    SerializedObject serStranger;

    SerializedProperty bossName,
        maxHP,
        health,
        phase2Health,
        balance,
        loyalty,
        drop,
        barriers,
        vulnerability;

    bool teleportParametresShow;
    SerializedProperty attackParametres,
        teleportAttackParametres,
        preTeleportTime, 
        endTeleportTime,
        fastPreTeleportTime, 
        fastEndTeleportTime,
        superFastPreTeleportTime,
        fastPreTeleportAttackTime,
        teleportBlinkPeriod,
        teleportChaosTime,
        teleportTrickTime,
        afterAttackTime,
        teleportNearDistance, 
        teleportFarDistance,
        teleportChaosDistance,
        phase1MaxTeleportAttackTimes,
        phase2MaxTeleportAttackTimes,
        teleportAttackCooldown,
        trickCooldown;

    bool areaAttackParametresShow;
    SerializedProperty magicWave,
        shootOffset1,
        shootOffset2,
        areaAttackParametres,
        magicWaveAttackParametres,
        magicWaveSpeed,
        areaAttackCooldown;

    public override void OnEnable()
    {
        stranger = (StrangerController)target;
        serStranger = new SerializedObject(stranger);

        bossName = serStranger.FindProperty("bossName");
        maxHP = serStranger.FindProperty("maxHealth");
        health = serStranger.FindProperty("health");
        phase2Health = serStranger.FindProperty("phase2Health");
        balance = serStranger.FindProperty("balance");
        loyalty = serStranger.FindProperty("loyalty");
        drop = serStranger.FindProperty("drop");
        barriers = serStranger.FindProperty("barriers");
        vulnerability = serStranger.FindProperty("vulnerability");

        attackParametres = serStranger.FindProperty("attackParametres");
        teleportAttackParametres = serStranger.FindProperty("teleportAttackParametres");
        preTeleportTime = serStranger.FindProperty("preTeleportTime");
        endTeleportTime = serStranger.FindProperty("endTeleportTime");
        fastPreTeleportTime = serStranger.FindProperty("fastPreTeleportTime");
        fastEndTeleportTime = serStranger.FindProperty("fastEndTeleportTime");
        superFastPreTeleportTime = serStranger.FindProperty("superFastPreTeleportTime");
        fastPreTeleportAttackTime = serStranger.FindProperty("fastPreTeleportAttackTime");
        teleportBlinkPeriod = serStranger.FindProperty("teleportBlinkPeriod");
        teleportChaosTime = serStranger.FindProperty("teleportChaosTime");
        teleportTrickTime = serStranger.FindProperty("teleportTrickTime");
        afterAttackTime = serStranger.FindProperty("afterAttackTime");
        teleportNearDistance = serStranger.FindProperty("teleportNearDistance");
        teleportFarDistance = serStranger.FindProperty("teleportFarDistance");
        teleportChaosDistance = serStranger.FindProperty("teleportChaosDistance");
        phase1MaxTeleportAttackTimes = serStranger.FindProperty("phase1MaxTeleportAttackTimes");
        phase2MaxTeleportAttackTimes = serStranger.FindProperty("phase2MaxTeleportAttackTimes");
        teleportAttackCooldown = serStranger.FindProperty("teleportAttackCooldown");
        trickCooldown = serStranger.FindProperty("trickCooldown");

        magicWave = serStranger.FindProperty("magicWave");
        shootOffset1 = serStranger.FindProperty("shootOffset1");
        shootOffset2 = serStranger.FindProperty("shootOffset2");
        areaAttackParametres = serStranger.FindProperty("areaAttackParametres");
        magicWaveAttackParametres = serStranger.FindProperty("magicWaveAttackParametres");
        magicWaveSpeed = serStranger.FindProperty("magicWaveSpeed");
        areaAttackCooldown = serStranger.FindProperty("areaAttackCooldown");

    }

    public override void OnInspectorGUI()
    {

        EditorGUILayout.LabelField("General Parametres");

        EditorGUILayout.PropertyField(bossName);
        maxHP.floatValue = EditorGUILayout.FloatField("Max Health", maxHP.floatValue);
        EditorGUILayout.PropertyField(health);
        EditorGUILayout.PropertyField(phase2Health);
        balance.intValue = EditorGUILayout.IntField("Balance", balance.intValue);
        EditorGUILayout.PropertyField(loyalty);
        EditorGUILayout.PropertyField(drop, true);
        EditorGUILayout.PropertyField(barriers,true);
        stranger.Vulnerability = (byte)(DamageType)EditorGUILayout.EnumMaskPopup(new GUIContent("vulnerability"), (DamageType)stranger.Vulnerability);

        EditorGUILayout.Space();

        teleportParametresShow = EditorGUILayout.Foldout(teleportParametresShow, "Teleport Parametres");
        if (teleportParametresShow)
        {
            EditorGUILayout.PropertyField(attackParametres, true);
            EditorGUILayout.PropertyField(teleportAttackParametres, true);
            EditorGUILayout.PropertyField(preTeleportTime);
            EditorGUILayout.PropertyField(endTeleportTime);
            EditorGUILayout.PropertyField(fastPreTeleportTime);
            EditorGUILayout.PropertyField(fastEndTeleportTime);
            EditorGUILayout.PropertyField(superFastPreTeleportTime);
            EditorGUILayout.PropertyField(fastPreTeleportAttackTime);
            EditorGUILayout.PropertyField(teleportBlinkPeriod);
            EditorGUILayout.PropertyField(teleportChaosTime);
            EditorGUILayout.PropertyField(teleportTrickTime);
            EditorGUILayout.PropertyField(afterAttackTime);
            EditorGUILayout.PropertyField(teleportNearDistance);
            EditorGUILayout.PropertyField(teleportFarDistance);
            EditorGUILayout.PropertyField(teleportChaosDistance);
            EditorGUILayout.PropertyField(phase1MaxTeleportAttackTimes);
            EditorGUILayout.PropertyField(phase2MaxTeleportAttackTimes);
            EditorGUILayout.PropertyField(teleportAttackCooldown);
            EditorGUILayout.PropertyField(trickCooldown);
        }

        EditorGUILayout.Space();

        areaAttackParametresShow = EditorGUILayout.Foldout(areaAttackParametresShow, "Area Attack Parametres");
        if (areaAttackParametresShow)
        {
            magicWave.objectReferenceValue = EditorGUILayout.ObjectField("Magic Wave", magicWave.objectReferenceValue, typeof(GameObject));
            EditorGUILayout.PropertyField(shootOffset1);
            EditorGUILayout.PropertyField(shootOffset2);
            EditorGUILayout.PropertyField(areaAttackParametres, true);
            EditorGUILayout.PropertyField(magicWaveAttackParametres, true);
            EditorGUILayout.PropertyField(magicWaveSpeed);
            EditorGUILayout.PropertyField(areaAttackCooldown);
        }

        serStranger.ApplyModifiedProperties();
    }

}
#endif //UNITY_EDITOR