using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер, управляющий стрелком
/// </summary>
public class ShooterController : HumanoidController
{

    #region fields

    [SerializeField]protected GameObject missile;//Снаряды стрелка

    #endregion //fields

    #region parametres

    protected virtual Vector2 shootPosition { get { return new Vector2(0.0653f, 0f); } }//Откуда стреляет персонаж
    protected virtual float attackRate { get { return 3f; } }//Сколько секунд проходит между атаками

    public override bool Waiting {get{return base.Waiting;} set{base.Waiting = value;StopCoroutine("AttackProcess"); Animate(new AnimationEventArgs("stop")); } }

    [SerializeField]protected float missileSpeed = 3f;//Скорость снаряда после выстрела

    #endregion //parametres

    /// <summary>
    /// Совершить атаку
    /// </summary>
    protected override void Attack()
    {
        Animate(new AnimationEventArgs("attack", "", Mathf.RoundToInt(10 * (attackParametres.preAttackTime))));
        StartCoroutine("AttackProcess");
    }

    public override void TakeDamage(float damage, DamageType _dType, bool _microstun = true)
    {
        base.TakeDamage(damage, _dType, _microstun);
        if (_microstun)
            Animate(new AnimationEventArgs("stop"));
    }

    public override void TakeDamage(float damage, DamageType _dType, bool ignoreInvul, bool _microstun)
    {
        base.TakeDamage(damage, _dType, ignoreInvul, _microstun);
        if (_microstun)
            Animate(new AnimationEventArgs("stop"));
    }

    /// <summary>
    /// Процесс совершения атаки
    /// </summary>
    protected override IEnumerator AttackProcess()
    {
        employment = Mathf.Clamp(employment - 8, 0, maxEmployment);
        yield return new WaitForSeconds(attackParametres.preAttackTime);

        Vector2 pos = transform.position;
        Vector2 _shootPosition = pos + new Vector2(shootPosition.x * (int)orientation, shootPosition.y);
        Vector2 direction = (currentTarget - pos).x * (int)orientation >= 0f ? (currentTarget - _shootPosition).normalized : (int)orientation * Vector2.right;
        GameObject newMissile = Instantiate(missile, _shootPosition,Quaternion.identity) as GameObject;
        Rigidbody2D missileRigid = newMissile.GetComponent<Rigidbody2D>();
        missileRigid.velocity = direction * missileSpeed;
        HitBoxController missileHitBox = missileRigid.GetComponentInChildren<HitBoxController>();
        if (missileHitBox != null)
        {
            missileHitBox.SetEnemies(enemies);
            missileHitBox.SetHitBox(new HitParametres(attackParametres));
            missileHitBox.allyHitBox = loyalty==LoyaltyEnum.ally;
            missileHitBox.Attacker = gameObject;
        }
        employment = Mathf.Clamp(employment + 5, 0, maxEmployment);

        RushForward();

        yield return new WaitForSeconds(attackRate);
        employment = Mathf.Clamp(employment + 3, 0, maxEmployment);
    }

    /// <summary>
    /// Если после выстрела текущий противник находится слишком близко, а назад бежать - некуда, то двинуться вперёд
    /// </summary>
    protected virtual void RushForward()
    {
        if (!currentTarget.exists ? true : currentTarget.transform == null)
            return;//Проверка на существование противника
        Vector2 pos = transform.position;
        Vector2 targetDistance = currentTarget - pos;
        if (targetDistance.magnitude > waitingNearDistance)
            return;//Проверка на близость противника
        if (navMap == null || !(navMap is NavigationBunchedMap))
            return;
        NavigationBunchedMap _map = (NavigationBunchedMap)navMap;
        ComplexNavigationCell currentCell = (ComplexNavigationCell)_map.GetCurrentCell(transform.position);
        if (currentCell == null)
            return;//Проверки на существование навигационной карты и текущей навигационной клетки
        bool hasBehind = false;//Проверка на путь отступления
        int frontSign = Mathf.RoundToInt(Mathf.Sign(targetDistance.x));
        ComplexNavigationCell nextCell = currentCell;
        Vector2 pos1 = currentCell.cellPosition;
        foreach (NeighborCellStruct neighbor in currentCell.neighbors)
        {
            nextCell = _map.GetCell(neighbor.groupNumb, neighbor.cellNumb);
            Vector2 pos2 = nextCell.cellPosition;
            if (Mathf.Abs(pos1.y - pos2.y) < navCellSize / 2f && (pos2.x - pos1.x) * frontSign < 0 && neighbor.connectionType == NavCellTypeEnum.usual)
            {
                hasBehind = true;
                break;
            }
        }
        if (hasBehind)
            return;//Есть путь отступления
        //Если пути назад нет, то пытаемся прорваться вперёд
        bool hasNext = true;
        nextCell = currentCell;
        while (hasNext)
        {
            pos1 = currentCell.cellPosition;
            hasNext = false;
            if (Mathf.Abs(pos1.x - pos.x) > waitingNearDistance*2)
            {
                break;
            }
            foreach (NeighborCellStruct neighbor in currentCell.neighbors)
            {
                nextCell = _map.GetCell(neighbor.groupNumb, neighbor.cellNumb);
                Vector2 pos2 = nextCell.cellPosition;
                if (Mathf.Abs(pos1.y - pos2.y) < navCellSize / 2f && (pos2.x - pos1.x) * frontSign > 0 && neighbor.connectionType == NavCellTypeEnum.usual)
                {
                    currentCell = nextCell;
                    hasNext = true;
                    break;
                }
            }
        }
        Waypoints=FindPath(currentCell.cellPosition, maxAgressivePathDepth*2);
    }

    /// <summary>
    /// Подготовить данные для ведения деятельности в следующей модели поведения
    /// </summary>
    protected override void RefreshTargets()
    {
        base.RefreshTargets();
        Animate(new AnimationEventArgs("stop"));
        StopCoroutine("AttackProcess");
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {

        if (mainTarget.exists && employment > 2)
        {
            Vector2 pos = transform.position;
            Vector2 targetPosition=mainTarget;
            Vector2 targetDistance= targetPosition - pos;
            Vector2 mainPos = mainTarget;
            if (waypoints == null)
            {

                #region directWay

                float sqDistance = targetDistance.sqrMagnitude;

                if (sqDistance < waitingNearDistance)
                {
                    if (!wallCheck.WallInFront && (precipiceCheck.WallInFront || !grounded))
                        Move((OrientationEnum)Mathf.RoundToInt(-Mathf.Sign(targetDistance.x)));
                    else if (targetDistance.x * (int)orientation > 0)
                        Turn();
                    if (!waiting && employment > 8)
                    {
                        StopMoving();
                        if ((targetPosition - pos).x * (int)orientation < 0f)
                            Turn();
                        Attack();
                    }
                }
                else if (sqDistance < waitingFarDistance)
                {
                    StopMoving();
                    if ((int)orientation * targetDistance.x < 0f)
                        Turn();
                    if (!waiting && employment > 8)
                    {
                        StopMoving();
                        if ((targetPosition - pos).x * (int)orientation < 0f)
                            Turn();
                        Attack();
                    }
                }
                else
                {
                    if (!wallCheck.WallInFront && (precipiceCheck.WallInFront || !grounded) && (Mathf.Abs((pos - mainPos).y) < navCellSize * 5f ? true : !targetCharacter.OnLadder))
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                    else if (targetDistance.x * (int)orientation < 0f)
                        Turn();
                    else
                    {
                        StopMoving();
                        if (Vector2.SqrMagnitude(pos - mainPos) > minCellSqrMagnitude * 16f &&
                            (Vector2.SqrMagnitude(mainPos - prevTargetPosition) > minCellSqrMagnitude || !prevTargetPosition.exists))
                        {
                            Waypoints = FindPath(targetPosition, maxAgressivePathDepth);
                            if (waypoints == null)
                                StopMoving();
                        }
                    }
                }

                #endregion //directWay

            }
            else
            {

                #region complexWay

                if (employment > 8 ? NeedToFindPath() : false)//Если главная цель сменила своё местоположение
                {
                    Waypoints = FindPath(mainPos, maxAgressivePathDepth);
                    return;
                }

                if (employment>8)
                    if (grounded && !onLadder && targetDistance.sqrMagnitude < waitingFarDistance)
                    {
                        StopMoving();
                        if ((targetPosition - pos).x * (int)orientation < 0f)
                            Turn();
                        Waypoints = null;
                        Attack();
                        return;
                    }

                if (waypoints.Count > 0)
                {
                    if (!currentTarget.exists)
                    {
                        currentTarget = new ETarget(waypoints[0].cellPosition);
                        if (((ComplexNavigationCell)waypoints[0]).cellType == NavCellTypeEnum.movPlatform)
                            FindPlatform(((ComplexNavigationCell)waypoints[0]).id);
                    }

                    bool waypointIsAchieved = false;
                    targetPosition = currentTarget;
                    targetDistance = targetPosition - pos;
                    if (!waitingForPlatform && (transform.parent != platformTarget.transform || !platformTarget.exists))
                    {
                        if (onLadder)
                        {
                            LadderMove(Mathf.Sign(targetDistance.y));
                            waypointIsAchieved = Mathf.Abs(currentTarget.y - pos.y) < navCellSize / 2f;
                        }
                        else if (currentPlatform != null ? !grounded : true)
                        {
                            if (Mathf.Abs(targetDistance.x) > navCellSize / 4f)
                                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                            else
                            {
                                transform.position = new Vector3(targetPosition.x, pos.y);
                                StopMoving();
                            }
                            waypointIsAchieved = Vector3.SqrMagnitude(currentTarget - transform.position) < minCellSqrMagnitude;
                        }
                    }
                    else
                    {
                        StopMoving();
                        if (!waitingForPlatform)
                        {
                            Vector2 platformDirection = currentPlatform.Direction;
                            float projection = Vector2.Dot(targetDistance, platformDirection);
                            Turn((OrientationEnum)Mathf.Sign(platformDirection.x));
                            waypointIsAchieved = Mathf.Abs(projection) < navCellSize / 2f;
                            if (currentTarget == platformTarget && transform.parent == platformTarget.transform)
                            {
                                transform.position = platformTarget + Vector3.up * 0.08f;
                                waypointIsAchieved = true;
                            }
                        }
                    }


                    if (waypointIsAchieved)
                    {
                        ComplexNavigationCell currentWaypoint = (ComplexNavigationCell)waypoints[0];
                        if (currentTarget == platformTarget)
                        {
                            if (waypoints.Count > 1 ? (waypoints[1].cellPosition - (Vector2)transform.position).x * (int)orientation < 0f : false)
                                Turn();
                            transform.position = platformTarget + Vector3.up * .09f;
                        }
                        else
                            currentTarget.Exists = false;

                        waypoints.RemoveAt(0);

                        if (waypoints.Count > 2 ? !targetCharacter.OnLadder : false)//Проверить, не стал ли путь до главной цели прямым и простым, чтобы не следовать ему больше
                        {
                            bool directPath = employment>8;

                            if (directPath)
                            {
                                Vector2 cellsDirection = (waypoints[1].cellPosition - waypoints[0].cellPosition).normalized;
                                NavCellTypeEnum cellsType = ((ComplexNavigationCell)waypoints[0]).cellType;
                                for (int i = 2; i < waypoints.Count; i++)
                                {
                                    if (Vector2.Angle((waypoints[i].cellPosition - waypoints[i - 1].cellPosition), cellsDirection) > minAngle || ((ComplexNavigationCell)waypoints[i - 1]).cellType != cellsType)
                                    {
                                        directPath = false;
                                        break;
                                    }
                                }
                            }
                            if (directPath)
                            {
                                //Если путь прямой, несложный, то монстр может самостоятельно добраться до игрока, не используя маршрут, и атаковать его
                                Waypoints = null;
                                return;
                            }
                        }

                        if (waypoints.Count == 0)//Если маршрут кончился, перестать следовать ему
                        {
                            if (!targetCharacter.OnLadder)
                            {
                                Waypoints = null;
                                return;
                            }
                            else
                            {
                                if (onLadder)
                                    StopLadderMoving();
                            }
                        }
                        else
                        {
                            ComplexNavigationCell nextWaypoint = (ComplexNavigationCell)waypoints[0];
                            NeighborCellStruct neighborConnection = currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb);
                            //Продолжаем следование
                            currentTarget = new ETarget(nextWaypoint.cellPosition);
                            if (nextWaypoint.cellType == NavCellTypeEnum.movPlatform && !platformTarget.exists)
                            {
                                StopMoving();
                                waitingForPlatform = true;
                                FindPlatform(nextWaypoint.id);
                                platformConnectionType = neighborConnection.connectionType;
                            }
                            else if (neighborConnection.connectionType == NavCellTypeEnum.jump && (grounded || onLadder))
                            {
                                //Перепрыгиваем препятствие
                                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x)));
                                //transform.position = new Vector3(currentWaypoint.cellPosition.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                                Jump();
                            }
                            /*else if (Mathf.Abs(currentWaypoint.cellPosition.x-nextWaypoint.cellPosition.x)<2*navCellSize &&
                                       currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb).connectionType == NavCellTypeEnum.jump &&
                                       nextWaypoint.cellPosition.y < currentWaypoint.cellPosition.y - 2 * navCellSize)
                            {
                                //Спрыгиваем вниз
                                jumping = true;
                            }*/
                            else if (!onLadder ?
                                currentWaypoint.cellType == NavCellTypeEnum.ladder && nextWaypoint.cellType == NavCellTypeEnum.ladder && Mathf.Approximately(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x, 0f) : false)
                            {
                                LadderOn();
                            }
                            if (currentWaypoint.cellType == NavCellTypeEnum.ladder && (currentWaypoint.id != nextWaypoint.id))
                            {
                                LadderOff();
                                //Jump();
                            }
                            else if (currentWaypoint.cellType == NavCellTypeEnum.movPlatform && nextWaypoint.cellType != NavCellTypeEnum.movPlatform)
                                CurrentPlatform = null;
                        }
                    }
                }
            }

            #endregion //complexWay

        }
    }

}