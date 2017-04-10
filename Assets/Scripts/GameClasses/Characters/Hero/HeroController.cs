using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер, управляющий ГГ
/// </summary>
public class HeroController : CharacterController
{

    #region consts

    protected const float groundRadius = .01f;
    protected const float jumpTime = .2f, jumpInputTime = .2f;
    protected const float flipTime = .6f;

    protected const int maxJumpInput = 10;

    protected const float invulTime = 1f;
    protected const float minChargeTime = .3f;//Какое минимальное время нужно дердать кнопку атаки, чтобы начать зарядку оружия (используется, чтобы отличить обычную атаку от заряженной атаки)

    protected const float ladderCheckOffset = .05f, ladderStep = .01f;

    protected const float minDamageFallSpeed = 4.2f;//Минимальная скорость по оси y, которая должна быть при падении, чтобы засчитался урон
    protected const float damagePerFallSpeed = 2f;

    protected const float suffocateTime = .3f;//Сколько времени должно пройти, чтобы запас воздуха уменьшился на 1 или здоровье ГГ на .5
    protected const int maxAirSupply = 10;

    protected const float highWallCheckPosition = 0.02f, lowWallCheckPosition = 0f;
    protected const float highWallCheckSize = .08f, lowWallCheckSize = .05f;

    protected const float totemAnimalTime = 20f;//Время действия эффекта "Тотемное животное"
    protected const float tribalRitualTime = 15f;//Время действия эффекта "Ритуал племени"
    protected const float tribalRitualCoof = 1.3f;//Во сколько раз увеличивается скорость при эффекте "Ритуал племени"

    #endregion //consts

    #region fields

    protected Transform waterCheck;
    protected Transform wallAboveCheck;//Индикатор того, что над персонажем располагается твёрдое тело, земля
    protected WallChecker wallCheck;//Индикатор, необходимый для отсутствия зависаний в стене
    protected WallChecker groundCheck;
    protected Interactor interactor;//Индикатор, ответственный за обнаружение и взаимодействие со всеми интерактивными объектами

    protected Collider2D col1, col2;

    [SerializeField]
    protected WeaponClass currentWeapon;//Оружие, которое используется персонажем в данный момент
    public WeaponClass CurrentWeapon
    {
        get
        {
            return currentWeapon;
        }
        set
        {
            currentWeapon = value.GetWeapon();
            if (currentWeapon is BowClass)
            {
                ((BowClass)currentWeapon).ReloadWeapon();
                fightingMode = AttackTypeEnum.range;
            }
            else
                fightingMode = AttackTypeEnum.melee;
            OnEquipmentChanged(new EquipmentEventArgs(currentWeapon, null));
            hitBox.AttackerInfo = new AttackerClass(gameObject, fightingMode);
        }
    }

    protected EquipmentClass equipment;//Инвентарь игрока.
    public EquipmentClass Equipment { get { return equipment; } set { equipment = value; } }
    public GameObject dropPrefab;
    public List<TrinketClass> trinkets = new List<TrinketClass>();

    [SerializeField]protected GameObject summonedAnimal;//Животное, которое может призвать на помощь герой

    #endregion //fields

    #region parametres

    public override float Health { get { return base.Health; } set { float prevHealth = health; base.Health = value; OnHealthChanged(new HealthEventArgs(value, health-prevHealth, maxHealth)); } }
    public override float MaxHealth { get { return base.MaxHealth; } set { maxHealth = value; OnHealthChanged(new HealthEventArgs(value, 0f, maxHealth)); } }
    public int Balance { get { return balance;} set { balance = value; } }
    public float Speed { get { return speed; } set { speed = value; } }
    public float JumpForce { get { return jumpForce; } set { jumpForce = value; } }
    public float JumpAdd { get { return jumpAdd;} set { jumpAdd = value; } }

    protected int airSupply = 10;//Запас воздуха
    public int AirSupply { get { return airSupply; } set { airSupply = value; OnSuffocate(new SuffocateEventArgs(airSupply)); } }
    protected override bool Underwater//Свойство, которое описывает погружение героя в воду
    {
        get
        {
            return base.Underwater;
        }

        set
        {
            underWater = value;
            WaterIndicator waterIndicator = waterCheck.GetComponent<WaterIndicator>();
            if (value)
            {
                if (GetBuff("FrozenProcess") == null)
                {
                    BecomeWet(0f);
                    StopCoroutine("WetProcess");
                    Animate(new AnimationEventArgs("stopWet"));
                }
                speedCoof *= waterCoof;
                rigid.gravityScale = .6f;
                waterIndicator.StartCoroutine(SuffocateProcess());
            }
            else
            {
                if (GetBuff("FrozenProcess") == null)
                {
                    StopWet();
                    BecomeWet(0f);
                    rigid.gravityScale = 1f;
                    AirSupply = maxAirSupply;
                }
                waterIndicator.StopAllCoroutines();
                speedCoof /= waterCoof;
            }
            Animate(new AnimationEventArgs("waterSplash"));
        }
    }

    [SerializeField] protected float jumpForce = 200f,
                                     jumpAdd = 20f,//Добавление к силе прыжка при зажимании
                                     flipForce = 200f,
                                     ladderSpeed = .8f,
                                     waterCoof = .7f;

    [SerializeField] protected LayerMask whatIsGround, whatIsAim;

    protected bool jumping;
    protected int jumpInput = 0;
    protected GroundStateEnum groundState;
    protected float fallSpeed = 0f;
    protected bool onLadder;
    public override bool OnLadder { get { return onLadder; } }
    protected bool dontShoot = false;

    protected AttackerClass attacker;
    protected TrinketEffectClass mutagenEffect;//Эффект, который связан с мутагеном
    protected bool mutagenActive = false;//Действует ли мутаген
    protected bool invul;//Если true, то персонаж невосприимчив к урону

    protected AttackTypeEnum fightingMode;
    public bool attacking = false;
    protected float chargeBeginTime = 0f;

    #region effectParametres

    protected override float stunTime { get { return 1f; } }

    protected override float burnDamage { get { return 1f; } }

    protected override float poisonDamage { get { return .5f; } }
       
    protected override float frozenTime { get { return 2f; } }

    #endregion //effectParametres

    #endregion //parametres

    #region eventHandlers

    public EventHandler<EquipmentEventArgs> equipmentChangedEvent;
    public EventHandler<HealthEventArgs> healthChangedEvent;
    public EventHandler<SuffocateEventArgs> suffocateEvent;
    public EventHandler<BuffEventArgs> buffAddEvent;
    public EventHandler<BuffEventArgs> buffRemoveEvent;

    #endregion //eventHandlers

    protected virtual void Update()
    {
        if (!immobile)
        {
            #region usualMovement

            if (!onLadder)
            {
                if (employment > 6)
                {
                    float horValue = Input.GetAxis("Horizontal");
                    float jHorValue = JoystickController.instance.GetAxis(JAxis.Horizontal);
                    if (Input.GetButton("Horizontal") || Mathf.Abs(jHorValue) > .1f)
                    {
                        float value = Mathf.Abs(horValue) > Mathf.Abs(jHorValue) ? horValue : jHorValue;
                        Move(value > 0f ? OrientationEnum.right : OrientationEnum.left);
                    }

                    if (Input.GetButtonDown("Jump") || JoystickController.instance.GetButtonDown(JButton.button2))
                    {
                        jumpInput = 0;
                        if (groundState == GroundStateEnum.grounded && !jumping)
                        {
                            Jump();
                        }
                    }

                    //if (Input.GetButton("Jump"))
                    //{
                        //if (jumpInput)
                        //rigid.AddForce(new Vector2(0f, jumpAdd * (underWater ? waterCoof : 1f)));
                    //}

                    if (Input.GetButtonUp("Jump") || JoystickController.instance.GetButtonUp(JButton.button2))
                    {
                        jumpInput = 0;
                    }

                    if (Input.GetAxis("Vertical") > .2f || JoystickController.instance.GetAxis(JAxis.Vertical)>.2f)
                    {
                        LadderOn();
                    }

                    if (employment > 7)
                    {
                        if (Input.GetButtonDown("Attack") || JoystickController.instance.GetButtonDown(JButton.button7))
                        {
                            if (groundState != GroundStateEnum.crouching)
                            {
                                if (interactor.ReadyForInteraction())
                                    interactor.Interact();
                                else
                                {
                                    if (currentWeapon.chargeable)
                                        StartCharge();
                                    else
                                        Attack();
                                }
                            }
                        }
                        else if (Input.GetButtonDown("Flip") || JoystickController.instance.GetButtonDown(JButton.button6))
                            if ((rigid.velocity.x * (int)orientation > .1f) && (groundState == GroundStateEnum.grounded) && (employment > 8))
                                Flip();
                        if (Input.GetButtonDown("ChangeInteraction") || JoystickController.instance.GetButtonDown(JButton.button5))
                            interactor.ChangeInteraction();
                    }
                }

                if (currentWeapon.chargeable)
                {
                    if ((Input.GetButtonUp("Attack") || JoystickController.instance.GetButtonUp(JButton.button7)) && chargeBeginTime>0f)
                    {
                        StopCharge();
                    }
                }
            }

            #endregion //usualMovement

            #region ladderMovement

            else
            {
                float vertValue = Input.GetAxis("Vertical");
                float jVertValue = JoystickController.instance.GetAxis(JAxis.Vertical);
                if ( Input.GetButton("Vertical") || Mathf.Abs(jVertValue)>.1f)
                    LadderMove(Mathf.Abs(vertValue)>Mathf.Abs(jVertValue)? vertValue: jVertValue);
                else
                    StopLadderMoving();
                if (Input.GetButtonDown("Jump") || JoystickController.instance.GetButtonDown(JButton.button2))
                {
                    LadderOff();
                    rigid.AddForce(new Vector2(0f, jumpForce / 2));
                    StartCoroutine(JumpProcess());
                }
            }

            #endregion //ladderMovement

        }

        Analyse();

        if (onLadder)
        {
            Animate(new AnimationEventArgs("ladderMove"));
        }
        else if (groundState == GroundStateEnum.inAir)
        {
            Animate(new AnimationEventArgs("airMove"));
        }
        else
        {
            Animate(new AnimationEventArgs("groundMove", groundState == GroundStateEnum.crouching ? "crouching" : "", 0));
        }
    }

    protected virtual void FixedUpdate()
    {
        if (jumpInput > 0 && jumpInput <= maxJumpInput)
        {
            rigid.AddForce(new Vector2(0f, jumpAdd * (underWater ? waterCoof : 1f)));
            jumpInput++;
        }
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
        indicators = transform.FindChild("Indicators");
        waterCheck = indicators.FindChild("WaterCheck");
        groundCheck = indicators.FindChild("GroundCheck").GetComponent<WallChecker>();
        groundCheck.enabled = false;
        wallCheck = indicators.FindChild("WallCheck").GetComponent<WallChecker>();
        wallAboveCheck = indicators.FindChild("WallAboveCheck");
        interactor = indicators.FindChild("Interactor").GetComponent<Interactor>();

        immobile = false;
        jumping = false;
        onLadder = false;

        mutagenEffect = null;
        equipment = new EquipmentClass();
        if (currentWeapon != null)
        {
            currentWeapon = currentWeapon.GetWeapon();//Сразу же в начале игры в это поле занесём независимую от 
                                                      //оригинала копию этого же поля, чтобы можно было производить операции с оружием, не меняя файловую структуру игры
            fightingMode = (currentWeapon is SwordClass) ? AttackTypeEnum.melee : AttackTypeEnum.range;
            SetItem(currentWeapon);
        }

        Collider2D[] cols = new Collider2D[2];
        cols = GetComponents<Collider2D>();
        if (cols.Length > 0)
        {
            col1 = cols[0];
            if (cols.Length > 1)
            {
                col2 = cols[1];
                col2.enabled = false;
            }
        }

        if (!PlayerPrefs.HasKey("Hero Health"))//Здоровье не восполняется при переходе на следующий уровень. Поэтому, его удобно сохранять в PlayerPrefs
            PlayerPrefs.SetFloat("Hero Health", maxHealth);
        Health = PlayerPrefs.GetFloat("Hero Health");
    }

    /// <summary>
    /// Анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        if (groundCheck.CheckWall())
            groundState = GroundStateEnum.grounded;
        else
            groundState = GroundStateEnum.inAir;

        if ((groundState == GroundStateEnum.grounded))
        {
            bool crouching = false;
            if (Physics2D.OverlapCircle(wallAboveCheck.position, groundRadius, whatIsGround)/* || Input.GetButton("Flip")*/)
            {
                groundState = GroundStateEnum.crouching;
                crouching = true;
            }
            if (employment > 6)
            {
                Crouch(crouching);
            }

            if (fallSpeed > minDamageFallSpeed)
            {
                TakeDamage(new HitParametres(Mathf.Round((fallSpeed - minDamageFallSpeed) * damagePerFallSpeed), DamageType.Physical,1), true);
            }
            if (fallSpeed > minDamageFallSpeed / 10f)
                Animate(new AnimationEventArgs("fall"));
            fallSpeed = 0f;
        }
        else
        {
            Crouch(false);
            fallSpeed = -rigid.velocity.y;
        }

        bool _underWater = Physics2D.OverlapCircle(waterCheck.position, groundRadius, LayerMask.GetMask("Water"));
        if (underWater != _underWater)
        {
            Underwater = _underWater;
        }

        if (onLadder)
        {
            if (Input.GetAxis("Vertical") < -.2f || JoystickController.instance.GetAxis(JAxis.Vertical)<-.2f ? 
                !Physics2D.OverlapCircle(transform.position - transform.up * ladderCheckOffset, ladderStep, LayerMask.GetMask("ladder")):false)
            {
                LadderOff();
                rigid.AddForce(new Vector2(0f, jumpForce / 2));
                StartCoroutine(JumpProcess());
            }
        }

    }

    /// <summary>
    /// Процесс самого прыжка
    /// </summary>
    protected virtual IEnumerator JumpProcess()
    {
        employment = Mathf.Clamp(employment - 2, 0, maxEmployment);
        jumpInput = 1;
        yield return new WaitForSeconds(jumpInputTime);
        jumpInput = 0;
        yield return new WaitForSeconds(jumpTime);
        employment = Mathf.Clamp(employment + 2, 0, maxEmployment);
        jumping = false;
    }

    /// <summary>
    /// Функция приседания
    /// </summary>
    protected void Crouch(bool crouching)
    {
        col1.enabled = !crouching;
        col2.enabled = crouching;
        Vector2 size = wallCheck.Size;
        Vector2 pos = wallCheck.DefaultPosition;
        wallCheck.DefaultPosition = new Vector2(pos.x, crouching ? lowWallCheckPosition : highWallCheckPosition);
        wallCheck.Size = new Vector2(size.x, crouching ? lowWallCheckSize : highWallCheckSize);
    }

    /// <summary>
    /// Процесс задыхания под вод
    /// </summary>
    protected IEnumerator SuffocateProcess()
    {
        //Сначала заканчивается запас воздуха
        int _airSupply = airSupply;
        for (int i = 0; i < _airSupply; i++)
        {
            yield return new WaitForSeconds(suffocateTime);
            AirSupply--;
        }
        //А потом и жизнь персонажа
        while (true)
        {
            yield return new WaitForSeconds(suffocateTime*4f);
            TakeDamage(new HitParametres(1f, DamageType.Physical, 0), true);
        }
    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        float horValue = Input.GetAxis("Horizontal");
        float jHorValue = JoystickController.instance.GetAxis(JAxis.Horizontal);
        float value = Mathf.Abs(horValue) > Mathf.Abs(jHorValue) ? horValue : jHorValue;
        bool crouching = (groundState == GroundStateEnum.crouching);
        rigid.velocity = new Vector3((wallCheck.WallInFront) ? 0f : value * speed*speedCoof, rigid.velocity.y);
        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Прекратить перемещение
    /// </summary>
    protected override void StopMoving()
    {
        base.StopMoving();
        rigid.velocity = new Vector3(0f, rigid.velocity.y);
    }

    /// <summary>
    /// Взобраться на лестницу
    /// </summary>
    protected override void LadderOn()
    {
        if (interactor.Ladder != null)
        {
            base.LadderOn();
            onLadder = true;
            Animate(new AnimationEventArgs("setLadderMove", "", 1));
            Vector3 vect = transform.position;
            transform.position = new Vector3(interactor.Ladder.transform.position.x, vect.y, vect.z);
        }
    }

    /// <summary>
    /// Слезть с лестницы
    /// </summary>
    protected override void LadderOff()
    {
        onLadder = false;
        rigid.gravityScale = underWater? .6f: 1f;
        Animate(new AnimationEventArgs("setLadderMove", "", 0));
        //if (Input.GetAxis("Vertical")>0f)
        //{
        //  rigid.AddForce(new Vector2(0f, jumpForce / 2));
        //  StartCoroutine(JumpProcess());
        //}
    }

    /// <summary>
    /// Перемещение по лестнице
    /// </summary>
    protected override void LadderMove(float direction)
    {
        rigid.velocity = new Vector3(0f,
                         Physics2D.OverlapCircle(transform.position + Mathf.Sign(direction) * transform.up * ladderCheckOffset, ladderStep, LayerMask.GetMask("ladder")) ? 
                         direction * ladderSpeed * speedCoof : 0f);
    }

    /// <summary>
    /// Развернуться
    /// </summary>
    /// <param name="_orientation">В какую сторону должен смотреть персонаж после поворота</param>
    public override void Turn(OrientationEnum _orientation)
    {
        if (employment >= 8)
        {
            base.Turn(_orientation);
            wallCheck.SetPosition(0f, (int)orientation);
        }
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    protected override void Turn()
    {
        base.Turn();
        wallCheck.SetPosition(0f, (int)orientation);
    }

    /// <summary>
    /// Прыгнуть
    /// </summary>
    protected override void Jump()
    {
        jumping = true;
        rigid.AddForce(new Vector2(0f, jumpForce * (underWater ? waterCoof : 1f)));
        StartCoroutine(JumpProcess());
    }

    /// <summary>
    /// Начать зарядку оружия
    /// </summary>
    protected virtual void StartCharge()
    {
        if (fightingMode == AttackTypeEnum.range ? !((BowClass)currentWeapon).canShoot : false)
            return;
        chargeBeginTime = Time.fixedTime;
        int employmentDelta = fightingMode == AttackTypeEnum.range ? 5 : 3;
        employment = Mathf.Clamp(employment - employmentDelta, 0, maxEmployment);
        Animate(new AnimationEventArgs("holdAttack", currentWeapon.itemName, 0));
    }

    /// <summary>
    /// Закончить зарядку оружия
    /// </summary>
    protected virtual void StopCharge()
    {
        int employmentDelta = fightingMode == AttackTypeEnum.range ? 5 : 3;
        float chargeTime = (Time.fixedTime - chargeBeginTime);
        if (chargeTime < minChargeTime)
            chargeBeginTime = 0f;
        currentWeapon.ChargeValue = chargeTime;
        employment = Mathf.Clamp(employment + employmentDelta, 0, maxEmployment);
        Attack();            
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        currentWeapon.StartAttack();
        if (fightingMode == AttackTypeEnum.melee)
        {
            if (chargeBeginTime > 0f)
                Animate(new AnimationEventArgs("releaseAttack", currentWeapon.itemName, Mathf.RoundToInt(10 * currentWeapon.attackTime + currentWeapon.endAttackTime)));
            else
            {
                Animate(new AnimationEventArgs("releaseAttack", "",0));
                Animate(new AnimationEventArgs("attack", currentWeapon.itemName, Mathf.RoundToInt(10 * (currentWeapon.preAttackTime + currentWeapon.attackTime + currentWeapon.endAttackTime))));
            }
            StartCoroutine(AttackProcess());
        }
        else if (fightingMode == AttackTypeEnum.range)
        {
            if (((BowClass)currentWeapon).canShoot)
            {
                StopMoving();
                if (chargeBeginTime > 0f)
                    Animate(new AnimationEventArgs("releaseAttack", currentWeapon.itemName, Mathf.RoundToInt(10 * currentWeapon.attackTime)));
                else
                {
                    Animate(new AnimationEventArgs("releaseAttack", "", 0));
                    Animate(new AnimationEventArgs("shoot", currentWeapon.itemName, Mathf.RoundToInt(10 * (currentWeapon.preAttackTime + currentWeapon.attackTime))));
                }
                StartCoroutine(ShootProcess());
            }
        }
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        SwordClass sword = (SwordClass)currentWeapon;

        if (chargeBeginTime == 0f)
            yield return new WaitForSeconds(sword.preAttackTime);
        else
            chargeBeginTime = 0f;

        attacking = true;
        sword.Attack(hitBox, transform.position);
        hitBox.AttackDirection = Vector2.right * (int)orientation;
        yield return new WaitForSeconds(sword.attackTime);
        attacking = false;
        yield return new WaitForSeconds(sword.endAttackTime);
        currentWeapon.StopAttack();
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Процесс выстрела
    /// </summary>
    protected virtual IEnumerator ShootProcess()
    {
        employment = Mathf.Clamp(employment - 5, 0, maxEmployment);
        BowClass bow = (BowClass)currentWeapon;

        if (chargeBeginTime == 0f)
            yield return new WaitForSeconds(currentWeapon.preAttackTime);
        else
            chargeBeginTime = 0f;

        bow.Shoot(hitBox, transform.position + Vector3.right*(int)orientation*.05f, (int)orientation, whatIsAim, enemies);
        yield return new WaitForSeconds(currentWeapon.attackTime);

        currentWeapon.StopAttack();
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);
    }

    /// <summary>
    /// Остановить атаку
    /// </summary>
    protected override void StopAttack()
    {
        base.StopAttack();
        Animate(new AnimationEventArgs("stop"));
        currentWeapon.StopAttack();
    }

    /// <summary>
    /// Совершить кувырок
    /// </summary>
    protected virtual void Flip()
    {
        rigid.velocity = new Vector2(0f, 0f);
        rigid.AddForce(new Vector2((int)orientation*flipForce*(underWater ? waterCoof : 1f), 0f));
        StartCoroutine(InvulProcess(flipTime, false));
        StartCoroutine(FlipProcess());
        Animate(new AnimationEventArgs("flip"));
    }

    /// <summary>
    /// Процесс кувырка
    /// </summary>
    protected virtual IEnumerator FlipProcess()
    {
        employment = Mathf.Clamp(employment - 5, 0, maxEmployment);
        col1.enabled = false;
        col2.enabled = true;
        yield return new WaitForSeconds(flipTime);
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);
        col1.enabled = true;
        col2.enabled = false;
    }

    public override void TakeAttackerInformation(AttackerClass attackerInfo)
    {
        attacker = attackerInfo;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(HitParametres hitData)
    {
        if (!invul)
        {
            if (mutagenEffect != null)
            {
                float rand = UnityEngine.Random.Range(0f, 1f);
                if (rand < mutagenEffect.effectProbability && attacker != null ? attacker.attackType == AttackTypeEnum.melee : false)
                {
                    SpiritShieldEffect(ref hitData.damage, hitData.damageType, attacker);
                    return;
                }
            }
            Health = Mathf.Clamp(Health - hitData.damage, 0f, maxHealth);

            if (health <= 0f)
            {
                if (hitData.damage > 200f && hitData.damageType == DamageType.Fire)
                {
                    Animate(new AnimationEventArgs("death", "fire", 0));
                    rigid.velocity = Vector2.zero;
                    rigid.isKinematic = true;
                }
                Death();
                return;
            }
            else
                Animate(new AnimationEventArgs("hitted", "", hitData.attackPower>balance ? 0 : 1));

            if ((hitData.damageType != DamageType.Physical) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
                TakeDamageEffect(hitData.damageType);
            bool stunned = GetBuff("StunnedProcess") != null;
            if (hitData.attackPower>balance || stunned)
            {
                //StopCoroutine("AttackProcess");
                //dontShoot = false;
                if (onLadder)
                    LadderOff();
                StopAttack();
            }
            if (hitData.attackPower>0)
                StartCoroutine(InvulProcess(invulTime, true));
            anim.Blink();
        }
        attacker = null;
        mutagenActive = false;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
        if (ignoreInvul || !invul)
        {
            if (mutagenEffect != null && attacker!=null? attacker.attackType==AttackTypeEnum.melee:false)
            {
                float rand = UnityEngine.Random.Range(0f, 1f);
                if (rand < mutagenEffect.effectProbability)
                {
                    SpiritShieldEffect(ref hitData.damage, hitData.damageType, attacker);
                    return;
                }
            }
            Health = Mathf.Clamp(Health - hitData.damage, 0f, maxHealth);
            if (health <= 0f)
            {
                if (hitData.damage > 200f && hitData.damageType == DamageType.Fire)
                {
                    Animate(new AnimationEventArgs("death", "fire", 0));
                    rigid.velocity = Vector2.zero;
                    rigid.isKinematic = true;
                }
                Death();
                return;
            }
            else
                Animate(new AnimationEventArgs("hitted", "", hitData.attackPower > balance ? 0 : 1));

            if ((hitData.damageType != DamageType.Physical) ? UnityEngine.Random.Range(0f, 100f) <= hitData.effectChance : false)
                TakeDamageEffect(hitData.damageType);
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) sprite.enabled = true;
            bool stunned = GetBuff("StunnedProcess") != null;
            if (hitData.attackPower > balance || stunned)
            {
                //StopCoroutine("AttackProcess");
                //dontShoot = false;
                if (onLadder)
                    LadderOff();
            }
            if (hitData.attackPower>0)
                StartCoroutine(InvulProcess(invulTime, true));
            anim.Blink();
        }
        attacker = null;
        mutagenActive = false;
    }

    /// <summary>
    /// Функция, описывающая процессы при смерти персонажа
    /// </summary>
    protected override void Death()
    {
        StopCoroutine("AttackProcess");
        if (indicators != null)
        {
            BattleField bField = indicators.GetComponentInChildren<BattleField>();
            if (bField != null)
                bField.KillAllies();
        }
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            BuffClass buff = buffs[i];
            StopCustomBuff(new BuffData(buff));
        }
        Animate(new AnimationEventArgs("death"));
        immobile = true;
        SpecialFunctions.StartStoryEvent(this, CharacterDeathEvent, new StoryEventArgs());
    }

    /// <summary>
    /// Воскреснуть
    /// </summary>
    protected virtual void Resurect()
    {
        Health = maxHealth;
        immobile = false;
        Animate(new AnimationEventArgs("stop"));
    }

    /// <summary>
    /// Эффект духовного щита
    /// </summary>
    protected virtual void SpiritShieldEffect(ref float _damage, DamageType _dType, AttackerClass _attacker)
    {
        if (_attacker == null || _attacker.attackType != AttackTypeEnum.melee)
            return;
        _damage = 0f;
        AIController ai = attacker.attacker.GetComponent<AIController>();
        if (ai is BossController)
            return;
        ai.TakeDamageEffect(_dType);
        ai.TakeDamage(new HitParametres(ai.MaxHealth*.2f, _dType,0), true);
        attacker = null;
    }

    /// <summary>
    /// Снять с персонажа все активные боевые эффекты (связанные с различными типами урона), восстановить здоровье
    /// </summary>
    public virtual void RestoreStats()
    {
        Health = maxHealth;
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            BuffClass buff = buffs[i];
            StopCustomBuff(new BuffData(buff));
        }
    }

    #region equipment

    /// <summary>
    /// Добавить предмет в инвентарь
    /// </summary>
    public void SetItem(ItemClass item)
    {
        if (item is WeaponClass)
        {
            WeaponClass _weapon = (WeaponClass)item;
            if (equipment.weapons.Contains(_weapon))
                return;
            equipment.weapons.Add(_weapon);
            //SpecialFunctions.SetSecretText(2f, "Вы нашли " + item.itemTextName1);
            OnEquipmentChanged(new EquipmentEventArgs(null, _weapon));
            SpecialFunctions.gameUI.ConsiderItem(_weapon, _weapon.itemDescription);
        }
        else if (item is HeartClass)
        {
            Health = Mathf.Clamp(Health + ((HeartClass)item).hp, 0f, maxHealth);
        }
        else if (item is TrinketClass)
        {
            TrinketClass _trinket = (TrinketClass)item;
            if (item is MutagenClass)
            {
                MutagenClass _mutagen = (MutagenClass)item;
                if (equipment.activeTrinkets.Contains(_mutagen))
                    return;
                equipment.activeTrinkets.Add(_mutagen);
                SetMutagen(_mutagen);
            }
            else
            {
                if (equipment.trinkets.Contains(_trinket))
                    return;
                equipment.trinkets.Add(_trinket);
            }
            OnEquipmentChanged(new EquipmentEventArgs(null, _trinket));
            SpecialFunctions.gameUI.ConsiderItem(_trinket, _trinket.itemDescription);
        }
        else
        {
            if (item.itemName == "GoldHeart")
            {
                SpecialFunctions.gameUI.ConsiderItem(item, "Увеличивает максимальное количество здоровья");
                MaxHealth += 4f;
            }
            else
            {
                equipment.bag.Add(item);
                OnEquipmentChanged(new EquipmentEventArgs(null, item));
            }
            if (item.itemName == "GoldHeartShard")
            {
                List<ItemClass> goldShards = equipment.bag.FindAll(x => x.itemName == "GoldHeartShard");
                SpecialFunctions.gameUI.ConsiderItem(item, goldShards.Count.ToString() + "/5");
                if (goldShards.Count == 5)
                {
                    for (int i = 0; i < 5; i++)
                        RemoveItem(item);
                    AddItem("GoldHeart");
                }
            }
            else if (item.itemName == "LifeBookPage")
                SpecialFunctions.gameUI.ConsiderItem(item, "Увеличивает максимальное число активных особых предметов");
            else
                SpecialFunctions.SetSecretText(2f, "Вы нашли " + item.itemTextName1);
        }
    }

    /// <summary>
    /// Надеть тринкет
    /// </summary>
    public void SetTrinket(TrinketClass _trinket)
    {
        if (_trinket == null)
            return;
        foreach (TrinketClass trinket in equipment.activeTrinkets)
            if (trinket.itemName == _trinket.itemName)
                return;
        equipment.activeTrinkets.Add(_trinket);
    }

    /// <summary>
    /// Установить мутаген
    /// </summary>
    public void SetMutagen(MutagenClass mutagen)
    {
        if (mutagen == null)
            return;
        mutagenEffect = mutagen.trinketEffects[0];
    }

    /*
    /// <summary>
    /// Убрать мутаген
    /// </summary>
    public void RemoveMutagen()
    {
        mutagenEffect = null;
    }*/

    public void RemoveItem(ItemClass _item)
    {
        ItemClass rItem = equipment.bag.Find(x => x.itemName == _item.itemName);
        if (rItem != null)
        {
            equipment.bag.Remove(rItem);
            OnEquipmentChanged(new EquipmentEventArgs(null, null, rItem));
        }
    }

    /// <summary>
    /// Добавить предмет с данным названием в рюкзак
    /// </summary>
    /// <param name="id">Название предмета</param>
    public void AddItem(string itemName)
    {
        Dictionary<string, ItemClass> itemDict = SpecialFunctions.statistics.ItemDict;
        if (itemDict.ContainsKey(itemName))
            SetItem(itemDict[itemName]);
    }

    /// <summary>
    /// Убрать предмет с данным названием из рюкзака
    /// </summary>
    /// <param name="id">Название предмета</param>
    public void RemoveItem(string itemName)
    {
        Dictionary<string, ItemClass> itemDict = SpecialFunctions.statistics.ItemDict;
        if (itemDict.ContainsKey(itemName))
            RemoveItem(itemDict[itemName]);
    }

    #endregion //equipment

    /// <summary>
    /// Процесс, при котором персонаж находится в инвуле
    /// </summary>
    protected IEnumerator InvulProcess(float _invulTime,bool hitted)
    {
        HeroVisual hAnim = null;
        if ((anim is HeroVisual))
            hAnim = (HeroVisual)anim;
        if (hAnim != null && hitted)
            hAnim.InvulBlink();
        invul = true;
        yield return new WaitForSeconds(_invulTime);
        invul = false;
        //yield return new WaitForSeconds(0f);
    }

    public override bool InInvul()
    {
        return invul;
    }

    #region effects

    /// <summary>
    /// Добавить бафф
    /// </summary>
    public override void AddBuff(BuffClass _buff)
    {
        base.AddBuff(_buff);
        OnBuffAdd(new BuffEventArgs(_buff, _buff.buffName.Contains("Process")));
    }

    /// <summary>
    /// Убрать бафф
    /// </summary>
    /// <param name="_bName">Название, соответствующее баффу</param>
    public override void RemoveBuff(string _bName)
    {
        BuffClass _buff = GetBuff(_bName);
        if (_buff != null)
        {
            buffs.Remove(_buff);
            OnBuffRemove(new BuffEventArgs(_buff, _buff.buffName.Contains("Process")));
        }
    }

    /// <summary>
    /// Снять бафф с указанным именем
    /// </summary>
    protected override void StopCustomBuff(BuffData _bData)
    {
        switch (_bData.buffName)
        {
            case "StunnedProcess":
                {
                    StopStun();
                    break;
                }
            case "BurningProcess":
                {
                    StopBurning();
                    break;
                }
            case "PoisonProcess":
                {
                    StopPoison();
                    break;
                }
            case "WetProcess":
                {
                    StopWet();
                    break;
                }
            case "ColdProcess":
                {
                    StopCold();
                    break;
                }
            case "FrozenProcess":
                {
                    StopFrozen();
                    break;
                }
            case "TribalRitual":
                {
                    StopTribalRitual();
                    break;
                }
            default:
                {
                    SpecialFunctions.gameController.StopBuff(_bData);
                    break;
                }
        }
    }

    /// <summary>
    /// Получить стан
    /// </summary>
    protected override void BecomeStunned(float _time)
    {
        if (immobile)//Если на персонаже уже висит стан, то нельзя навесить ещё один
            return;
        StartCoroutine("StunnedProcess", _time == 0 ? stunTime : _time);
    }

    /// <summary>
    /// Получить поджог
    /// </summary>
    protected override void BecomeBurning(float _time)
    {
        if (GetBuff("BurningProcess") != null)
            return;
        if (GetBuff("WetProcess") != null)
            return;//Нельзя мокрого персонажа
        StopWet();//Высохнуть
        StopCold();//Согреться
        if (GetBuff("FrozenProcess") != null)
        {
            StopFrozen();//Если персонажа подожгли, когда он был заморожен, то он отмараживается и не получает никакого урона от огня, так как считаем, что всё тепло ушло на разморозку
            return;
        }
        StartCoroutine("BurningProcess", _time == 0 ? burningTime : _time);
    }

    /// <summary>
    /// Издать боевой клич
    /// </summary>
    public virtual void BattleCry()
    {
        Animate(new AnimationEventArgs("battleCry"));
    }

    /// <summary>
    /// Призвать тотемное животное
    /// </summary>
    public virtual void SummonTotemAnimal()
    {
        Instantiate(summonedAnimal, transform.position, Quaternion.identity);
        AddBuff(new BuffClass("TotemAnimal", Time.fixedTime,totemAnimalTime));
    }

    /// <summary>
    /// Вызвать эффект "Племенной ритуал"
    /// </summary>
    public virtual void StartTribalRitual(bool shortTime=true)
    {
        if (GetBuff("TribalRitual") != null)
            return;
        StartCoroutine("TribalRitualProcess", shortTime? tribalRitualTime: 10000f);
    }

    /// <summary>
    /// Процесс действия эффекта "Племенной ритуал"
    /// </summary>
    protected virtual IEnumerator TribalRitualProcess(float _time)
    {
        AddBuff(new BuffClass("TribalRitual", Time.fixedTime,_time));
        speedCoof *= tribalRitualCoof;
        yield return new WaitForSeconds(_time);
        RemoveBuff("TribalRitual");
        speedCoof /= tribalRitualCoof;
    }

    /// <summary>
    /// Остановить действие эффекта "Племенной ритуал"
    /// </summary>
    public virtual void StopTribalRitual()
    {
        if (GetBuff("TribalRitual") == null)
            return;
        RemoveBuff("TribalRitual");
        speedCoof /= tribalRitualCoof;
        StopCoroutine("TribalRitualProcess");
    }

    #endregion //effects

    #region events

    /// <summary>
    /// Событие "уровень здоровья изменился"
    /// </summary>
    protected virtual void OnHealthChanged(HealthEventArgs e)
    {
        EventHandler<HealthEventArgs> handler = healthChangedEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    /// <summary>
    /// Событие "инвентарь героя изменился"
    /// </summary>
    protected virtual void OnEquipmentChanged(EquipmentEventArgs e)
    {
        EventHandler<EquipmentEventArgs> handler = equipmentChangedEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    /// <summary>
    /// Событие "герой задыхается под водой"
    /// </summary>
    protected virtual void OnSuffocate(SuffocateEventArgs e)
    {
        EventHandler<SuffocateEventArgs> handler = suffocateEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    /// <summary>
    /// Событие "на героя подействовал бафф"
    /// </summary>
    protected virtual void OnBuffAdd(BuffEventArgs e)
    {
        EventHandler<BuffEventArgs> handler = buffAddEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    /// <summary>
    /// Событие "на героя перестал действовать бафф"
    /// </summary>
    protected virtual void OnBuffRemove(BuffEventArgs e)
    {
        EventHandler<BuffEventArgs> handler = buffRemoveEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

    #region storyActions

    /// <summary>
    /// Добавить предмет в рюкзак
    /// </summary>
    protected virtual void AddItem(StoryAction _action)
    {
        StartCoroutine(StoryAddItemProcess(_action.argument/10f, _action.id1));
    }

    /// <summary>
    /// Процесс выдачи герою предмета (Скриптовый процесс)
    /// </summary>
    protected virtual IEnumerator StoryAddItemProcess(float itemTime, string itemName)
    {
        yield return new WaitForSeconds(itemTime);
        AddItem(itemName);
    }

    /// <summary>
    /// Убрать предмет из рюкзака, если он так есть
    /// </summary>
    protected virtual void RemoveItem(StoryAction _action)
    {
        RemoveItem(_action.id1);
    }

    /// <summary>
    /// Воскреснуть
    /// </summary>
    protected virtual void Resurect(StoryAction _action)
    {
        Resurect();
    }

    /// <summary>
    /// Сформировать словарь сюжетных действий
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        storyActionBase.Add("addItem", AddItem);
        storyActionBase.Add("removeItem", RemoveItem);
        storyActionBase.Add("resurect", Resurect);
    }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public override List<string> actionNames()
    {
        List<string> _actionNames = base.actionNames();
        _actionNames.Add("addItem");
        _actionNames.Add("removeItem");
        _actionNames.Add("resurect");
        return _actionNames;
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs1()
    {
        Dictionary<string, List<string>> _actionIDs1 = base.actionIDs1();
        _actionIDs1.Add("addItem", SpecialFunctions.statistics.itemBase.ItemNames);
        _actionIDs1.Add("removeItem", SpecialFunctions.statistics.itemBase.ItemNames);
        _actionIDs1.Add("resurect", new List<string>());
        return _actionIDs1;
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs2()
    {
        Dictionary<string, List<string>> _actionIDs2 = base.actionIDs2();
        _actionIDs2.Add("addItem", new List<string>());
        _actionIDs2.Add("removeItem", new List<string>());
        _actionIDs2.Add("resurect", new List<string>());
        return _actionIDs2;
    }

    #endregion //IHaveStory

}
