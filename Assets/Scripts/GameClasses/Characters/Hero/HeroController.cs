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
    protected const float jumpTime = .2f;
    protected const float flipTime = .6f;

    protected const float invulTime = 1f;

    protected const float attackTime = .5f;
    protected const float shootTime = .65f;
    protected const float shootDistance = 15f;

    #endregion //consts

    #region fields

    protected Transform groundCheck;
    protected WallChecker wallCheck;//Индикатор, необходимый для отсутствия зависаний в стене
    protected Interactor interactor;//Индикатор, ответственный за обнаружение и взаимодействие со всеми интерактивными объектами

    [SerializeField] protected GameObject bullet; //Чем стреляет герой
    [SerializeField] protected GameObject attackParticles; //Чем атакует герой

    protected LineRenderer line;
    [SerializeField] protected Material arrowMaterial;

    #endregion //fields

    #region parametres

    public override float Health { get { return base.Health; } set{base.Health = value; OnHealthChanged(new HealthEventArgs(value));}}

    private Vector2
                    hitSize = new Vector2(.1f, .1f),
                    hitPosition = new Vector2(.1f, 0f);

    [SerializeField] protected float jumpForce = 200f,
                                     flipForce= 150f;

    [SerializeField] protected LayerMask whatIsGround, whatIsAim;

    [SerializeField] protected float attackDamage, shootDamage;//Урон, наносимый в ближнем и дальнем бою
    [SerializeField] protected float hitForce = 60f;

    protected bool jumping;
    protected bool grounded;

    protected bool invul;//Если true, то персонаж невосприимчив к урону

    protected bool immobile;//Можно ли управлять персонажем

    #endregion //parametres

    #region eventHandlers

    public EventHandler<HealthEventArgs> healthChangedEvent;

    #endregion //eventHandlers

    protected virtual void Update()
    {
        if (!immobile)
        {
            if (employment > 6)
            {
                if (Input.GetButton("Horizontal"))
                {
                    Move(Input.GetAxis("Horizontal") > 0f ? OrientationEnum.right : OrientationEnum.left);
                }
                if (Input.GetButtonDown("Jump"))
                {
                    if (grounded && !jumping)
                    {
                        rigid.AddForce(new Vector2(0f, jumpForce));
                        StartCoroutine(JumpProcess());
                    }
                }
                if (employment > 8)
                {
                    if (Input.GetButtonDown("Attack"))
                        Attack();
                    else if (Input.GetButtonDown("Shoot"))
                        Shoot();
                    else if (Input.GetButtonDown("Flip") && (rigid.velocity.x*(int)orientation>.1f)&&(grounded))
                        Flip();
                }
            }
            if (Input.GetButtonDown("Interact"))
            {
                interactor.Interact();
            }
        }

        Analyse();

        if (grounded)
        {
            Animate(new AnimationEventArgs("groundMove"));
        }
        else
        {
            Animate(new AnimationEventArgs("airMove"));
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
        wallCheck = indicators.FindChild("WallCheck").GetComponent<WallChecker>();
        interactor = indicators.FindChild("Interactor").GetComponent<Interactor>();

        immobile = false;
        jumping = false;
    }

    /// <summary>
    /// Анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, whatIsGround);
    }

    /// <summary>
    /// Процесс самого прыжка
    /// </summary>
    protected IEnumerator JumpProcess()
    {
        jumping = true;
        yield return new WaitForSeconds(jumpTime);
        jumping = false;
    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        rigid.velocity = new Vector3(wallCheck.WallInFront() ? 0f : Input.GetAxis("Horizontal") * speed, rigid.velocity.y);
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
        hitBox.SetHitBox(new HitClass(attackDamage, attackTime, hitSize, hitPosition, hitForce));
        Animate(new AnimationEventArgs("attack"));
        StartCoroutine(AttackProcess());
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
    /// Выстрелить
    /// </summary>
    protected virtual void Shoot()
    {
        StopMoving();
        RaycastHit2D hit=Physics2D.Raycast(transform.position + (int)orientation * transform.right * .1f, (int)orientation * transform.right, shootDistance, whatIsAim);
        Vector2 endPoint = transform.position + (int)orientation * transform.right * (shootDistance + .1f);
        if (hit)
        {
            IDamageable target;
            if ((target = hit.collider.gameObject.GetComponent<IDamageable>()) != null)
            {
                target.TakeDamage(shootDamage);
            }
            else
            {
                GameObject _bullet = GameObject.Instantiate(bullet, new Vector3(hit.point.x, hit.point.y, transform.position.z), transform.rotation) as GameObject;
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
        line.SetPosition(0, transform.position + (int)orientation*transform.right * .1f);
        line.SetPosition(1, new Vector3(endPoint.x, endPoint.y, transform.position.z));
        Destroy(line, .1f);
        StartCoroutine(ShootProcess());
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
        StartCoroutine(FlipProcess());
        Animate(new AnimationEventArgs("flip"));
    }

    /// <summary>
    /// Процесс кувырка
    /// </summary>
    protected virtual IEnumerator FlipProcess()
    {
        employment = Mathf.Clamp(employment - 5, 0, maxEmployment);
        yield return new WaitForSeconds(flipTime);
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage)
    {
        if (!invul)
        {
            base.TakeDamage(damage);
            StartCoroutine(InvulProcess());
        }
    }

    /// <summary>
    /// Функция, описывающая процессы при смерти персонажа
    /// </summary>
    protected override void Death()
    {
        Application.LoadLevel(Application.loadedLevel);   
    }

    /// <summary>
    /// Задать персонажу управляемость
    /// </summary>
    public void SetImmobile(bool _immobile)
    {
        immobile = _immobile;
    }

    /// <summary>
    /// Процесс, при котором персонаж находится в инвуле
    /// </summary>
    protected IEnumerator InvulProcess()
    {
        HeroVisual hAnim = (HeroVisual)anim;
        if (hAnim != null)
            hAnim.Blink();
        invul = true;
        yield return new WaitForSeconds(invulTime);
        invul = false;
    }

    #region events

    /// <summary>
    /// Событие "уровень здоровья изменился"
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnHealthChanged(HealthEventArgs e)
    {
        EventHandler<HealthEventArgs> handler = healthChangedEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

}
