using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DogController : HumanoidController
{

    #region parametres

    protected override float attackDistance { get { return .16f; } }

    #endregion //parametres

    protected override List<NavigationCell> Waypoints
    {
        get
        {
            return waypoints;
        }
        set
        {
            StopFollowOptPath();
            waypoints = value;
            if (value != null)
            {
                currentTarget.Exists = false;
            }
            else
            {
                StopAvoid();
                if (mainTarget.exists && behavior == BehaviorEnum.agressive)
                {
                    currentTarget = mainTarget;
                }
            }
            jumping = false;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!grounded)
        {
            Animate(new AnimationEventArgs("airMove"));
        }
        else
        {
            Animate(new AnimationEventArgs("groundMove"));
        }
    }

    /// <summary>
    /// Совершить прыжок
    /// </summary>
    protected override void Jump()
    {
        rigid.velocity = new Vector3(rigid.velocity.x, 0f, 0f);
        rigid.AddForce(new Vector2(jumpForce * 0.5f, jumpForce));
        StartCoroutine(JumpProcess());
    }

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
                    RaycastHit2D hit = Physics2D.Raycast(pos, direction.normalized, direction.magnitude, LayerMask.GetMask(gLName));
                    if (hit)
                    {
                        //Если враг ушёл достаточно далеко
                        if (direction.magnitude > sightRadius * 0.75f)
                        {
                            GoToThePoint(mainTarget);
                            if (behavior == BehaviorEnum.agressive)
                            {
                                GoHome();
                                break;
                            }
                            else
                                StartCoroutine("BecomeCalmProcess");
                        }
                    }
                    //if (currentTarget == null)
                    //break;
                    if (!hit ?
                        ((pos - prevPosition).sqrMagnitude < speed * speedCoof * Time.fixedDeltaTime / 10f && currentTarget != mainTarget) : true)
                    {
                        if (!avoid)
                            StartCoroutine("AvoidProcess");
                    }

                    break;
                }
            case BehaviorEnum.patrol:
                {
                    if (!currentTarget.exists)
                    {
                        if (!avoid)
                        {
                            StartCoroutine("AvoidProcess");
                        }
                        break;
                    }
                    if (!avoid)
                    {
                        if ((pos - prevPosition).sqrMagnitude < speed * speedCoof * Time.fixedDeltaTime / 10f)
                            StartCoroutine("AvoidProcess");
                    }
                    Vector2 direction = Vector3.right * (int)orientation;
                    if (mainTarget.exists)
                    {
                        if (Vector2.SqrMagnitude(mainTarget - pos) < minCellSqrMagnitude)
                        {
                            MainTarget = mainTarget;
                            BecomeAgressive();
                        }
                    }
                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * direction, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
                    if (hit)
                    {
                        if (enemies.Contains(hit.collider.gameObject.tag))
                        {
                            MainTarget = new ETarget(hit.collider.transform);
                            BecomeAgressive();
                        }
                    }

                    //Если нет основной цели и стоящий на земле гуманоид - союзник героя, то он следует к нему
                    if (loyalty == LoyaltyEnum.ally ? !mainTarget.exists && grounded : false)
                    {
                        float sqDistance = Vector2.SqrMagnitude(beginPosition - pos);
                        if (sqDistance > allyDistance * 1.2f && followAlly)
                        {
                            if (Vector2.SqrMagnitude(beginPosition - (Vector2)prevTargetPosition) > minCellSqrMagnitude)
                            {
                                prevTargetPosition = new EVector3(pos);//Динамическое преследование героя-союзника
                                GoHome();
                                StartCoroutine("ConsiderAllyPathProcess");
                            }
                        }
                        else
                        if (sqDistance < allyDistance)
                        {
                            if (grounded || onLadder)
                                StopMoving();
                            BecomeCalm();
                        }
                    }

                    break;
                }

            case BehaviorEnum.calm:
                {
                    Vector2 direction = Vector3.right * (int)orientation;
                    RaycastHit2D hit = Physics2D.Raycast(pos + sightOffset * direction, direction, sightRadius, LayerMask.GetMask(gLName, cLName));
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
                        {
                            if (Vector2.SqrMagnitude(beginPosition - (Vector2)prevTargetPosition) > minCellSqrMagnitude)
                            {
                                prevTargetPosition = new EVector3(pos);
                                GoHome();
                            }
                        }
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

        prevPosition = new EVector3(transform.position, true);

    }

    /// <summary>
    /// Определить, нужно ли искать отдельный маршрут до главной цели
    /// </summary>
    /// <returns>Возвращает факт необходимости поиска пути</returns>
    protected override bool NeedToFindPath()
    {
        Vector2 mainPos = mainTarget;
        bool changePosition = grounded  && Vector2.SqrMagnitude(mainPos - prevTargetPosition) > minCellSqrMagnitude * 16f;
        return changePosition;
    }



    #region behaviorActions

    /// <summary>
    /// Процесс обхода препятствия
    /// </summary>
    protected override IEnumerator AvoidProcess()
    {
        avoid = true;
        EVector3 _prevPos = prevPosition;
        yield return new WaitForSeconds(avoidTime);
        Vector3 pos = (Vector2)transform.position;
        if ((transform.position - _prevPos).sqrMagnitude < speed * speedCoof * Time.fixedDeltaTime / 10f && avoid)
        {
            if (currentTarget.exists)
            {
                if (currentTarget != mainTarget)
                    transform.position += (currentTarget - pos).normalized * navCellSize;
                yield return new WaitForSeconds(avoidTime);
                pos = (Vector2)transform.position;
                //Если не получается обойти ставшее на пути препятствие
                if (currentTarget.exists && currentTarget != mainTarget && avoid &&
                    (pos - _prevPos).sqrMagnitude < speed * speedCoof * Time.fixedDeltaTime / 10f)
                {
                    if (mainTarget.exists)
                    {
                        if (behavior == BehaviorEnum.agressive)
                        {
                            Waypoints = FindPath(mainTarget, maxAgressivePathDepth);
                            if (waypoints == null)
                                GoHome();
                        }
                        else
                            GoHome();
                    }
                    else
                    {
                        if (waypoints != null ? waypoints.Count > 0 : false)
                            GoToThePoint(waypoints[waypoints.Count - 1].cellPosition);
                        else
                            GoHome();
                    }
                    if (behavior == BehaviorEnum.patrol && beginPosition.transform == null)
                        StartCoroutine(ResetStartPositionProcess(transform.position));

                }
            }
            else if (!currentTarget.exists)
            {
                yield return new WaitForSeconds(avoidTime);
                pos = (Vector2)transform.position;
                if (!currentTarget.exists)
                {
                    GoHome();
                    if (behavior == BehaviorEnum.patrol && beginPosition.transform == null)
                        StartCoroutine(ResetStartPositionProcess(pos));
                }
            }
        }
        avoid = false;
    }

    /// <summary>
    /// Специальный процесс, учитывающий то, что персонаж долгое время не может достичь какой-то позиции. Тогда, считаем, что опорной позицией персонажа станет текущая позиция (если уж он не может вернуться домой)
    /// </summary>
    /// <param name="prevPosition">Предыдущая позиция персонажа. Если спустя какое-то время ИИ никак не сдвинулся относительно неё, значит нужно привести в действие данный процесс</param>
    /// <returns></returns>
    protected override IEnumerator ResetStartPositionProcess(Vector2 prevPosition)
    {
        yield return new WaitForSeconds(avoidTime);
        Vector2 pos = transform.position;
        if (Vector2.SqrMagnitude(prevPosition - pos) < minCellSqrMagnitude && behavior == BehaviorEnum.patrol)
        {
            beginPosition = new ETarget(pos);
            beginOrientation = orientation;
            BecomeCalm();
        }
    }

    /// <summary>
    /// Агрессивное поведение
    /// </summary>
    protected override void AgressiveBehavior()
    {

        if (mainTarget.exists && employment > 2)
        {

            Vector2 targetPosition;
            Vector2 targetDistance;
            Vector2 pos = transform.position;
            Vector2 mainPos = mainTarget;
            if (waypoints == null)
            {

                #region directWay

                targetPosition = mainTarget;
                targetDistance = targetPosition - pos;
                float sqDistance = targetDistance.sqrMagnitude;

                if (waiting)
                {

                    #region waiting

                    if (sqDistance < waitingNearDistance)
                    {
                        if (!wallCheck.WallInFront && (precipiceCheck.WallInFront || !grounded))
                            Move((OrientationEnum)Mathf.RoundToInt(-Mathf.Sign(targetDistance.x)));
                    }
                    else if (sqDistance < waitingFarDistance)
                    {
                        StopMoving();
                        if ((int)orientation * (targetPosition - pos).x < 0f)
                            Turn();
                    }
                    else
                    {
                        if (!wallCheck.WallInFront && (precipiceCheck.WallInFront || !grounded))
                        {
                            Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                        }
                        else if ((targetPosition - pos).x * (int)orientation < 0f)
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

                    #endregion //waiting

                }

                else
                {

                    if (employment > 8)
                    {

                        if (Vector2.SqrMagnitude(targetDistance) > attackDistance * attackDistance)
                        {
                            if (!wallCheck.WallInFront && (precipiceCheck.WallInFront || !grounded))
                            {
                                if (Mathf.Abs(targetDistance.x) > attackDistance)
                                    Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                                else
                                    StopMoving();
                            }
                            else if ((targetPosition - pos).x * (int)orientation < 0f)
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
                        else
                        {
                            StopMoving();
                            if ((targetPosition - pos).x * (int)orientation < 0f)
                                Turn();
                            Attack();
                        }
                    }
                }
                #endregion //directWay

            }
            else
            {

                #region complexWay

                if (NeedToFindPath())//Если главная цель сменила своё местоположение
                {
                    Waypoints = FindPath(mainPos, maxAgressivePathDepth);
                    //prevTargetPosition = new EVector3(mainTarget.transform.position, true);
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

                    if (Mathf.Abs(targetDistance.x) > navCellSize / 4f)
                        Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
                    else
                    {
                        transform.position = new Vector3(targetPosition.x, pos.y);
                        StopMoving();
                    }
                    waypointIsAchieved = Vector3.SqrMagnitude(currentTarget - transform.position) < minCellSqrMagnitude;

                    if (waypointIsAchieved)
                    {
                        ComplexNavigationCell currentWaypoint = (ComplexNavigationCell)waypoints[0];

                        currentTarget.Exists = false;

                        waypoints.RemoveAt(0);

                        if (waypoints.Count > 2)//Проверить, не стал ли путь до главной цели прямым и простым, чтобы не следовать ему больше
                        {
                            bool directPath = true;
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

                            if (directPath)
                            {
                                //Если путь прямой, несложный, то монстр может самостоятельно добраться до игрока, не используя маршрут, и атаковать его
                                Waypoints = null;
                                return;
                            }
                        }

                        if (waypoints.Count == 0)//Если маршрут кончился, перестать следовать ему
                        {
                            Waypoints = null;
                            return;
                        }
                        else
                        {
                            ComplexNavigationCell nextWaypoint = (ComplexNavigationCell)waypoints[0];
                            NeighborCellStruct neighborConnection = currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb);
                            //Продолжаем следование
                            currentTarget = new ETarget(nextWaypoint.cellPosition);
                            if (neighborConnection.connectionType == NavCellTypeEnum.jump && grounded)
                            {
                                //Перепрыгиваем препятствие
                                Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x)));
                                //transform.position = new Vector3(currentWaypoint.cellPosition.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                                Jump();
                            }
                        }
                    }
                }
            }

            #endregion //complexWay

        }
    }

    /// <summary>
    /// Поведение патрулирования
    /// </summary>
    protected override void PatrolBehavior()
    {
        Vector2 pos = transform.position;
        if (waypoints != null ? waypoints.Count > 0 : false)
        {
            if (!currentTarget.exists)
                currentTarget = new ETarget(waypoints[0].cellPosition);

            bool waypointIsAchieved = false;
            Vector2 targetPosition = currentTarget;
            Vector2 targetDistance = targetPosition - pos;
            
            if (Mathf.Abs(targetDistance.x) > navCellSize / 4f)
                Move((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(targetDistance.x)));
            else
            {
                transform.position = new Vector3(targetPosition.x, pos.y);
                StopMoving();
            }
            waypointIsAchieved = Vector2.SqrMagnitude(currentTarget - pos) < minCellSqrMagnitude;
              
            if (waypointIsAchieved)
            {
                ComplexNavigationCell currentWaypoint = (ComplexNavigationCell)waypoints[0];
                currentTarget.Exists = false;

                waypoints.RemoveAt(0);

                if (waypoints.Count == 0)//Если маршрут кончился, перестать следовать ему
                {
                    StopMoving();
                    //Достигли конца маршрута
                    if (Vector3.Distance(beginPosition, currentWaypoint.cellPosition) < navCellSize)
                    {
                        transform.position = beginPosition;
                        Turn(beginOrientation);
                        BecomeCalm();
                        return;
                    }
                    else
                        GoHome();//Никого в конце маршрута не оказалось, значит, возвращаемся домой
                }
                else
                {
                    ComplexNavigationCell nextWaypoint = (ComplexNavigationCell)waypoints[0];
                    NeighborCellStruct neighborConnection = currentWaypoint.GetNeighbor(nextWaypoint.groupNumb, nextWaypoint.cellNumb);
                    //Продолжаем следование
                    currentTarget = new ETarget(nextWaypoint.cellPosition);
                    if (neighborConnection.connectionType == NavCellTypeEnum.jump && grounded)
                    {
                        //Перепрыгиваем препятствие
                        Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(nextWaypoint.cellPosition.x - currentWaypoint.cellPosition.x)));
                        //transform.position = new Vector3(currentWaypoint.cellPosition.x + (int)orientation * navCellSize / 2f, pos.y, pos.z);
                        Jump();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Функция, заставляющая ИИ выдвигаться к заданной точке
    /// </summary>
    /// <param name="targetPosition">Точка, достичь которую стремится ИИ</param>
    protected override void GoToThePoint(Vector2 targetPosition)
    {
        if (navMap == null)
            return;
        if (!(navMap is NavigationBunchedMap))
        waypoints = ((NavigationBunchedMap)navMap).GetSimplePath(transform.position, targetPosition, true);
        if (waypoints == null)
            return;
        BecomePatrolling();
    }

    /// <summary>
    /// Функция, которая строит маршрут
    /// </summary>
    /// <param name="endPoint">точка назначения</param>
    ///<param name="maxDepth">Максимальная сложность маршрута</param>
    /// <returns>Навигационные ячейки, составляющие маршрут</returns>
    protected override List<NavigationCell> FindPath(Vector2 endPoint, int _maxDepth)
    {
        if (navMap == null || !(navMap is NavigationBunchedMap))
            return null;

        NavigationBunchedMap _map = (NavigationBunchedMap)navMap;
        List<NavigationCell> _path = new List<NavigationCell>();
        ComplexNavigationCell beginCell = (ComplexNavigationCell)_map.GetCurrentCell(transform.position), endCell = (ComplexNavigationCell)_map.GetCurrentCell(endPoint);

        if (beginCell == null || endCell == null)
            return null;
        prevTargetPosition = new EVector3(endPoint, true);

        int depthOrder = 0, currentDepthCount = 1, nextDepthCount = 0;
        //navMap.ClearMap();
        List<NavigationGroup> clearedGroups = new List<NavigationGroup>();//Список "очищенных групп" (со стёртой информации о посещённости ячеек)
        List<NavigationGroup> cellGroups = _map.cellGroups;
        NavigationGroup clearedGroup = cellGroups[beginCell.groupNumb];
        clearedGroup.ClearCells();
        clearedGroups.Add(clearedGroup);
        clearedGroup = cellGroups[endCell.groupNumb];
        if (!clearedGroups.Contains(clearedGroup))
        {
            clearedGroup.ClearCells();
            clearedGroups.Add(clearedGroup);
        }

        Queue<ComplexNavigationCell> cellsQueue = new Queue<ComplexNavigationCell>();
        cellsQueue.Enqueue(beginCell);
        beginCell.visited = true;
        while (cellsQueue.Count > 0 && endCell.fromCell == null)
        {
            ComplexNavigationCell currentCell = cellsQueue.Dequeue();
            if (currentCell == null)
                return null;
            List<ComplexNavigationCell> neighbourCells = currentCell.neighbors.ConvertAll<ComplexNavigationCell>(x => _map.GetCell(x.groupNumb, x.cellNumb));
            for (int i=0;i<neighbourCells.Count;i++)
            {
                ComplexNavigationCell cell = neighbourCells[i];
                if (cell.cellType == NavCellTypeEnum.movPlatform || currentCell.neighbors[i].connectionType == NavCellTypeEnum.ladder)
                    continue;
                if (cell.groupNumb != currentCell.groupNumb)
                {
                    clearedGroup = cellGroups[cell.groupNumb];
                    if (!clearedGroups.Contains(clearedGroup))
                    {
                        clearedGroup.ClearCells();
                        clearedGroups.Add(clearedGroup);
                    }
                }

                if (cell != null ? !cell.visited : false)
                {

                    cell.visited = true;
                    cellsQueue.Enqueue(cell);
                    cell.fromCell = currentCell;
                    nextDepthCount++;
                }
            }
            currentDepthCount--;
            if (currentDepthCount == 0)
            {
                //Если путь оказался состоящим из слишком большого количества узлов, то не стоит пользоваться этим маршрутом. 
                //Этот алгоритм поиска используется для создания коротких маршрутов, которые можно будет быстро поменять при необходимости. 
                //Эти маршруты используются в агрессивном состоянии, когда не должно быть такого, 
                //что ИИ обходит слишком большие дистанции, чтобы достичь игрока. Если такое случается, он должен ждать, чему соответствует несуществование подходящего маршрута
                depthOrder++;
                if (depthOrder == _maxDepth)
                    return null;
                currentDepthCount = nextDepthCount;
                nextDepthCount = 0;
            }
        }

        if (endCell.fromCell == null)//Невозможно достичь данной точки
            return null;

        //Восстановим весь маршрут с последней ячейки
        NavigationCell pathCell = endCell;
        _path.Insert(0, pathCell);
        while (pathCell.fromCell != null)
        {
            _path.Insert(0, pathCell.fromCell);
            pathCell = pathCell.fromCell;
        }

        #region optimize

        //Удалим все ненужные точки
        for (int i = 0; i < _path.Count - 2; i++)
        {
            ComplexNavigationCell checkPoint1 = (ComplexNavigationCell)_path[i], checkPoint2 = (ComplexNavigationCell)_path[i + 1];
            if (checkPoint1.cellType == NavCellTypeEnum.jump || checkPoint1.cellType == NavCellTypeEnum.movPlatform)
                continue;
            if (checkPoint1.cellType != checkPoint2.cellType)
                continue;
            Vector2 movDirection1 = (checkPoint2.cellPosition - checkPoint1.cellPosition).normalized;
            Vector2 movDirection2 = Vector2.zero;
            int index = i + 2;
            ComplexNavigationCell checkPoint3 = (ComplexNavigationCell)_path[index];
            while (Vector2.SqrMagnitude(movDirection1 - (checkPoint3.cellPosition - checkPoint2.cellPosition).normalized) < .01f &&
                   checkPoint1.cellType == checkPoint3.cellType &&
                   index < _path.Count)
            {
                index++;
                if (index < _path.Count)
                {
                    checkPoint2 = checkPoint3;
                    checkPoint3 = (ComplexNavigationCell)_path[index];
                }
            }
            for (int j = i + 1; j < index - 1; j++)
            {
                _path.RemoveAt(i + 1);
            }
        }

        #endregion //optimize

        return _path;
    }

    #endregion //behaviorActions

    #region optimization

    /// <summary>
    /// Функция, которая восстанавливает положение и состояние персонажа, пользуясь данными, полученными в оптимизированном режиме
    /// </summary>
    protected override void RestoreActivePosition()
    {
        if (!currentTarget.exists)
        {
            Turn(beginOrientation);
        }
        else
        {
            Vector2 pos = transform.position;
            Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(currentTarget.x - pos.x)));
        }
    }

    /// <summary>
    /// Функция, которая переносит персонажа в ту позицию, в которой он может нормально функционировать для ведения оптимизированной версии 
    /// </summary>
    protected override void GetOptimizedPosition()
    {
        StopAvoid();
        if (!grounded)
        {
            if (waypoints == null)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, navMap.mapSize.magnitude, LayerMask.GetMask(gLName));
                if (!hit)
                {
                    Death();
                }
                else
                {
                    transform.position = hit.point + Vector2.up * 0.02f;
                }
            }
        }
    }

    /// <summary>
    /// Процесс оптимизированного прохождения пути. Заключается в том, что персонаж, зная свой маршрут, появляется в его различиных позициях, не используя 
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator PathPassOptProcess()
    {
        followOptPath = true;
        if (waypoints == null && !currentTarget.exists)
        {
            if (Vector2.SqrMagnitude((Vector2)transform.position - beginPosition) < minCellSqrMagnitude)
                BecomeCalm();
            else
            {
                GoHome();
                if (waypoints == null)
                {
                    if (beginPosition.transform == null)
                    {
                        //Если не получается добраться до начальной позиции, то считаем, что текущая позиция становится начальной
                        beginPosition = new ETarget(transform.position);
                        beginOrientation = orientation;
                        BecomeCalm();
                        followOptPath = false;
                    }
                }
                else
                    StartCoroutine("PathPassOptProcess");
            }
        }
        else
        {
            while ((waypoints != null ? waypoints.Count > 0 : false) || currentTarget.exists)
            {
                if (!currentTarget.exists)
                    currentTarget = new ETarget(waypoints[0].cellPosition);

                Vector2 pos = transform.position;
                Vector2 targetPos = currentTarget;

                if (Vector2.SqrMagnitude(pos - targetPos) <= minCellSqrMagnitude)
                {
                    transform.position = targetPos;
                    currentTarget.Exists = false;
                    pos = transform.position;
                    if (waypoints != null ? waypoints.Count > 0 : false)
                    {
                        ComplexNavigationCell currentCell = (ComplexNavigationCell)waypoints[0];
                        waypoints.RemoveAt(0);
                        if (waypoints.Count <= 0)
                            break;
                        ComplexNavigationCell nextCell = (ComplexNavigationCell)waypoints[0];
                        currentTarget = new ETarget(nextCell.cellPosition);

                        NeighborCellStruct neighborConnection = currentCell.GetNeighbor(nextCell.groupNumb, nextCell.cellNumb);

                        if (neighborConnection.groupNumb != -1 )
                        {
                            transform.position = nextCell.cellPosition;
                            yield return new WaitForSeconds(optTimeStep);
                            continue;
                        }
                    }
                }
                if (currentTarget.exists)
                {
                    targetPos = currentTarget;
                    Vector2 direction = targetPos - pos;
                    transform.position = pos + direction.normalized * Mathf.Clamp(speed, 0f, direction.magnitude);
                }
                yield return new WaitForSeconds(optTimeStep);
            }
            waypoints = null;
            currentTarget.Exists = false;
            followOptPath = false;
        }
    }

    #endregion //optimization

}