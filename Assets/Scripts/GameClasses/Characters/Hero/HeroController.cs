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

    protected const float ladderCheckOffset = .05f, ladderStep = .01f;

    protected const float minDamageFallSpeed = 4.2f;//Минимальная скорость по оси y, которая должна быть при падении, чтобы засчитался урон
    protected const float damagePerFallSpeed = 2f;

    protected const float suffocateTime = .3f;//Сколько времени должно пройти, чтобы запас воздуха уменьшился на 1 или здоровье ГГ на .5
    protected const int maxAirSupply = 10;

    protected const float highWallCheckPosition = 0.04f, lowWallCheckPosition = 0f;
    protected const float highWallCheckSize = .08f, lowWallCheckSize = .05f;

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
    public WeaponClass CurrentWeapon { get { return currentWeapon; } set { currentWeapon = value; OnEquipmentChanged(new EquipmentEventArgs(currentWeapon)); } }

    protected List<ItemClass> bag = new List<ItemClass>();//Рюкзак игрока.
    public List<ItemClass> Bag { get { return bag; } }
    public GameObject dropPrefab;

    #endregion //fields

    #region parametres

    public override float Health { get { return base.Health; } set { base.Health = value; OnHealthChanged(new HealthEventArgs(value)); } }
    public float MaxHealth { get { return base.maxHealth; } }

    protected int airSupply = 10;//Запас воздуха
    public int AirSupply { get { return airSupply; } set { airSupply = value; OnSuffocate(new SuffocateEventArgs(airSupply)); } }

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
    public bool OnLadder { get { return onLadder; } }
    protected bool underWater;
    protected bool dontShoot = false;

    protected bool invul;//Если true, то персонаж невосприимчив к урону

    protected string fightingMode;

    public bool attacking = false;

    #endregion //parametres

    #region eventHandlers

    public EventHandler<EquipmentEventArgs> equipmentChangedEvent;
    public EventHandler<HealthEventArgs> healthChangedEvent;
    public EventHandler<SuffocateEventArgs> suffocateEvent;

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
                                    Attack();
                            }
                        }
                        else if (Input.GetButtonDown("Flip"))
                            if ((rigid.velocity.x * (int)orientation > .1f) && (groundState == GroundStateEnum.grounded) && (employment > 8))
                                Flip();
                        if (Input.GetButtonDown("ChangeInteraction"))
                            interactor.ChangeInteraction();
                    }
                }
            }

            #endregion //usualMovement

            #region ladderMovement

            else
            {
                if (Input.GetButton("Vertical"))
                {
                    LadderMove(Input.GetAxis("Vertical"));
                }
                else
                {
                    StopLadderMoving();
                }
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
        wallCheck = indicators.FindChild("WallCheck").GetComponent<WallChecker>();
        wallAboveCheck = indicators.FindChild("WallAboveCheck");
        interactor = indicators.FindChild("Interactor").GetComponent<Interactor>();

        immobile = false;
        jumping = false;
        onLadder = false;

        if (currentWeapon != null)
        {
            fightingMode = (currentWeapon is SwordClass) ? "melee" : "range";
        }
        bag = new List<ItemClass>();

        Collider2D[] cols = new Collider2D[2];
        cols = GetComponents<Collider2D>();
        col1 = cols[0]; col2 = cols[1];
        col2.enabled = false;

        if (!PlayerPrefs.HasKey("Hero Health"))//Здоровье не восполняется при переходе на следующий уровень. Поэтому, его удобно сохранять в PlayerPrefs
            PlayerPrefs.SetFloat("Hero Health", maxHealth);
        Health = PlayerPrefs.GetFloat("Hero Health");
    }

    /// <summary>
    /// Анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        if (groundCheck.WallInFront)
            groundState = GroundStateEnum.grounded;
        else
            groundState = GroundStateEnum.inAir;

        if ((groundState == GroundStateEnum.grounded))
        {
            bool crouching = false;
            if (Physics2D.OverlapCircle(wallAboveCheck.position, groundRadius, whatIsGround) || Input.GetButton("Flip"))
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
                TakeDamage(Mathf.Round((fallSpeed - minDamageFallSpeed) * damagePerFallSpeed), true);
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
            underWater = _underWater;
            WaterIndicator waterIndicator = waterCheck.GetComponent<WaterIndicator>();
            if (_underWater)
            {
                rigid.gravityScale = .6f;
                waterIndicator.StartCoroutine(SuffocateProcess());
            }
            else
            {
                rigid.gravityScale = 1f;
                waterIndicator.StopAllCoroutines();
                AirSupply = maxAirSupply;
            }
            Animate(new AnimationEventArgs("waterSplash"));
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
    protected IEnumerator JumpProcess()
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
        //Сначала заканчивается запас здоровья
        int _airSupply = airSupply;
        for (int i = 0; i < _airSupply; i++)
        {
            yield return new WaitForSeconds(suffocateTime);
            AirSupply--;
        }
        //А потом и жизнь персонажа
        while (true)
        {
            yield return new WaitForSeconds(suffocateTime);
            TakeDamage(1f);
        }
    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        bool crouching = (groundState == GroundStateEnum.crouching);
        float currentSpeed = speed * ((underWater || crouching) ? waterCoof : 1f);
        rigid.velocity = new Vector3((wallCheck.WallInFront) ? 0f : Input.GetAxis("Horizontal") * currentSpeed, rigid.velocity.y);
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
        rigid.gravityScale = 1f;
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
                                     Physics2D.OverlapCircle(transform.position + Mathf.Sign(direction) * transform.up * ladderCheckOffset, ladderStep, LayerMask.GetMask("ladder")) ? direction * ladderSpeed : 0f);
    }

    /// <summary>
    /// Развернуться
    /// </summary>
    /// <param name="_orientation">В какую сторону должен смотреть персонаж после поворота</param>
    protected override void Turn(OrientationEnum _orientation)
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

    protected override void Jump()
    {
        jumping = true;
        rigid.AddForce(new Vector2(0f, jumpForce * (underWater ? waterCoof : 1f)));
        StartCoroutine(JumpProcess());
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        if (fightingMode == "melee")
        {
            Animate(new AnimationEventArgs("attack", currentWeapon.itemName, Mathf.RoundToInt(10 * (currentWeapon.preAttackTime + currentWeapon.attackTime+currentWeapon.endAttackTime))));
            StartCoroutine(AttackProcess());
        }
        else if (fightingMode == "range")
        {
            if (((BowClass)currentWeapon).canShoot)
            {
                StopMoving();
                Animate(new AnimationEventArgs("shoot", currentWeapon.name, Mathf.RoundToInt(10 * (currentWeapon.preAttackTime + currentWeapon.attackTime))));
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
        yield return new WaitForSeconds(sword.preAttackTime);
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
        yield return new WaitForSeconds(currentWeapon.preAttackTime);
        bow.Shoot(hitBox, transform.position+Vector3.down*0.035f + Vector3.right*(int)orientation*.05f, (int)orientation, whatIsAim, enemies);
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
    public override void TakeDamage(float damage)
    {
        if (!invul)
        {
            base.TakeDamage(damage);
            dontShoot = false;
            LadderOff();
            StartCoroutine(InvulProcess(invulTime, true));
        }
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage, bool ignoreInvul)
    {
        base.TakeDamage(damage, ignoreInvul);
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) sprite.enabled = true;
        dontShoot = false;
        LadderOff();
        StartCoroutine(InvulProcess(invulTime, true));
    }

    /// <summary>
    /// Функция, описывающая процессы при смерти персонажа
    /// </summary>
    protected override void Death()
    {
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
    /// Добавить предмет в инвентарь
    /// </summary>
    public void SetItem(ItemClass item, bool withoutDrop)
    {
        if (item is WeaponClass)
        {
            if (!withoutDrop)
            {
                GameObject drop = Instantiate(dropPrefab, transform.position, transform.rotation) as GameObject;
                drop.GetComponent<DropClass>().item = currentWeapon;
            }
            currentWeapon = (WeaponClass)item;
            if (currentWeapon is BowClass)
            {
                ((BowClass)currentWeapon).ReloadWeapon();
                fightingMode = "range";
            }
            else
             fightingMode ="melee";
            OnEquipmentChanged(new EquipmentEventArgs(currentWeapon));
        }
        else if (item is HeartClass)
        {
            Health = Mathf.Clamp(Health + ((HeartClass)item).hp, 0f, maxHealth);
        }
        else
        {
            bag.Add(item);
        }
            
    }

    /// <summary>
    /// Процесс, при котором персонаж находится в инвуле
    /// </summary>
    protected IEnumerator InvulProcess(float _invulTime,bool hitted)
    {
        HeroVisual hAnim = (HeroVisual)anim;
        if (hAnim != null && hitted)
            hAnim.Blink();
        invul = true;
        yield return new WaitForSeconds(_invulTime);
        invul = false;
    }

    public override bool InInvul()
    {
        return invul;
    }

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

    protected virtual void OnEquipmentChanged(EquipmentEventArgs e)
    {
        EventHandler<EquipmentEventArgs> handler = equipmentChangedEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    protected virtual void OnSuffocate(SuffocateEventArgs e)
    {
        EventHandler<SuffocateEventArgs> handler = suffocateEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

}
