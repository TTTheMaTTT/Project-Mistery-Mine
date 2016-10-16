using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// Контроллер, управляющий ГГ
/// </summary>
public class HeroController : CharacterController
{

    #region consts

    protected const float groundRadius = .01f;
    protected const float jumpTime = .2f, jumpInputTime=.2f ;
    protected const float flipTime = .6f;

    protected const float invulTime = 1f;

    protected const float attackTime = .5f;
    protected const float shootTime = .65f;
    protected const float shootDistance = 15f;

    protected const float ladderCheckOffset = .05f, ladderStep = .01f;

    protected const float minDamageFallSpeed = 5.5f;//Минимальная скорость по оси y, которая должна быть при падении, чтобы засчитался урон
    protected const float damagePerFallSpeed = 2f;

    protected const float suffocateTime = .3f;//Сколько времени должно пройти, чтобы запас воздуха уменьшился на 1 или здоровье ГГ на .5
    protected const int maxAirSupply = 10;

    #endregion //consts

    #region fields

    protected Transform groundCheck;
    protected Transform waterCheck;
    protected Transform wallAboveCheck;//Индикатор того, что над персонажем располагается твёрдое тело, земля
    protected WallChecker wallCheck;//Индикатор, необходимый для отсутствия зависаний в стене
    protected Interactor interactor;//Индикатор, ответственный за обнаружение и взаимодействие со всеми интерактивными объектами

    protected Collider2D col1,col2;

    [SerializeField] protected GameObject attackParticles; //Чем атакует герой

    protected LineRenderer line;
    [SerializeField] protected Material arrowMaterial;

    [SerializeField]
    protected WeaponClass currentWeapon;//Оружие, которое используется персонажем в данный момент
    public GameObject dropPrefab;

    #endregion //fields

    #region parametres

    public override float Health { get { return base.Health; } set{base.Health = value; OnHealthChanged(new HealthEventArgs(value));}}

    protected int airSupply = 10;//Запас воздуха
    public int AirSupply { get { return airSupply; }  set { airSupply = value; OnSuffocate(new SuffocateEventArgs(airSupply)); } }

    [SerializeField] protected float jumpForce = 200f,
                                     jumpAdd = 20f,//Добавление к силе прыжка при зажимании
                                     flipForce= 150f,
                                     ladderSpeed=.8f,
                                     waterCoof=.7f;

    [SerializeField] protected LayerMask whatIsGround, whatIsAim;

    protected bool jumping, jumpInput=false;
    protected GroundStateEnum groundState;
    protected float fallSpeed=0f;
    protected bool onLadder;
    protected bool underWater;

    protected bool invul;//Если true, то персонаж невосприимчив к урону

    protected bool immobile;//Можно ли управлять персонажем

    protected string fightingMode;

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
                        if (groundState==GroundStateEnum.grounded && !jumping)
                        {
                            rigid.AddForce(new Vector2(0f, jumpForce * (underWater ? waterCoof : 1f)));
                            StartCoroutine(JumpProcess());
                        }
                    }

                    if (Input.GetButton("Jump"))
                    {
                        if (jumpInput)
                            rigid.AddForce(new Vector2(0f, jumpAdd * (underWater ? waterCoof : 1f)));
                    }

                    if (Input.GetButtonDown("Up"))
                    {
                        LadderOn();
                    }

                    if (employment > 8)
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
                        else if (Input.GetButtonDown("Flip") && (rigid.velocity.x * (int)orientation > .1f) && (groundState==GroundStateEnum.grounded))
                            Flip();
                    }
                }
            }

            #endregion //usualMovement

            #region ladderMovement

            else
            {
                if (Input.GetButton("Vertical"))
                {
                    LadderMove();
                }
                else
                {
                    StopLadderMoving();
                }
                if (Input.GetButtonDown("Jump"))
                {
                    LadderOff();
                }
            }

            #endregion //ladderMovement

        }

        Analyse();

        if (onLadder)
        {
            Animate(new AnimationEventArgs("ladderMove"));
        }
        else if (groundState==GroundStateEnum.inAir)
        {
            Animate(new AnimationEventArgs("airMove"));
        }
        else
        {
            Animate(new AnimationEventArgs("groundMove"));
        }
	}

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
        Transform indicators = transform.FindChild("Indicators");
        groundCheck = indicators.FindChild("GroundCheck");
        waterCheck = indicators.FindChild("WaterCheck");
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

        Collider2D[] cols = new Collider2D[2];
        cols = GetComponents<Collider2D>();
        col1 = cols[0]; col2 = cols[1];
        col2.enabled = false;

    }

    /// <summary>
    /// Анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        if (Physics2D.OverlapCircle(groundCheck.position, groundRadius, whatIsGround))
            groundState = GroundStateEnum.grounded;
        else
            groundState = GroundStateEnum.inAir;

        if ((groundState==GroundStateEnum.grounded))
        {
            bool crouching=false;
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
                TakeDamage(Mathf.Round((fallSpeed - minDamageFallSpeed) * damagePerFallSpeed));
            }
            fallSpeed = 0f;
        }
        else
        {
            Crouch(false);
            fallSpeed = -rigid.velocity.y;
        }

        bool _underWater=Physics2D.OverlapCircle(waterCheck.position, groundRadius, LayerMask.GetMask("Water"));
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
        }

        if (onLadder)
        {
            if (!Physics2D.OverlapCircle(transform.position - transform.up * ladderCheckOffset, ladderStep, LayerMask.GetMask("ladder")))
                LadderOff();
        }

    }

    /// <summary>
    /// Процесс самого прыжка
    /// </summary>
    protected IEnumerator JumpProcess()
    {
        employment = Mathf.Clamp(employment - 2, 0, maxEmployment);
        jumpInput = true;
        yield return new WaitForSeconds(jumpInputTime);
        jumpInput = false;
        jumping = true;
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
    }

    /// <summary>
    /// Процесс задыхания под вод
    /// </summary>
    protected IEnumerator SuffocateProcess()
    {
        //Сначала заканчивается запас здоровья
        int _airSupply = airSupply;
        for (int i =0;i< _airSupply;i++)
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
        rigid.velocity = new Vector3((wallCheck.WallInFront() && !crouching) ? 0f : Input.GetAxis("Horizontal") * currentSpeed, rigid.velocity.y);
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
    protected virtual void LadderOn()
    {
        if (interactor.Ladder != null)
        {
            if (orientation == OrientationEnum.left)
            {
                Turn(OrientationEnum.right);
            }
            onLadder = true;
            rigid.velocity = Vector3.zero;
            rigid.gravityScale = 0f;
            Animate(new AnimationEventArgs("setLadderMove", "", 1));
            Vector3 vect = transform.position;
            transform.position = new Vector3(interactor.Ladder.transform.position.x, vect.y, vect.z);
        }
    }

    /// <summary>
    /// Слезть с лестницы
    /// </summary>
    protected virtual void LadderOff()
    {
        onLadder = false;
        rigid.gravityScale = 1f;
        Animate(new AnimationEventArgs("setLadderMove", "", 0));
            rigid.AddForce(new Vector2(0f, jumpForce / 2));
            StartCoroutine(JumpProcess());
        //if (Input.GetAxis("Vertical")>0f)
        //{
        //  rigid.AddForce(new Vector2(0f, jumpForce / 2));
        //  StartCoroutine(JumpProcess());
        //}
    }

    /// <summary>
    /// Перемещение по лестнице
    /// </summary>
    protected virtual void LadderMove()
    {
        float value = Input.GetAxis("Vertical");
        rigid.velocity = new Vector3(0f, 
                                     Physics2D.OverlapCircle(transform.position + Mathf.Sign(value) * transform.up * ladderCheckOffset, ladderStep, LayerMask.GetMask("ladder")) ? value * ladderSpeed : 0f);
    }

    /// <summary>
    /// Прекратить передвижение по лестнице
    /// </summary>
    protected virtual void StopLadderMoving()
    {
        rigid.velocity = Vector2.zero;
    }

    protected override void Turn(OrientationEnum _orientation)
    {
        if (employment == 10)
        {
            base.Turn(_orientation);
        }
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        if (fightingMode == "melee")
        {
            SwordClass sword = (SwordClass)currentWeapon;
            hitBox.SetHitBox(new HitClass(sword.damage, attackTime, sword.attackSize, sword.attackPosition, sword.attackForce));
            Animate(new AnimationEventArgs("attack"));
            StartCoroutine(AttackProcess());
        }
        else if (fightingMode == "range")
        {
            BowClass bow = (BowClass)currentWeapon;
            StopMoving();
            RaycastHit2D hit = Physics2D.Raycast(transform.position + (int)orientation * transform.right * .1f, (int)orientation * transform.right, shootDistance, whatIsAim);
            Vector2 endPoint = transform.position + (int)orientation * transform.right * (shootDistance + .1f);
            if (hit)
            {
                IDamageable target;
                if ((target = hit.collider.gameObject.GetComponent<IDamageable>()) != null)
                {
                    target.TakeDamage(bow.damage);
                }
                else
                {
                    GameObject _bullet = GameObject.Instantiate(bow.arrow, new Vector3(hit.point.x, hit.point.y, transform.position.z), transform.rotation) as GameObject;
                    Vector3 vect = _bullet.transform.localScale;
                    _bullet.transform.localScale = new Vector3((int)orientation * vect.x, vect.y, vect.z);
                }
                endPoint = hit.point;
            }
            Animate(new AnimationEventArgs("shoot"));
            line = gameObject.AddComponent<LineRenderer>();
            line.material = arrowMaterial;
            line.SetWidth(.02f, .02f);
            line.SetVertexCount(2);
            line.SetPosition(0, transform.position + (int)orientation * transform.right * .1f);
            line.SetPosition(1, new Vector3(endPoint.x, endPoint.y, transform.position.z));
            Destroy(line, .1f);
            StartCoroutine(ShootProcess());
        }
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        GameObject _attackParticles = Instantiate(attackParticles, hitBox.transform.position, hitBox.transform.rotation) as GameObject;
        _attackParticles.transform.parent = transform;
        Destroy(_attackParticles, attackTime);
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(attackTime);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Процесс выстрела
    /// </summary>
    protected virtual IEnumerator ShootProcess()
    {
        employment = Mathf.Clamp(employment - 5, 0, maxEmployment);
        yield return new WaitForSeconds(shootTime);
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
        StartCoroutine(InvulProcess(invulTime, true));
    }

    /// <summary>
    /// Функция, описывающая процессы при смерти персонажа
    /// </summary>
    protected override void Death()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);   
    }

    /// <summary>
    /// Задать персонажу управляемость
    /// </summary>
    public void SetImmobile(bool _immobile)
    {
        immobile = _immobile;
    }

    /// <summary>
    /// Добавить предмет в инвентарь
    /// </summary>
    public void SetItem(ItemClass item)
    {
        if (item is WeaponClass)
        {
            GameObject drop = Instantiate(dropPrefab, transform.position, transform.rotation) as GameObject;
            drop.GetComponent<DropClass>().item = currentWeapon;
            currentWeapon = (WeaponClass)item;
            fightingMode = (currentWeapon is SwordClass) ? "melee" : "range";
            OnEquipmentChanged(new EquipmentEventArgs(currentWeapon));
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
