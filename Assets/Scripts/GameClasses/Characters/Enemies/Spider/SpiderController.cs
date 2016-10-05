using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер паука (в данном случае джунглиевого)
/// </summary>
public class SpiderController : AIController
{

    #region consts

    protected const float attackTime = .6f, preAttackTime = .3f;

    #endregion //consts

    #region parametres

    [SerializeField] protected float attackDistance = .2f;//На каком расстоянии должен стоять паук, чтобы решить атаковать

    #endregion //parametres

    protected virtual void FixedUpdate()
    {
        if (agressive && target != null && employment>2)
        {
            Vector3 targetPosition = target.transform.position;
            if (Vector2.Distance(targetPosition, transform.position) > attackDistance)
            {
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
            }
            else
            {
                Attack();
            }
        }

        Animate(new AnimationEventArgs("groundMove"));

    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
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

}
