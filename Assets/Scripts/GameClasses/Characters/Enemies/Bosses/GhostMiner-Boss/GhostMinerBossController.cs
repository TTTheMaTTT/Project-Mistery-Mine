using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер босса призраков-шахтёров
/// </summary>
public class GhostMinerBossController : BossController
{

    #region consts

    protected const float sightOffsetX = .2f, sightOffsetY = .1f;//Насколько сдвинут прицел босса
    protected const float diveDownOffset = 1.5f;//Насколько глубоко босс уходит под землю?

    protected const float attackTime = .6f, preAttackTime = .6f, preCritAttackTime = .9f;//Тайминги атак
    protected const float attackRate = 3f;//Сколько секунд проходит между атаками

    protected const float divingHealthFold = 50f;//Какому числу должно быть кратно здоровье, чтобы босс начал совершать особую атаку

    protected const float critPosibility = .25f;//Вероятность критической атаки

    #endregion //consts

    #region fields

    [SerializeField]protected GameObject coal;//уголь, которым стреляет босс
    [SerializeField]protected GameObject drop;//что скидывается с босса после смерти
    protected PolygonCollider2D col;//Коллайдер персонажа
    protected WallChecker wallCheck;

    #endregion //fields

    #region parametres

    [SerializeField]
    protected float coalSpeed = .1f;//Скорость снаряда

    [SerializeField]
    protected float diveOutSpeed = .3f;//Скорость персонажа, когда он совершает особую атаку    

    [SerializeField]protected float attackDistance = 1f;//На каком расстоянии персонаж начинает атаковать
    [SerializeField]protected Vector2 coalHitSize = new Vector2(.1f,.1f);//Размер хитбокса снаряда
    [SerializeField]protected float coalForce = 80f;//Сила удара углём
    protected bool crit;

    protected bool diving = false;
    protected int divingPhase = 0;
    protected Vector3 divingPosition;
    protected float divingHealth=250f;//Если здоровье персонажа опустится ниже этого значения, то босс начнёт совершать особую атаку
    protected float divingPrecision = .05f;//Точность определения местоположения противника, когда босс находится под ним

    #endregion //parametres

    protected virtual void FixedUpdate()
    {
        if (agressive)
        {
            Vector3 targetPosition = currentTarget.transform.position;
            if (!diving)
            {
                if (employment > 2)
                {
                    float distance = Vector3.SqrMagnitude(targetPosition- transform.position);
                    if (distance > attackDistance * attackDistance)
                    {
                        if (!wallCheck.WallInFront())
                            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
                        else if ((targetPosition - transform.position).x * (int)orientation < 0f)
                            Turn();
                    }
                    else if (employment > 8)
                    {
                        if ((targetPosition - transform.position).x * (int)orientation < 0f)
                            Turn();
                        Attack();
                    }
                }
            }

            else
            {
                //Здесь описаны все фазы совершения особой атаки босса

                Vector3 direction = (divingPosition - transform.position).normalized;

                switch (divingPhase)
                {
                    case 1:
                        {
                            //Сначала босс опускается под землю
                            rigid.velocity = direction * speed*2;
                            if (Mathf.Abs(transform.position.y - divingPosition.y) < divingPrecision)
                            {
                                Vector3 pos = transform.position;
                                transform.position = new Vector3(pos.x, divingPosition.y, pos.z);
                                divingPhase++;
                                divingPosition = new Vector3(targetPosition.x,pos.y,pos.z);
                                rigid.velocity = Vector2.zero;
                            }
                            break;
                        }
                    case 2:
                        {
                            //"Заныривает" под противника
                            if (!wallCheck.WallInFront())
                                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
                            else if ((targetPosition - transform.position).x * (int)orientation < 0f)
                                Turn();
                            else
                                StopMoving();
                            if (Mathf.Abs(targetPosition.x - transform.position.x) < divingPrecision)
                            {
                                Vector3 pos = transform.position;
                                transform.position = new Vector3(targetPosition.x, pos.y, pos.z);
                                divingPhase++;
                                divingPosition = transform.position + diveDownOffset * Vector3.up;

                                Animate(new AnimationEventArgs("attack", "SpecialAttack", 0));
                                employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
                                hitBox.SetHitBox(new HitClass(damage, -1f, attackSize, attackPosition, hitForce));
                                rigid.velocity = Vector2.zero;
                            }
                            
                            break;
                        }
                    case 3:
                        {
                            //И выходит из-под земли с атакой
                            rigid.velocity = direction * diveOutSpeed;
                            if (Mathf.Abs(transform.position.y- divingPosition.y)<divingPrecision)
                            {
                                Vector3 pos = transform.position;
                                transform.position = new Vector3(pos.x, divingPosition.y, pos.z);
                                employment = Mathf.Clamp(employment + 8, 0, maxEmployment);
                                SetDiving(false);
                                rigid.velocity = Vector2.zero;
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }

            if (employment>2)
                if ((targetPosition - transform.position).x * (int)orientation < 0f)
                    Turn();
        }

        Analyse();
        Animate(new AnimationEventArgs("groundMove"));
    }

    protected override void Initialize()
    {
        base.Initialize();
        col = GetComponent<PolygonCollider2D>();
        Transform indicators = transform.FindChild("Indicators");
        wallCheck = indicators.FindChild("WallCheck").GetComponent<WallChecker>();
    }

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = new Vector2((int)_orientation * speed, rigid.velocity.y);
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }

    }

    /// <summary>
    /// Остановиться
    /// </summary>
    protected override void StopMoving()
    {
        rigid.velocity = new Vector2(0f, 0f);
    }

    /// <summary>
    /// Анализировать окружающую обстановку
    /// </summary>
    protected override void Analyse()
    {
        base.Analyse();
        if (!agressive)
        {
            if (Vector3.Distance(SpecialFunctions.player.transform.position, transform.position) <= sightRadius)
                BecomeAgressive();
        }

        if (health <= divingHealth)
        {
            while (health <=divingHealth)
                divingHealth -= divingHealthFold;
            if (agressive && !diving)
            {
                SetDiving(true);
            }
        }
    }

    /// <summary>
    /// Установить (или отменить) боссу режим особой атаки
    /// </summary>
    protected void SetDiving(bool yes)
    {
        if (yes)
        {
            diving = true;
            rigid.gravityScale = 0f;
            col.enabled = false;
            divingPhase = 1;
            divingPosition = transform.position + Vector3.down * diveDownOffset;
            StopAttack();
        }
        else
        {
            diving = false;
            rigid.gravityScale = 1f;
            col.enabled = true;
            divingPhase = 0;
            StartCoroutine(EmploymentProcess(5));
            hitBox.ResetHitBox();
        }
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        float a = (Random.Range(0f, 100f)) / 100f;
        crit =  (a<= critPosibility);
        StartCoroutine(AttackProcess());
    }

    /// <summary>
    /// Остановить атаку
    /// </summary>
    protected void StopAttack()
    {
        employment = maxEmployment;
        Animate(new AnimationEventArgs("stopAttack"));
        StopCoroutine(AttackProcess());
    }

    /// <summary>
    /// Процесс атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        Animate(new AnimationEventArgs("attack", crit ? "CritAttack": "Attack", 0));
        employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        yield return new WaitForSeconds(crit ? preCritAttackTime : preAttackTime);
        Vector3 playerPosition = SpecialFunctions.player.transform.position;

        Vector3 direction = (playerPosition - transform.position).x * (int)orientation >= 0f? (playerPosition - (transform.position + 
                                                                            new Vector3(sightOffsetX * (int)orientation, sightOffsetY, 0f))).normalized: (int)orientation*Vector3.right;
        GameObject newCoal = Instantiate(coal, transform.position + new Vector3(sightOffsetX * (int)orientation, sightOffsetY, 0f),
                                                  Quaternion.identity) as GameObject;
        Rigidbody2D coalRigid = newCoal.GetComponent<Rigidbody2D>();
        coalRigid.velocity = direction * coalSpeed;
        HitBox coalHitBox = coalRigid.GetComponentInChildren<HitBox>();
        if (coalHitBox != null)
        {
            coalHitBox.SetEnemies(enemies);
            coalHitBox.SetHitBox(new HitClass(crit? damage*1.5f:damage, -1f, coalHitSize, Vector2.zero, coalForce));
        }
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);

        yield return new WaitForSeconds(attackRate);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Процесс занятости
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator EmploymentProcess(int _employment)
    {
        employment = Mathf.Clamp(employment - _employment, 0, maxEmployment);
        yield return new WaitForSeconds(attackTime + preAttackTime);
        employment = Mathf.Clamp(employment + _employment, 0, maxEmployment);
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage)
    {
        Health = Mathf.Clamp(Health - damage, 0f, maxHealth);
        if (health <= 0f)
            Death();
        GhostMinerBossVisual bAnim = (GhostMinerBossVisual)anim;
        if (bAnim != null)
            bAnim.Blink();
    }

    /// <summary>
    /// Функция смерти
    /// </summary>
    protected override void Death()
    {
        GameObject _drop = Instantiate(drop, transform.position, transform.rotation) as GameObject;
        base.Death();
    }

}
