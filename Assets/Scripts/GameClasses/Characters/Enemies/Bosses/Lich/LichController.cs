using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Контроллер, управляющий личом
/// </summary>
public class LichController : BossController
{

    #region consts

    protected const float areaRadius = 4f;//Насколько далеко может лич отходить от своей начальной позиции
    protected const float appearTime = 1.6f;//Сколько времени персонаж появляется

    #endregion //consts

    #region fields

    protected Hearing hearing;//Слух персонажа

    [SerializeField]protected GameObject missile;//Снаряды стрелка
    [SerializeField]protected GameObject summon;//Кого может призвать лич
    protected List<AIController> minions = new List<AIController>();//Миньоны, подконтрольные личу

    #endregion //fields

    #region parametres

    public override int ID
    {
        get
        {
            return base.ID;
        }

        set
        {
            StartCoroutine("AppearProcess");
            base.ID = value;
        }
    }

    protected override float sightRadius{get{return 4f;}}//Расстояние, на котором босс перестаёт замечать цель

    [SerializeField] protected float missileSpeed=3f;//Скорость снаряда после выстрела
    [SerializeField] protected Vector2 shootOffset=new Vector2(0.062f, -0.021f);//Откуда происходить выстрел
    [SerializeField] protected float attackRate = 1.4f,//Как часто лич может вообще совершать атаки
                                    shootCooldown=2f,//Кулдаун стрельбы
                                    summonCooldown=5f;//Кулдаун призыва

    protected float summonMaxDistance = 1f, summonMinDIstance = .5f;
    //[SerializeField]protected int maxMinionCount;//Максимальное число миньонов, которые может призвать лич
    [SerializeField]protected float summonPreTime, summonEndTime;//Времена призыва миньонов
    protected bool appearing = false;

    //Список кулдаунов
    protected override List<Timer> Timers { get { return new List<Timer> { new Timer("attackCooldown",attackRate),
                                                                           new Timer("shootCooldown", shootCooldown),
                                                                           new Timer("summonCooldown", summonCooldown)}; } }



    #endregion //parametres

    protected override void FixedUpdate()
    {
        if (!immobile)
            base.FixedUpdate();
        if (!appearing)
            Animate(new AnimationEventArgs("fly"));
    }

    protected override void Initialize()
    {
        minions = new List<AIController>();
        indicators = transform.FindChild("Indicators");
        if (indicators != null)
        {
            hearing = indicators.GetComponentInChildren<Hearing>();
            hearing.AllyHearing = false;
            hearing.hearingEventHandler += HandleHearingEvent;
        }
        base.Initialize();
        rigid.gravityScale = 0f;

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            areaTrigger.InitializeAreaTrigger();
        }

        BecomeCalm();
    }

    /// <summary>
    /// Процесс появления лича
    /// </summary>
    /// <returns></returns>
    protected IEnumerator AppearProcess()
    {
        immobile = true;
        appearing = true;
        yield return new WaitForSeconds(.05f);
        Animate(new AnimationEventArgs("appear"));
        yield return new WaitForSeconds(appearTime);
        immobile = false;
        appearing = false;
    }

#region movement

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = (currentTarget - transform.position).normalized * speed * speedCoof;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Остановить передвижение
    /// </summary>
    protected override void StopMoving()
    {
        rigid.velocity = Vector2.zero;
    }

    /// <summary>
    /// Двинуться прочь от цели
    /// </summary>
    /// <param name="_orientation">Ориентация персонажа при перемещении</param>
    protected override void MoveAway(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = (transform.position - currentTarget).normalized * speed * speedCoof;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Если лич оказался во вермя боя далеко от начальной позиции, то он будет к ней возвращаться
    /// </summary>
    protected virtual void RushToStartPosition()
    {
        if (Vector2.SqrMagnitude(transform.position - beginPosition) > areaRadius * .75f)
            currentTarget = new ETarget(beginPosition);
    }

#endregion //movement

#region attack

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        StopMoving();
        Animate(new AnimationEventArgs("attack", "", Mathf.RoundToInt(100 * (attackParametres.preAttackTime+attackParametres.endAttackTime))));
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
        Vector2 _shootPosition = pos + new Vector2(shootOffset.x * (int)orientation, shootOffset.y);
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
            missileHitBox.AttackerInfo = new AttackerClass(gameObject, AttackTypeEnum.range);
        }
        yield return new WaitForSeconds(attackParametres.endAttackTime);
        employment = Mathf.Clamp(employment + 8, 0, maxEmployment);

        StartTimer("attackCooldown");
        StartTimer("shootCooldown");

    }

    /// <summary>
    /// Призыв миньона
    /// </summary>
    protected virtual void Summon()
    {
        StopMoving();
        StartCoroutine("SummonProcess");
    }

    /// <summary>
    /// Процесс призыва
    /// </summary>
    protected virtual IEnumerator SummonProcess()
    {
        Animate(new AnimationEventArgs("attack", "Summon", Mathf.RoundToInt(100*(summonPreTime+summonEndTime))));
        employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        yield return new WaitForSeconds(summonPreTime);
        for (int i = 0; i < 3; i++)
        {
            GameObject _minion = SpecialFunctions.gameController.InstantiateWithId(summon,
                                                                                 transform.position + (Random.Range(0f, 1f) > .5f ? 1 : -1) *
                                                                                                       Vector3.right *
                                                                                                       Random.Range(summonMinDIstance, summonMaxDistance),
                                                                                  Quaternion.identity) as GameObject;
            AIController _ai = _minion.GetComponent<AIController>();
            minions.Add(_ai);
            _ai.Turn(orientation);
            _ai.StoryGoToThePoint(new StoryAction("goToThePoint", "hero", "", 0));
            _ai.CharacterDeathEvent += HandleMinionDeathEvent;
        }
        yield return new WaitForSeconds(summonEndTime);
        employment = Mathf.Clamp(employment + 8, 0, maxEmployment);
        RushToStartPosition();
        StartTimer("attackCooldown");
        StartTimer("summonCooldown");
    }

    /// <summary>
    /// Остановить атаку
    /// </summary>
    protected override void StopAttack()
    {
        base.StopAttack();
        StopCoroutine("SummonProcess");
        StopTimer("attackCooldown",true);
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(HitParametres hitData)
    {
        if (appearing)
            return;
        base.TakeDamage(hitData);
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
        if (appearing)
            return;
        base.TakeDamage(hitData, ignoreInvul);
    }

    //Функция смерти
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
        for (int i=minions.Count-1;i>=0;i--)
            minions[i].TakeDamage(new HitParametres(10000f, DamageType.Physical));//Все призванные миньоны также умирают
        Destroy(gameObject,2f);
    }

#endregion //attack

#region damageEffects

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

#endregion //damageEffects

    /// <summary>
    /// Анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        base.Analyse();
        Vector2 pos = transform.position;

        if (behavior == BehaviorEnum.agressive)
        {
            //Если текущая цель убежала достаточно далеко или сам лич вышел далеко от начальной позиции, то лич будет возвращаться
            if (Vector2.SqrMagnitude(mainTarget - pos) > sightRadius * sightRadius || Vector2.SqrMagnitude(pos - beginPosition) > areaRadius * areaRadius)
            {
                ChangeMainTarget();
                if (mainTarget.Exists?Vector2.SqrMagnitude(mainTarget - pos) > sightRadius * sightRadius || Vector2.SqrMagnitude(pos - beginPosition) > areaRadius * areaRadius: true)
                    GoHome();
            }
            CheckMinions();
        }       
    }

    /// <summary>
    /// Заставляет миньонов рваться в бой
    /// </summary>
    protected virtual void CheckMinions()
    {
        foreach (AIController _minion in minions)
            if (_minion.Behavior == BehaviorEnum.calm)
                _minion.StoryGoToThePoint(new StoryAction("goToThePoint", "hero", "", 0));
    }

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
    /// Разозлиться
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        hearing.enabled = false;//В агрессивном состоянии персонажу не нужен слух
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        hearing.enabled = true;
    }

    /// <summary>
    /// Перейти в состояние патрулирования
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        hearing.enabled = true;
    }

        /// <summary>
    /// Выдвинуться к указанной точке
    /// </summary>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        currentTarget = new ETarget(targetPosition);
        BecomePatrolling();
    }

    //Функция, реализующая агрессивное состояние ИИ
    protected override void AgressiveBehavior()
    {
        if (!mainTarget.exists || !currentTarget.exists || employment <= 3)
            return;

        Vector2 targetPosition = currentTarget;
        Vector2 pos = transform.position;
        float sqDistance = Vector2.SqrMagnitude(targetPosition - pos);
        if (currentTarget == mainTarget)
        {
            if (sqDistance < waitingNearDistance * waitingNearDistance)
            {
                if (!IsTimerActive("attackCooldown"))
                    MoveAway((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(pos.x - targetPosition.x)));
            }
            else if (sqDistance < waitingFarDistance * waitingFarDistance)
            {
                StopMoving();
                if ((targetPosition - pos).x * (int)orientation < 0f)
                    Turn();
            }
            else
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));

        }
        else
        {
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
        }

        if (sqDistance<waitingFarDistance*waitingFarDistance && !IsTimerActive("attackCooldown"))
        {
            if (!IsTimerActive("shootCooldown"))
            {
                currentTarget = mainTarget;
                if ((targetPosition - pos).x * (int)orientation < 0f)
                    Turn();
                Attack();
                return;
            }
            if (!IsTimerActive("summonCooldown") && minions.Count==0)
            {
                currentTarget = mainTarget;
                if ((targetPosition - pos).x * (int)orientation < 0f)
                    Turn();
                Summon();
                return;
            }
        }
    }

    /// <summary>
    /// Функция, реализующая состояние ИИ, при котором тот перемещается между текущими точками следования
    /// </summary>
    protected override void PatrolBehavior()
    {

        if (!currentTarget.exists)
        {
            GoHome();
            return;
        }

        Vector2 targetPosition = currentTarget;
        Vector2 pos = transform.position;
        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
        if ( Vector2.SqrMagnitude(targetPosition) < minDistance)
        {
            if (Vector2.SqrMagnitude(beginPosition - pos) < minDistance)
                BecomeCalm();
            else
                GoHome(); 
        }
    }

#endregion //behaviorAction

#region optimization

    protected override void AnalyseOpt()
    {
    }

    protected override void ChangeBehaviorToOptimized()
    {
        transform.position = beginPosition;
        BecomeCalm();
    }

#endregion //optimization

#region eventHandlers

    /// <summary>
    /// Учесть, что число призванных миньонов уменьшилось
    /// </summary>
    public void HandleMinionDeathEvent(object sender, StoryEventArgs e)
    {
        if (!(sender is AIController))
            return;
        minions.Remove((AIController)sender);
    }

#endregion //eventHandlers

}
