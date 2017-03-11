using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Контроллер гигантской летучей мыши
/// </summary>
public class BatBossController: BossController
{
    #region consts

    protected const float pushBackForce = 500f;
    private const float batSize = .5f;

    private const float maxAvoidDistance = 30f, avoidOffset = 1.5f;

    #endregion //consts

    #region fields

    protected Hearing hearing;//Слух персонажа

    public GameObject drop;//Что выпадает из летучей мыши, если её 2 раза ударить

    #endregion //fields

    #region parametres

    protected int damageCount = 0;//Подсчёт кол-ва нанесения урона

    [SerializeField]
    protected float healthDrain = 10f;//Сколько летучая мышь восстанавливает себе здоровья при укусе

    #endregion //parametres

    protected override void Update()
    {
        base.Update();
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
        rigid.gravityScale = 0f;
        rigid.isKinematic = true;

        hitBox.AttackEventHandler += HandleAttackProcess;
        hearing = indicators.GetComponentInChildren<Hearing>();
        hearing.hearingEventHandler += HandleHearingEvent;
        hearing.AllyHearing = false;

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionIn = NullAreaFunction;
            areaTrigger.triggerFunctionOut = NullAreaFunction;
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
            areaTrigger.InitializeAreaTrigger();
        }
        

        BecomeCalm();

    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = (currentTarget - transform.position).normalized * speed;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Функция, ответственная за анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        Vector2 pos = transform.position;
        if (rigid.velocity.magnitude < minSpeed)
        {
            float angle = 0f;
            Vector2 rayDirection;
            for (int i = 0; i < 8; i++)
            {
                rayDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (Physics2D.Raycast(pos, rayDirection, batSize, LayerMask.GetMask(gLName)))
                {
                    rigid.AddForce(-rayDirection * pushBackForce / 2f);
                    break;
                }
                angle += Mathf.PI / 4f;
            }
        }
        if (behavior == BehaviorEnum.agressive)
        {
            if (currentTarget.exists)
            {
                if (currentTarget != mainTarget)
                {
                    Vector2 direction = (mainTarget - pos).normalized;
                    RaycastHit2D hit = Physics2D.Raycast(pos + direction.normalized * batSize, direction, sightRadius);
                    if (hit)
                        if (hit.collider.transform == mainTarget.transform)
                            currentTarget = mainTarget;
                }
                if (Physics2D.Raycast(pos, currentTarget - pos, batSize, LayerMask.GetMask(gLName)))
                {
                    currentTarget = FindPath();
                }
            }
        }
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        hitBox.SetHitBox(attackParametres);
        rigid.isKinematic = false;
        hearing.enabled = false;
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        hitBox.ResetHitBox();
        rigid.isKinematic = true;
        hearing.enabled = true;
    }

    /// <summary>
    /// Перейти в состояние патрулирования
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        hitBox.ResetHitBox();
        rigid.isKinematic = false;
        hearing.enabled = true;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, bool _microstun=true)
    {
        if (_dType != DamageType.Physical)
        {
            if (((DamageType)vulnerability & _dType) == _dType)
                damage *= 1.25f;
            else if (_dType == attackParametres.damageType)
                damage *= .9f;//Если урон совпадает с типом атаки персонажа, то он ослабевается (бить огонь огнём - не самая гениальная затея)
        }
        Health = Mathf.Clamp(Health - damage, 0f, maxHealth);
        if (health <= 0f)
            Death();
        else
            Animate(new AnimationEventArgs("hitted"));
        if (behavior != BehaviorEnum.agressive && _microstun)
        {
            BecomeAgressive();
            damageCount++;
        }
        if (damageCount >= 2)
        {
            Instantiate(drop, transform.position, transform.rotation);
            damageCount = 0;
        }
    }

    /// <summary>
    /// Выдвинуться к целевой позиции
    /// </summary>
    /// <param name="targetPosition">Целевая позиция</param>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        BecomePatrolling();
        if (currentTarget.exists)
        {
            currentTarget = new ETarget( targetPosition);
        }
    }

    /// <summary>
    /// Простейший алгоритм обхода препятствий
    /// </summary>
    protected ETarget FindPath()
    {
        Vector2 pos = transform.position;

        bool a1 = Physics2D.Raycast(pos, Vector2.up, batSize, LayerMask.GetMask(gLName)) && (mainTarget.y - pos.y > avoidOffset);
        bool a2 = Physics2D.Raycast(pos, Vector2.right, batSize, LayerMask.GetMask(gLName)) && (mainTarget.x > pos.x);
        bool a3 = Physics2D.Raycast(pos, Vector2.down, batSize, LayerMask.GetMask(gLName)) && (mainTarget.y - pos.y < avoidOffset);
        bool a4 = Physics2D.Raycast(pos, Vector2.left, batSize, LayerMask.GetMask(gLName)   ) && (mainTarget.x < pos.x);

        bool open1 = false, open2 = false;
        Vector2 aimDirection = a1 ? Vector2.up : a2 ? Vector2.right : a3 ? Vector2.down : a4 ? Vector2.left : Vector2.zero;
        if (aimDirection == Vector2.zero)
            return mainTarget;
        else
        {
            Vector2 vect1 = new Vector2(aimDirection.y, aimDirection.x);
            Vector2 vect2 = new Vector2(-aimDirection.y, -aimDirection.x);
            Vector2 pos1 = pos;
            Vector2 pos2 = pos1;
            while (Physics2D.Raycast(pos1, aimDirection, batSize, LayerMask.GetMask(gLName)) && ((pos1 - pos).magnitude < maxAvoidDistance))
                pos1 += vect1 * batSize;
            open1 = !Physics2D.Raycast(pos1, aimDirection, batSize, LayerMask.GetMask(gLName));
            while (Physics2D.Raycast(pos2, aimDirection, batSize, LayerMask.GetMask(gLName)) && ((pos2 - pos).magnitude < maxAvoidDistance))
                pos2 += vect2 * batSize;
            open2 = !Physics2D.Raycast(pos2, aimDirection, batSize, LayerMask.GetMask(gLName));
            Vector2 newTargetPosition = (open1 && !open2) ? pos1 : (open2 && !open1) ? pos2 : ((mainTarget - pos1).magnitude < (mainTarget - pos2).magnitude) ? pos1 : pos2;
            return new ETarget(newTargetPosition);
        }
    }

    #region behaviourActions

    /// <summary>
    /// Спокойное поведение
    /// </summary>
    protected override void CalmBehavior()
    {
        if (!immobile)
        {
            if (rigid.velocity.magnitude < minSpeed)
            {
                Animate(new AnimationEventArgs("idle"));
            }
            else
            {
                Animate(new AnimationEventArgs("fly"));
            }
        }
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        if (!immobile)
        {
            Vector2 pos = transform.position;
            if ( mainTarget.exists)
            {
                if (currentTarget.exists)
                {
                    Vector3 targetPosition = currentTarget;
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
                    if (currentTarget != mainTarget && Vector3.SqrMagnitude(currentTarget - pos) < batSize * batSize)
                        currentTarget = FindPath();
                }
            }

            if ( rigid.velocity.magnitude < minSpeed)
            {
                Animate(new AnimationEventArgs("idle"));
            }
            else
            {
                Animate(new AnimationEventArgs("fly"));
            }
        }
    }

    /// <summary>
    /// Поведение преследования какой-либо цели
    /// </summary>
    protected override void PatrolBehavior()
    {
        base.PatrolBehavior();

        Vector2 targetPosition = currentTarget;
        Vector2 pos = transform.position;
        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - pos.x)));
        if (Vector2.SqrMagnitude(currentTarget - pos) < minDistance * minDistance)
        {
            StopMoving();
            if (Vector2.SqrMagnitude(targetPosition - beginPosition) > minDistance * minDistance)
                GoHome();
            else
                BecomeCalm();
        }
        Animate(new AnimationEventArgs("fly"));
    }

    #endregion //behaviourActions

    #region events

    /*
    /// <summary>
    /// Обработка события "Услышал врага"
    /// </summary>
    protected virtual void HandleHearingEvent(object sender, EventArgs e)
    {
        BecomeAgressive();
    }
    */

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected override void HandleAttackProcess(object sender, HitEventArgs e)
    {
        if (e.HPDif < 0f)
        {
            Health = Mathf.Clamp(health + healthDrain, 0f, maxHealth);
        }
    }

    #endregion //events
}