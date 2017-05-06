using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер, управляющий большим и неповоротливым големом
/// </summary>
public class GolemController : AIController
{

    #region consts

    protected const float dropForceX = 25f, dropForceY = 80f;

    protected const float golemSize = .12f;//Характерный размер голема
    protected const float areaRadius = 3f;//В пределах какого расстояния от своей начальной позиции осуществляет свою деятельность голем

    #endregion //consts

    #region fields

    protected WallChecker precipiceCheck;
    [SerializeField]protected List<GameObject> drop = new List<GameObject>();//Дроп при смерти персонажа
    protected Hearing hearing;//Слух персонажа

    #endregion //fields

    #region parametres

    protected override float attackDistance { get { return .2f; } }//На каком расстоянии должен стоять ИИ, чтобы решить атаковать
    protected override float sightRadius { get { return 2f; } }

    public override LoyaltyEnum Loyalty
    {
        get
        {
            return base.Loyalty;
        }

        set
        {
            base.Loyalty = value;
            if (hearing != null)
                hearing.AllyHearing = (value == LoyaltyEnum.ally);
        }
    }

    #endregion //parametres

    protected override void FixedUpdate()
    {
        if (!immobile)
        {
            base.FixedUpdate();
        }
        Animate(new AnimationEventArgs("groundMove"));
    }

    /// <summary>
    /// Инициализация
    /// </summary>
    protected override void Initialize()
    {
        indicators = transform.FindChild("Indicators");
        if (indicators != null)
        {
            precipiceCheck = indicators.FindChild("PrecipiceCheck").GetComponent<WallChecker>();

            hearing = indicators.GetComponentInChildren<Hearing>();
            if (hearing != null)
                hearing.hearingEventHandler += HandleHearingEvent;
        }

        base.Initialize();

        if (areaTrigger != null)
        {
            areaTrigger.triggerFunctionOut += AreaTriggerExitChangeBehavior;
        }

        BecomeCalm();

    }

    protected override void Start()
    {
        base.Start();

        if (areaTrigger!=null)
            areaTrigger.InitializeAreaTrigger();
    }

    #region movement

    /// <summary>
    /// Передвижение
    /// </summary>
    /// <param name="_orientation">Направление движения (влево/вправо)</param>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = precipiceCheck.WallInFront ? new Vector2((int)orientation * speed * speedCoof, rigid.velocity.y): new Vector2(0f, rigid.velocity.y);
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);
        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    /// <param name="_orientation">В какую сторону должен смотреть персонаж после поворота</param>
    public override void Turn(OrientationEnum _orientation)
    {
        base.Turn(_orientation);
        precipiceCheck.SetPosition(0f, (int)orientation);
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    protected override void Turn()
    {
        base.Turn();
        precipiceCheck.SetPosition(0f, (int)orientation);
    }

    #endregion //movement

    #region attack

    /// <summary>
    /// Функция получения урона
    /// </summary>
    /// <param name="hitData">Данные урона</param>
    public override void TakeDamage(HitParametres hitData)
    {
        if (hitData.attackPower == 0)//Голем не подвергается действиям препятствий
            return;
        base.TakeDamage(hitData);
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    /// <param name="hitData">Данные урона</param>
    /// <param name="ignoreInvul">Игнорирует ли прошедшая атака инвул персонажа</param>
    public override void TakeDamage(HitParametres hitData, bool ignoreInvul)
    {
        if (hitData.attackPower == 0)//Голем не подвергается действиям препятствий
            return;
        base.TakeDamage(hitData, ignoreInvul);
    }

    /*
    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, int attackPower = 0)
    {
        if (GetBuff("StunnedProcess") != null || GetBuff("FrozenProcess") != null)
            base.TakeDamage(damage, _dType, attackPower);
        else
            base.TakeDamage(damage, _dType, 0);
    }

    /// <summary>
    /// Функция получения урона, которая, возможно, игнорирует состояние инвула
    /// </summary>
    public override void TakeDamage(float damage, DamageType _dType, bool ignoreInvul, int attackPower = 0)
    {
        if (GetBuff("StunnedProcess") != null || GetBuff("FrozenProcess") != null)
            base.TakeDamage(damage, _dType, attackPower);
        else
            base.TakeDamage(damage, _dType, 0);
    }
    */

    /// <summary>
    /// Функция смерти
    /// </summary>
    protected override void Death()
    {
        base.Death();
        if (dead)
            foreach (GameObject dropObj in drop)
            {
                GameObject drop1 = Instantiate(dropObj, transform.position, transform.rotation) as GameObject;
                Rigidbody2D rigid = drop1.GetComponent<Rigidbody2D>();
                if (rigid != null)
                    rigid.AddForce(new Vector2(Random.Range(-dropForceX, dropForceX), dropForceY));
            }
    }

    #endregion //attack

    #region effects

    /// <summary>
    /// Оглушиться
    /// </summary>
    protected override void BecomeStunned(float _time)
    {
        float rand = Random.Range(0f, 1f);
        if (rand<.5f)
            base.BecomeStunned(_time);
    }

    protected override void BecomePoisoned(float _time)
    {
    }

    #endregion //effects

    /// <summary>
    /// Провести анализ окружающей обстановки
    /// </summary>
    protected override void Analyse()
    {
        Vector2 pos = transform.position;
        switch (behavior)
        {
            case BehaviorEnum.agressive:
                {

                    Vector2 direction = mainTarget - pos;
                    Vector2 directionN = direction.normalized;
                    RaycastHit2D hit = Physics2D.Raycast(pos+directionN* sightOffset, direction.normalized, direction.magnitude, LayerMask.GetMask(gLName));
                    if (hit)
                    {
                        //Если враг ушёл достаточно далеко
                        if (direction.magnitude > sightRadius * 0.75f)
                        {
                            ChangeMainTarget();
                        }
                    }

                    if (Vector2.SqrMagnitude(mainTarget - (Vector2)beginPosition) > areaRadius * areaRadius)
                        ChangeMainTarget();

                    break;
                }
            case BehaviorEnum.patrol:
                {
                    Vector2 direction = Vector2.right * (int)orientation;
                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * direction + .04f * Vector2.down, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (enemies.Contains(hit.collider.gameObject.tag))
                        {
                            MainTarget = new ETarget(hit.collider.transform);
                            BecomeAgressive();
                        }
                    }

                    if (loyalty == LoyaltyEnum.ally)
                    {
                        float sqDistance = Vector2.SqrMagnitude(beginPosition - pos);
                        if (sqDistance < allyDistance)
                        {
                            StopMoving();
                            BecomeCalm();
                        }
                    }

                    break;
                }

            case BehaviorEnum.calm:
                {
                    Vector2 direction = Vector3.right * (int)orientation;
                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * direction+.04f*Vector2.down, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (enemies.Contains(hit.collider.gameObject.tag))
                        {
                            MainTarget = new ETarget(hit.collider.transform);
                            BecomeAgressive();
                        }
                    }

                    if (loyalty == LoyaltyEnum.ally)
                    {
                        if (Vector2.SqrMagnitude(beginPosition - pos) > allyDistance * 1.2f)
                            if (Vector2.SqrMagnitude(beginPosition - (Vector2)prevTargetPosition) > minCellSqrMagnitude)
                                GoHome();
                        if ((int)orientation * (beginPosition - pos).x < 0f)
                            Turn();//Всегда быть повёрнутым к герою-союзнику
                    }

                    break;
                }

            default:
                {
                    break;
                }
        }

    }

    #region behaviorActions

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        if (hearing != null)
            hearing.enabled = true;
    }

    /// <summary>
    /// Стать агрессивным
    /// </summary>
    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        if (hearing != null)
            hearing.enabled = false;
    }

    /// <summary>
    /// Стать патрулирующим
    /// </summary>
    protected override void BecomePatrolling()
    {
        base.BecomePatrolling();
        if (hearing != null)
            hearing.enabled = true;
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {
        Vector2 pos = transform.position;
        Vector2 targetPosition = mainTarget;
        Vector2 targetDistance = targetPosition - pos;
        float sqDistance = targetDistance.sqrMagnitude;

        if (waiting)
        {

            #region waiting

            if (sqDistance < waitingNearDistance)
                Move((OrientationEnum)Mathf.RoundToInt(-Mathf.Sign(targetDistance.x)));
            else if (sqDistance < waitingFarDistance)
            {
                StopMoving();
                if ((int)orientation * (targetPosition - pos).x < 0f)
                    Turn();
            }
            else
            {
                if (precipiceCheck.WallInFront)
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                else if ((targetPosition - pos).x * (int)orientation < 0f)
                    Turn();
            }

            #endregion //waiting

        }

        else
        {

            if (employment > 8)
            {

                if (Vector2.SqrMagnitude(targetDistance) > attackDistance * attackDistance)
                {
                    if (precipiceCheck.WallInFront)
                    {
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                    }
                    else if ((targetPosition - pos).x * (int)orientation < 0f)
                        Turn();                    
                }
                else
                {
                    StopMoving();
                    if ((targetPosition - pos).x * (int)orientation < 0f)
                        Turn();
                    Attack();
                }
            }
        }
    }

    /// <summary>
    /// Патрулирующее поведение
    /// </summary>
    protected override void PatrolBehavior()
    {
        Vector2 targetDistance = currentTarget - transform.position;
        if (targetDistance.sqrMagnitude > minCellSqrMagnitude)
            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
        else
        {
            if (Vector2.SqrMagnitude(currentTarget - (Vector2)beginPosition) < minCellSqrMagnitude)
            {
                BecomeCalm();
                Turn(beginOrientation);
            }
            else
                GoHome();
        }
    }

    /// <summary>
    /// Сменить главную цель
    /// </summary>
    protected override void ChangeMainTarget()
    {
        Transform prevTarget = mainTarget.transform;
        MainTarget = ETarget.zero;
        Transform obj = SpecialFunctions.battleField.GetNearestCharacter(transform.position, loyalty == LoyaltyEnum.ally, prevTarget);
        if (obj != null)
            MainTarget = new ETarget(obj);
        else
            GoHome();
    }

    /// <summary>
    /// Выдвинуться к указанной точке (если это возможно)
    /// </summary>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        BecomePatrolling();
        currentTarget = new ETarget(targetPosition);
        currentTarget.exists = true;
    }

    /// <summary>
    /// Направиться к изначальной позиции
    /// </summary>
    public override void GoHome()
    {
        MainTarget = ETarget.zero;
        BecomePatrolling();
        currentTarget = beginPosition;
    }

    /// <summary>
    /// Никак не среагировать на услышанный боевой клич
    /// </summary>
    public override void HearBattleCry(Vector2 cryPosition)
    {}

    #endregion //behaviorAction

    #region optimization

    /// <summary>
    /// Сменить модель поведения в связи с выходом из триггера
    /// </summary>
    protected override void AreaTriggerExitChangeBehavior()
    {
        if (behavior == BehaviorEnum.agressive)
        {
            GoHome();
        }
    }

    /// <summary>
    /// Функция реализующая анализ окружающей персонажа обстановки, когда тот находится в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
        if (behavior != BehaviorEnum.calm)
            if (!followOptPath)
                StartCoroutine("PathPassOptProcess");
    }

    /// <summary>
    /// Функция, которая восстанавливает положение и состояние персонажа, пользуясь данными, полученными в оптимизированном режиме
    /// </summary>
    protected override void RestoreActivePosition()
    {
        if (!currentTarget.exists)
        {
            Turn(beginOrientation);
        }
    }

    /// <summary>
    /// Функция, которая переносит персонажа в ту позицию, в которой он может нормально функционировать для ведения оптимизированной версии 
    /// </summary>
    protected override void GetOptimizedPosition()
    {
        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position+Vector2.down*golemSize, Vector2.down, navMap.mapSize.magnitude, LayerMask.GetMask(gLName));
        if (!hit)
        {
            Death();
        }
        else
        {
            transform.position = hit.point + Vector2.up * 0.02f;
        }
    }

    /// <summary>
    /// Процесс оптимизированного прохождения пути. Заключается в том, что персонаж, зная свой маршрут, появляется в его различиных позициях, не используя 
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator PathPassOptProcess()
    {
        followOptPath = true;
        if (!currentTarget.exists)
            BecomeCalm();
        else
        {
            while (currentTarget.exists)
            {
                Vector2 pos = transform.position;
                Vector2 targetPos = currentTarget;

                if (Vector2.SqrMagnitude(pos - targetPos) <= minCellSqrMagnitude)
                {
                    currentTarget.Exists = false;
                    pos = transform.position;
                }
                if (currentTarget.exists)
                {
                    Vector2 direction = targetPos - pos;
                    transform.position = pos + direction.normalized * Mathf.Clamp(speed, 0f, direction.magnitude);
                }
                yield return new WaitForSeconds(optTimeStep);
            }
            currentTarget.Exists = false;
            followOptPath = false;
        }
    }

    #endregion //optimization

}
