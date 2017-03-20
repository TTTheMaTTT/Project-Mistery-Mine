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
    protected const float deathTime = 2.1f;

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
                fightingMode = "range";
            }
            else
                fightingMode = "melee";
            OnEquipmentChanged(new EquipmentEventArgs(currentWeapon, null));
        }
    }

    protected EquipmentClass equipment;//Инвентарь игрока.
    public EquipmentClass Equipment { get { return equipment; } }
    public GameObject dropPrefab;

    [SerializeField]protected GameObject summonedAnimal;//Животное, которое может призвать на помощь герой

    #endregion //fields

    #region parametres

    public override float Health { get { return base.Health; } set { float prevHealth = health; base.Health = value; OnHealthChanged(new HealthEventArgs(value, health-prevHealth)); } }
    public float MaxHealth { get { return base.maxHealth; } set { maxHealth = value; } }
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
                                     flipForce = 150f,
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

    protected bool invul;//Если true, то персонаж невосприимчив к урону

    protected string fightingMode;
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

                    if (Input.GetButton("Horizontal"))
                    {
                        Move(Input.GetAxis("Horizontal") > 0f ? OrientationEnum.right : OrientationEnum.left);
                    }

                    if (Input.GetButtonDown("Jump"))
                    {
                        jumpInput = 0;
                        if (groundState == GroundStateEnum.grounded && !jumping)
                        {
                            Jump();
                        }
                    }

                    if (Input.GetButton("Jump"))
                    {
                        //if (jumpInput)
                        //rigid.AddForce(new Vector2(0f, jumpAdd * (underWater ? waterCoof : 1f)));
                    }

                    if (Input.GetButtonUp("Jump"))
                    {
                        jumpInput = 0;
                    }

                    if (Input.GetButtonDown("Up"))
                    {
                        LadderOn();
                    }

                    if (employment > 7)
                    {
                        if (Input.GetButtonDown("Attack"))
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
                        else if (Input.GetButtonDown("Flip"))
                            if ((rigid.velocity.x * (int)orientation > .1f) && (groundState == GroundStateEnum.grounded) && (employment > 8))
                                Flip();
                        if (Input.GetButtonDown("ChangeInteraction"))
                            interactor.ChangeInteraction();
                    }
                }

                if (currentWeapon.chargeable)
                {
                    if (Input.GetButtonUp("Attack") && chargeBeginTime>0f)
                    {
                        StopCharge();
                    }
                }
            }

            #endregion //usualMovement

            #region ladderMovement

            else
            {
                if (Input.GetButton("Vertical"))
                    LadderMove(Input.GetAxis("Vertical"));
                else
                    StopLadderMoving();
                if (Input.GetButtonDown("Jump"))
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

        equipment = new EquipmentClass();
        if (currentWeapon != null)
        {
            currentWeapon = currentWeapon.GetWeapon();//Сразу же в начале игры в это поле занесём независимую от 
                                                      //оригинала копию этого же поля, чтобы можно было производить операции с оружием, не меняя файловую структуру игры
            fightingMode = (currentWeapon is SwordClass) ? "melee" : "range";
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
                TakeDamage(Mathf.Round((fallSpeed - minDamageFallSpeed) * damagePerFallSpeed), DamageType.Physical, true, 1);
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
            if (!Physics2D.OverlapCircle(transform.position - transform.up * ladderCheckOffset, ladderStep, LayerMask.GetMask("ladder")))
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
            TakeDamage(1f, DamageType.Physical,true,0);
        }
    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        bool crouching = (groundState == GroundStateEnum.crouching);
        rigid.velocity = new Vector3((wallCheck.WallInFront) ? 0f : Input.GetAxis("Horizontal") * speed*speedCoof, rigid.velocity.y);
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
        if (fightingMode == "range" ? !((BowClass)currentWeapon).canShoot : false)
            return;
        chargeBeginTime = Time.fixedTime;
        int employmentDelta = fightingMode == "range" ? 5 : 3;
        employment = Mathf.Clamp(employment - employmentDelta, 0, maxEmployment);
        Animate(new AnimationEventArgs("holdAttack", currentWeapon.itemName, 0));
    }

    /// <summary>
    /// Закончить зарядку оружия
    /// </summary>
    protected virtual void StopCharge()
    {
        int employmentDelta = fightingMode == "range" ? 5 : 3;
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
        if (fightingMode == "melee")
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
        else if (fightingMode == "range")
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
        yield return new WaitForSeconds(sword.attackTime);
        attacking = false;
        yield return new WaitForSeconds(sword.endAttackTime);
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

        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);
    }

    /// <summary>
    /// Совершить кувырок
    /// </summary>
    protected virtual void Flip()
    {
        rigid.AddForce(new Vector2((int)orientation*flipForce, 0f));
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

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, int attackPower=0)
    {
        if (!invul)
        {
            Health = Mathf.Clamp(Health - damage, 0f, maxHealth);
            if (health <= 0f)
            {
                if (damage > 200f && _dType == DamageType.Fire)
                    Animate(new AnimationEventArgs("death", "fire", 0));
                rigid.velocity = Vector2.zero;
                rigid.isKinematic = true;
                Death();
                return;
            }
            else
                Animate(new AnimationEventArgs("hitted", "", attackPower>balance ? 0 : 1));
            bool stunned = GetBuff("StunnedProcess") != null;
            if (attackPower>balance || stunned)
            {
                //StopCoroutine("AttackProcess");
                //dontShoot = false;
                if (onLadder)
                    LadderOff();
            }
            if (attackPower>0)
                StartCoroutine(InvulProcess(invulTime, true));
            anim.Blink();
        }
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, bool ignoreInvul, int attackPower=0)
    {
        if (ignoreInvul || !invul)
        {
            Health = Mathf.Clamp(Health - damage, 0f, maxHealth);
            if (health <= 0f)
            {
                if (damage>200f && _dType==DamageType.Fire)
                    Animate(new AnimationEventArgs("death", "fire",0));
                rigid.velocity = Vector2.zero;
                rigid.isKinematic = true;
                Death();
                return;
            }
            else
                Animate(new AnimationEventArgs("hitted", "", attackPower > balance ? 0 : 1));
            SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) sprite.enabled = true;
            bool stunned = GetBuff("StunnedProcess") != null;
            if (attackPower > balance || stunned)
            {
                //StopCoroutine("AttackProcess");
                //dontShoot = false;
                if (onLadder)
                    LadderOff();
            }
            if (attackPower>0)
                StartCoroutine(InvulProcess(invulTime, true));
            anim.Blink();
        }
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
        StartCoroutine(DeathProcess());
    }

    /// <summary>
    /// Процесс смерти
    /// </summary>
    protected virtual IEnumerator DeathProcess()
    {
        Animate(new AnimationEventArgs("death"));
        immobile = true;
        SpecialFunctions.SetFade(true);
        PlayerPrefs.SetFloat("Hero Health", maxHealth);
        yield return new WaitForSeconds(deathTime);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
            SpecialFunctions.SetSecretText(2f, "Вы нашли " + item.itemTextName);
            OnEquipmentChanged(new EquipmentEventArgs(null, _weapon));
        }
        if (item is HeartClass)
        {
            Health = Mathf.Clamp(Health + ((HeartClass)item).hp, 0f, maxHealth);
        }
        else
        {
            equipment.bag.Add(item);
            SpecialFunctions.SetSecretText(2f, "Вы нашли " + item.itemTextName);
            OnEquipmentChanged(new EquipmentEventArgs(null, item));
        }
            
    }

    /// <summary>
    /// Процесс, при котором персонаж находится в инвуле
    /// </summary>
    protected IEnumerator InvulProcess(float _invulTime,bool hitted)
    {
        HeroVisual hAnim = (HeroVisual)anim;
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
    public virtual void StartTribalRitual()
    {
        if (GetBuff("TribalRitual") != null)
            return;
        StartCoroutine("TribalRitualProcess");
    }

    /// <summary>
    /// Процесс действия эффекта "Племенной ритуал"
    /// </summary>
    protected virtual IEnumerator TribalRitualProcess()
    {
        AddBuff(new BuffClass("TribalRitual", Time.fixedTime,tribalRitualTime));
        speedCoof *= tribalRitualCoof;
        yield return new WaitForSeconds(tribalRitualTime);
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

}
