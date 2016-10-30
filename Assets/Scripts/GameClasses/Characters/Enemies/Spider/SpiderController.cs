using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер паука (в данном случае джунглиевого)
/// </summary>
public class SpiderController : AIController
{

    #region consts

    protected const float attackTime = .6f, preAttackTime = .3f;

    protected const float patrolDistance = 2f;//По таким дистанциям паук будет патрулировать

    #endregion //consts

    #region fields

    protected WallChecker wallCheck, precipiceCheck;

    #endregion //fields

    #region parametres

    [SerializeField] protected float attackDistance = .2f;//На каком расстоянии должен стоять паук, чтобы решить атаковать

    protected Vector2 waypoint;//Пункт назначения, к которому стремится ИИ

    #endregion //parametres

    protected virtual void FixedUpdate()
    {
        if (!immobile)
        {
            if (agressive && mainTarget != null && employment > 2)
            {
                Vector3 targetPosition = mainTarget.transform.position;
                if (Vector2.Distance(targetPosition, transform.position) > attackDistance)
                {
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
                }
                else
                {
                    Attack();
                }
            }
            else if (!agressive)
            {
                if ((Vector2.Distance(waypoint, transform.position) < attackDistance) || (wallCheck.WallInFront() || !(precipiceCheck.WallInFront())))
                {
                    Turn((OrientationEnum)(-1 * (int)orientation));
                    Patrol();
                }
                else
                {
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(waypoint.x - transform.position.x)));
                }
            }
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
        if (indicators != null)
        {
            wallCheck = indicators.FindChild("WallCheck").GetComponent<WallChecker>();
            precipiceCheck = indicators.FindChild("PrecipiceCheck").GetComponent<WallChecker>();
        }
        Patrol();
    }

    protected override void FormDictionaries()
    {
        storyActionBase.Add("moveForward", MoveForwardAction);
    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = new Vector2((int)_orientation * speed,rigid.velocity.y);
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Определить следующую точку патрулирования
    /// </summary>
    protected virtual void Patrol()
    {
        waypoint = new Vector3((int)orientation * patrolDistance, 0f,0f) + transform.position;
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        Animate(new AnimationEventArgs("attack"));
        StartCoroutine(AttackProcess());
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        employment = Mathf.Clamp(employment - 3, 0, maxEmployment);
        yield return new WaitForSeconds(preAttackTime);
        hitBox.SetHitBox(new HitClass(damage, attackTime, attackSize, attackPosition, hitForce));
        yield return new WaitForSeconds(attackTime);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        BecomeAgressive();
    }

    #region storyActions

    /// <summary>
    /// Выйти вперёд
    /// </summary>
    public void MoveForwardAction(StoryAction _action)
    {
        Animator spAnim = GetComponent<Animator>();
        spAnim.Play("MoveForward");
        Animate(new AnimationEventArgs("moveForward"));
        StartCoroutine(MoveForwardProcess());
    }

    IEnumerator MoveForwardProcess()
    {
        yield return new WaitForSeconds(1f);
        Destroy(GetComponent<Animator>());
    }

    #endregion //storyActions

}
