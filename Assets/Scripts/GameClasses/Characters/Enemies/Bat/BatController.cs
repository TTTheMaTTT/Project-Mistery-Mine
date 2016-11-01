using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Скрипт, реализующий поведение летучей мыши
/// </summary>
public class BatController : AIController
{

    #region consts

    protected const float minSpeed = .1f;

    protected const float pushBackForce = 100f;
    private const float batSize = .2f;

    private const float maxAvoidDistance = 10f, avoideOffset = .5f;

    #endregion //consts

    #region fields

    protected Hearing hearing;//Слух персонажа

    public LayerMask whatIsGround = LayerMask.GetMask("ground");

    #endregion //fields


    protected virtual void FixedUpdate()
    {
        if (!immobile)
        {
            if (agressive && mainTarget != null)
            {
                if (currentTarget != null)
                {
                    Vector3 targetPosition = currentTarget.transform.position;
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
                    if (currentTarget != mainTarget && Vector3.Distance(currentTarget.transform.position, transform.position) < batSize)
                    {
                        DestroyImmediate(currentTarget);
                        currentTarget = FindPath();
                    }

                }
                if (currentTarget != mainTarget)
                {
                    Vector2 vect = mainTarget.transform.position - transform.position;
                    RaycastHit2D hit = Physics2D.Raycast(transform.position + new Vector3(vect.x, vect.y, 0f).normalized * batSize, vect, sightRadius);
                    if (hit)
                        if (hit.collider.gameObject == mainTarget)
                            currentTarget = mainTarget;
                }
                if (Physics2D.Raycast(transform.position, currentTarget.transform.position - transform.position, batSize, whatIsGround))
                {
                    currentTarget = FindPath();
                }
                Analyse();
            }

            if (!agressive && rigid.velocity.magnitude < minSpeed)
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
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();
        rigid.gravityScale = 0f;
        rigid.isKinematic = true;

        hitBox.AttackEventHandler += HandleAttackProcess;
        Transform indicators = transform.FindChild("Indicators");
        hearing = indicators.GetComponentInChildren<Hearing>();
        hearing.hearingEventHandler += HandleHearingEvent;

    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = (currentTarget.transform.position - transform.position).normalized * speed;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity,Time.fixedDeltaTime*acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    protected override void Analyse()
    {
        base.Analyse();
        if (rigid.velocity.magnitude < minSpeed)
        {
            float angle = 0f;
            Vector2 rayDirection;
            for (int i = 0; i < 8; i++)
            {
                rayDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (Physics2D.Raycast(transform.position, rayDirection, batSize, whatIsGround))
                {
                    rigid.AddForce(-rayDirection * pushBackForce / 2f);
                    break;
                }
                angle += Mathf.PI / 4f;
            }
        }
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        hitBox.SetHitBox(new HitClass(damage,-1f,attackSize,attackPosition,0f));
        rigid.isKinematic = false;
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        hitBox.ResetHitBox();
        rigid.isKinematic = true;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        BecomeAgressive();
    }

    /// <summary>
    /// Простейший алгоритм обхода препятствий
    /// </summary>
    protected GameObject FindPath()
    {
        if (currentTarget!=null? currentTarget != mainTarget:true)
            DestroyObject(currentTarget);

        bool a1 = Physics2D.Raycast(transform.position, Vector2.up, batSize, whatIsGround) && (mainTarget.transform.position.y- transform.position.y >avoideOffset);
        bool a2 = Physics2D.Raycast(transform.position, Vector2.right, batSize, whatIsGround) && (mainTarget.transform.position.x > transform.position.x);
        bool a3 = Physics2D.Raycast(transform.position, Vector2.down, batSize, whatIsGround) && (mainTarget.transform.position.y - transform.position.y < avoideOffset );
        bool a4 = Physics2D.Raycast(transform.position, Vector2.left, batSize, whatIsGround) && (mainTarget.transform.position.x < transform.position.x);

        bool open1=false, open2=false;
        Vector2 aimDirection = a1 ? Vector2.up : a2 ? Vector2.right : a3 ? Vector2.down : a4 ? Vector2.left : Vector2.zero;
        if (aimDirection == Vector2.zero)
            return mainTarget;
        else
        {
            Vector2 vect1 = new Vector2(aimDirection.y, aimDirection.x);
            Vector2 vect2 = new Vector2(-aimDirection.y, -aimDirection.x);
            Vector2 vect = new Vector2(transform.position.x, transform.position.y);
            Vector2 pos1 = vect;
            Vector2 pos2 =pos1;
            while (Physics2D.Raycast(pos1, aimDirection, batSize, whatIsGround) && ((pos1-vect).magnitude<maxAvoidDistance))
                pos1 += vect1 * batSize;
            open1 = !Physics2D.Raycast(pos1, aimDirection, batSize, whatIsGround);
            while (Physics2D.Raycast(pos2, aimDirection, batSize, whatIsGround) && ((pos2 - vect).magnitude < maxAvoidDistance))
                pos2 += vect2 * batSize;
            open2 = !Physics2D.Raycast(pos2, aimDirection, batSize, whatIsGround);
            Vector2 targetPosition = new Vector2(mainTarget.transform.position.x, mainTarget.transform.position.y);
            Vector2 newTargetPosition=(open1 && !open2)? pos1 :(open2 && !open1)? pos2 : ((targetPosition-pos1).magnitude<(targetPosition-pos2).magnitude)? pos1 :pos2;
            GameObject point = new GameObject("point");
            point.transform.position = newTargetPosition;
            return point;
        }
        return mainTarget;
    }
    
    #region events

    /// <summary>
    /// Обработка события "Услышал врага"
    /// </summary>
    protected virtual void HandleHearingEvent(object sender, EventArgs e)
    {
        BecomeAgressive();
    }

    /// <summary>
    ///  Обработка события "произошла атака"
    /// </summary>
    protected void HandleAttackProcess(object sender, HitEventArgs e)
    {
        rigid.velocity = Vector2.zero;
        rigid.AddForce((transform.position - mainTarget.transform.position).normalized * pushBackForce);//При столкновении с врагом летучая мышь отталкивается назад
    }

    #endregion //events

}
