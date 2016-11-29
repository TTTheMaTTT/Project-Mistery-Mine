using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер паука (в данном случае джунглиевого)
/// </summary>
public class SpiderController : AIController
{

    #region consts

    protected const float attackTime = .6f, preAttackTime = .3f;

    protected const float patrolDistance = 2f;//По таким дистанциям паук будет патрулировать

    protected const float beCalmTime = 4f;//Время через которое пауки перестают преследовать игрока, если он ушёл из их поля зрения

    #endregion //consts

    #region fields

    protected WallChecker wallCheck, precipiceCheck;

    #endregion //fields

    #region parametres

    [SerializeField] protected float attackDistance = .2f;//На каком расстоянии должен стоять паук, чтобы решить атаковать

    protected Vector2 waypoint;//Пункт назначения, к которому стремится ИИ

    protected bool moveOut = false;

    protected bool calmDown = false;//Успокаивается ли персонаж

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
                    if (!wallCheck.WallInFront() && (precipiceCheck.WallInFront()))
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetPosition.x - transform.position.x)));
                    else if ((targetPosition - transform.position).x * (int)orientation < 0f)
                        Turn();
                }
                else
                {
                    if ((targetPosition - transform.position).x * (int)orientation < 0f)
                        Turn();
                    Attack();
                }
            }
            else if (!agressive)
            {
                if ((Vector2.Distance(waypoint, transform.position) < attackDistance) || (wallCheck.WallInFront() || !(precipiceCheck.WallInFront())))
                {
                    Turn();
                    Patrol();
                }
                else
                {
                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(waypoint.x - transform.position.x)));
                }
            }
        }
        else if (moveOut)
        {
            MoveOut();
        }
        Animate(new AnimationEventArgs("groundMove"));
        Analyse();
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
    /// Провести анализ окружающей обстановки
    /// </summary>
    protected override void Analyse()
    {
        base.Analyse();
        
        //Определяем степень агрессивности
        if (agressive)
        {
            float targetDistance = Vector3.Distance(currentTarget.transform.position, transform.position);
            if (calmDown)
            {
                if (targetDistance<=sightRadius)
                {
                    StopCoroutine(BecomeCalmProcess());
                    calmDown = false;
                }
            }
            else
            {
                if (targetDistance > sightRadius)
                {
                    StartCoroutine(BecomeCalmProcess());
                }
            }
        }
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
    /// Процесс успокаивания персонажа
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerator BecomeCalmProcess()
    {
        calmDown = true;
        yield return new WaitForSeconds(beCalmTime);
        agressive = false;
        mainTarget = null;
        currentTarget = null;
    }

    #region storyActions

    /// <summary>
    /// Выйти вперёд
    /// </summary>
    public void MoveForwardAction(StoryAction _action)
    {
        //Animate(new AnimationEventArgs("moveForward"));
        StartCoroutine(MoveForwardProcess());
    }

    /// <summary>
    /// Процесс нападения из засады
    /// </summary>
    IEnumerator MoveForwardProcess()
    {
        moveOut = true;
        rigid.isKinematic = true;
        immobile = true;
        yield return new WaitForSeconds(1f);
        moveOut = false;
        rigid.isKinematic = false;
        immobile = false;
    }

    /// <summary>
    /// Выдвинуться вперёд (из некоторого препятствия)
    /// </summary>
    protected void MoveOut()
    {
        transform.position += new Vector3(speed * Time.fixedDeltaTime*(int)orientation, 0f, 0f);
    }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public override List<string> actionNames()
    {
        return new List<string>(){ };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() { { "moveForward", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() { { "moveForward", new List<string>() { } } };
    }

    #endregion //IHaveStory

}
