using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#region map

/// <summary>
/// Класс, представляющиый собой навигационную карту уровня. Указывает мобам (определённого типа), как им надо передвигаться
/// </summary>
[System.Serializable]
public class NavigationMap
{

    #region fields

    public NavMapTypeEnum mapType;//Тип карты

    #endregion //fields

    #region parametres

    [HideInInspector]public Vector2 mapSize;//Размер карты уровня
    [HideInInspector]public Vector2 mapDownLeft;//Положение левого нижнего угла карты уровня
    [HideInInspector]public Vector2 cellSize;//Размер ячейки

    #endregion //parametres

    public NavigationMap(NavMapTypeEnum _mapType)
    {
        mapType = _mapType;
    }

    /// <summary>
    /// Пометить все ячейки на карте как непосещённые
    /// </summary>
    public virtual void ClearMap()
    {
    }

    /// <summary>
    /// Вернуть ячейку в которой находится данная целевая точка
    /// </summary>
    /// <param name="targetPos">Текущая позиция</param>
    /// <returns>Текущая ячейка</returns>
    public virtual NavigationCell GetCurrentCell(Vector2 targetPos)
    {
        return null;
    }

    /// <summary>
    /// Вернуть ячейку в которой находится данная целевая точка
    /// </summary>
    /// <param name="targetPos">Текущая позиция</param>
    /// <returns>Текущая ячейка</returns>
    public virtual NavigationCell GetCurrentCellInEditor(Vector2 targetPos)
    {
        return null;
    }

    /// <summary>
    /// Возвращает маршрут, используя информацию о карте. Вернёт null, если пути не существует
    /// </summary>
    /// <param name="beginPosition">Начальная позиция</param>
    /// <param name="endPosition">Конечная позиция</param>
    /// <param name="optimize">Оптимизировать ли маршрут?</param>
    /// <returns>Массив ячеек</returns>
    public virtual List<NavigationCell> GetPath(Vector2 beginPosition, Vector2 endPosition, bool optimize)
    {
        return null;
    }

}

/// <summary>
/// Навигационная карта, что содержит информацию в списках навигационных групп
/// Используется нелетающими монстрами
/// </summary>
[System.Serializable]
public class NavigationBunchedMap: NavigationMap
{

    #region dictionaries

    public Dictionary<NavigationCellIndex, NavigationGroup> groupDictionary = new Dictionary<NavigationCellIndex, NavigationGroup>();

    #endregion //dictionaries

    #region fields

    public List<NavigationGroup> cellGroups = new List<NavigationGroup>();//Группы клеток, что входять в данную карту

    #endregion //fields

    #region parametres

    [HideInInspector]public Vector2 groupSize;//Размер группы

    #endregion //parametres

    public NavigationBunchedMap(NavMapTypeEnum _mapType): base(_mapType)
    {
        cellGroups = new List<NavigationGroup>();
    }

    /// <summary>
    /// Создать группы ячеек, используя информацию о размере и положении всей карты и о дефолтном размере групп 
    /// </summary>
    /// <param name="downLeft">Положение нижней левой точки карты</param>
    /// <param name="_mapSize">Размер карты</param>
    /// <param name="_groupSize">Размер части карты, соответствующей группе ячеек</param>
    public void CreateGroups(Vector2 downLeft, Vector2 _mapSize, Vector2 _groupSize)
    {
        cellGroups = new List<NavigationGroup>();
        float posX = downLeft.x, posY = downLeft.y;
        Vector2 upRight = new Vector2(downLeft.x + _mapSize.x, downLeft.y + _mapSize.y);
        while (posY < upRight.y)
        {
            posX = downLeft.x;
            while (posX < upRight.x)
            {
                NavigationGroup navGroup = new NavigationGroup();
                navGroup.SetSize(new Vector2(posX, posY), new Vector2(posX + _groupSize.x, posY + _groupSize.y));
                cellGroups.Add(navGroup);
                posX += _groupSize.x;
            }
            posY += _groupSize.y;
        }
    }

    /// <summary>
    /// Проверить группы ячеек на наличие этих самых ячеек. Если ячеек нет, удалить группу. Так же пронумеровать все ячейки
    /// </summary>
    public void CheckGroups()
    {
        for (int i = 0; i < cellGroups.Count; i++)
        {
            if (cellGroups[i].cells.Count == 0)
            {
                cellGroups.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < cellGroups.Count; i++)
        {
            for (int j = 0; j < cellGroups[i].cells.Count; j++)
            {
                ComplexNavigationCell _cell = cellGroups[i].cells[j];
                _cell.cellNumb = j;
                _cell.groupNumb = i;
            }
        }
    }

    /// <summary>
    /// Создать словарь навигационных групп
    /// </summary>
    public void MakeDictionary()
    {
        groupDictionary = new Dictionary<NavigationCellIndex, NavigationGroup>();
        foreach (NavigationGroup navGroup in cellGroups)
        {
            Vector2 _pos = (navGroup.upRight + navGroup.downLeft) / 2f;
            NavigationCellIndex navIndex = new NavigationCellIndex(new Vector2((_pos - mapDownLeft).x / groupSize.x, (_pos - mapDownLeft).y / groupSize.y));
            if (!groupDictionary.ContainsKey(navIndex))
            {
                groupDictionary.Add(navIndex, navGroup);
                navGroup.MakeDictionary(cellSize);
            }
        }
    }

    /// <summary>
    /// Пересчитать размеры всех групп
    /// </summary>
    /// <param name="cellSize">размер клетки</param>
    public void ResetGroupSizes(Vector2 _cellSize)
    {
        //foreach (NavigationGroup navGroup in cellGroups)
        //navGroup.SetSize(cellSize);
    }

    /// <summary>
    /// Пометить все ячейки на карте как непосещённые
    /// </summary>
    public override void ClearMap()
    {
        foreach (NavigationGroup navGroup in cellGroups)
            navGroup.ClearCells();
    }

    /// <summary>
    /// Определить в какой группе ячеек находится целевая точка
    /// </summary>
    /// <param name="targetPos">Целевая позиция</param>
    /// <returns>Текущая группа ячеек</returns>
    public NavigationGroup GetCurrentGroup(Vector2 targetPos)
    {
        if (groupDictionary != null)
        {
            NavigationCellIndex navIndex = new NavigationCellIndex(new Vector2((targetPos - mapDownLeft).x / groupSize.x, (targetPos - mapDownLeft).y / groupSize.y));
            if (!groupDictionary.ContainsKey(navIndex))
                return null;
            else
                return groupDictionary[navIndex];
        }
        else
        {
            NavigationGroup _navGroup = null;
            foreach (NavigationGroup navGroup in cellGroups)
            {
                if (navGroup.ContainsVector(targetPos))
                {
                    _navGroup = navGroup;
                    break;
                }
            }
            return _navGroup;
        }
    }

    /// <summary>
    /// Определить в какой группе ячеек находится целевая точка
    /// </summary>
    /// <param name="targetPos">Целевая позиция</param>
    /// <returns>Текущая группа ячеек</returns>
    public NavigationGroup GetCurrentGroupInEditor(Vector2 targetPos)
    {
        NavigationGroup _navGroup = null;
        foreach (NavigationGroup navGroup in cellGroups)
        {
            if (navGroup.ContainsVector(targetPos))
            {
                _navGroup = navGroup;
                break;
            }
        }
        return _navGroup;
    }

    /// <summary>
    /// Вернуть ячейку в которой находится данная целевая точка
    /// </summary>
    /// <param name="targetPos">Текущая позиция</param>
    /// <returns>Текущая ячейка</returns>
    public override NavigationCell GetCurrentCell(Vector2 targetPos)
    {
        ComplexNavigationCell _cell = null;
        NavigationGroup _group = GetCurrentGroup(targetPos);
        if (_group != null)
            _cell = _group.GetCurrentCell(targetPos, cellSize);
        if (_cell == null)
        {
            for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    Vector2 _pos = targetPos + new Vector2(i * cellSize.x, j * cellSize.y);
                    _group = GetCurrentGroup(_pos);
                    if (_group != null)
                        _cell = _group.GetCurrentCell(_pos, cellSize);
                    if (_cell != null)
                        return _cell;
                }
        }
        return _cell;
    }

    /// <summary>
    /// Вернуть ячейку в которой находится данная целевая точка
    /// </summary>
    /// <param name="targetPos">Текущая позиция</param>
    /// <returns>Текущая ячейка</returns>
    public override NavigationCell GetCurrentCellInEditor(Vector2 targetPos)
    {
        ComplexNavigationCell _cell = null;
        NavigationGroup _group = GetCurrentGroupInEditor(targetPos);
        if (_group != null)
            _cell = _group.GetCurrentCellInEditor(targetPos);
        return _cell;
    }

    /// <summary>
    /// Найти ячейку с заданными идентификационными номерами
    /// </summary>
    /// <param name="groupNumb">номер группы</param>
    /// <param name="cellNumb">номер ячейки</param>
    /// <returns>искомая ячейка</returns>
    public ComplexNavigationCell GetCell(int groupNumb, int cellNumb)
    {
        if (groupNumb >= cellGroups.Count)
            return null;
        NavigationGroup currentGroup = cellGroups[groupNumb];
        if (cellNumb >= currentGroup.cells.Count)
            return null;
        return currentGroup.cells[cellNumb];
    }

    /// <summary>
    /// Возвращает маршрут, используя информацию о карте. Вернёт null, если пути не существует
    /// </summary>
    /// <param name="beginPosition">Начальная позиция</param>
    /// <param name="endPosition">Конечная позиция</param>
    /// <param name="optimize">Оптимизировать ли маршрут?</param>
    /// <returns>Массив ячеек</returns>
    public override List<NavigationCell> GetPath(Vector2 beginPosition, Vector2 endPosition, bool optimize)
    {
        List<NavigationCell> _path = new List<NavigationCell>();
        ComplexNavigationCell beginCell = (ComplexNavigationCell)GetCurrentCell(beginPosition), endCell = (ComplexNavigationCell)GetCurrentCell(endPosition);

        if (beginCell == null || endCell == null)
            return null;

        //ClearMap();
        List<NavigationGroup> clearedGroups = new List<NavigationGroup>();//Список "очищенных групп" (со стёртой информации о посещённости ячеек)
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
            List<ComplexNavigationCell> neighbourCells = currentCell.neighbors.ConvertAll<ComplexNavigationCell>(x => GetCell(x.groupNumb, x.cellNumb));
            foreach (ComplexNavigationCell cell in neighbourCells)
            {
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
                }
            }
        }

        if (endCell.fromCell == null)//Невозможно достичь данной точки
            return null;

        //Восстановим весь маршрут с последней ячейки
        ComplexNavigationCell pathCell = endCell;
        _path.Insert(0, pathCell);
        while (pathCell.fromCell != null)
        {
            _path.Insert(0, pathCell.fromCell);
            pathCell = (ComplexNavigationCell)pathCell.fromCell;
        }

        if (optimize)
        {

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
        }

        return _path;
    }

    /// <summary>
    /// Возвращает маршрут, используя информацию о карте. Вернёт null, если пути не существует. Возвращаемый путь не содержит пути по лестницам и платформам
    /// </summary>
    /// <param name="beginPosition">Начальная позиция</param>
    /// <param name="endPosition">Конечная позиция</param>
    /// <param name="optimize">Оптимизировать ли маршрут?</param>
    /// <returns>Массив ячеек</returns>
    public virtual List<NavigationCell> GetSimplePath(Vector2 beginPosition, Vector2 endPosition, bool optimize)
    {
        List<NavigationCell> _path = new List<NavigationCell>();
        ComplexNavigationCell beginCell = (ComplexNavigationCell)GetCurrentCell(beginPosition), endCell = (ComplexNavigationCell)GetCurrentCell(endPosition);

        if (beginCell == null || endCell == null)
            return null;

        //ClearMap();
        List<NavigationGroup> clearedGroups = new List<NavigationGroup>();//Список "очищенных групп" (со стёртой информации о посещённости ячеек)
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
            List<ComplexNavigationCell> neighbourCells = currentCell.neighbors.ConvertAll<ComplexNavigationCell>(x => GetCell(x.groupNumb, x.cellNumb));
            for (int i=0;i<neighbourCells.Count;i++)
            {
                ComplexNavigationCell cell = neighbourCells[i];
                if (cell.cellType == NavCellTypeEnum.movPlatform || currentCell.neighbors[i].connectionType == NavCellTypeEnum.ladder)
                    continue;//Не рассматриваем клетки платформ и лестничные пути
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
                }
            }
        }

        if (endCell.fromCell == null)//Невозможно достичь данной точки
            return null;

        //Восстановим весь маршрут с последней ячейки
        ComplexNavigationCell pathCell = endCell;
        _path.Insert(0, pathCell);
        while (pathCell.fromCell != null)
        {
            _path.Insert(0, pathCell.fromCell);
            pathCell = (ComplexNavigationCell)pathCell.fromCell;
        }

        if (optimize)
        {

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
        }

        return _path;
    }

}


/// <summary>
/// Класс, представляющий собой карту, данные в которой хранятся в прямоугоьной матрице из простых навигационных клеток.
/// Предназначен для использования летающими существами
/// </summary>
[System.Serializable]
public class NavigationMatrixMap : NavigationMap
{

    #region fields

    public List<NavigationCellRow> cellRows=new List<NavigationCellRow>();//Прямоугольный массив из навигационных клеток

    #endregion //fields

    #region parametres

    public int cellRowSize = 0;//Размер ряда ячеек
    public int cellColumnSize = 0;//Размер колонки ячеек

    #endregion //parametres

    public NavigationMatrixMap(NavMapTypeEnum _mapType) : base(_mapType)
    {
        cellRows = new List<NavigationCellRow>();
    }

    /// <summary>
    /// Создаёт массив навигационных клеток, которые сответствует заданному размеру навигационных клеток и навигационной карты
    /// </summary>
    /// <param name="cellSize">Размер навигационной клетки</param>
    public void CreateCells()
    {
        cellColumnSize = Mathf.CeilToInt(mapSize.y / cellSize.y);
        cellRowSize = Mathf.CeilToInt(mapSize.x / cellSize.x);
        cellRows = new List<NavigationCellRow>();
        for ( int i=0;i< cellColumnSize;i++)
        {
            cellRows.Add(new NavigationCellRow());
            for (int j = 0;j < cellRowSize; j++)
            {
                cellRows[i].cells.Add(new SimpleNavigationCell(mapDownLeft + new Vector2((j + 0.5f) * cellSize.x, (i + 0.5f) * cellSize.y)));
            }
        }
    }

    /// <summary>
    /// Пометить все ячейки на карте как непосещённые
    /// </summary>
    public override void ClearMap()
    {
        foreach (NavigationCellRow row in cellRows)
            foreach (SimpleNavigationCell cell in row.cells)
                cell.ClearCell();
    }

    /// <summary>
    /// Вернуть ячейку в которой находится данная целевая точка
    /// </summary>
    /// <param name="targetPos">Текущая позиция</param>
    /// <returns>Текущая ячейка</returns>
    public override NavigationCell GetCurrentCell(Vector2 targetPos)
    {
        SimpleNavigationCell currentCell = null;
        int indexY = Mathf.FloorToInt((targetPos.y - mapDownLeft.y) / cellSize.y);
        int indexX= Mathf.FloorToInt((targetPos.x - mapDownLeft.x) / cellSize.x);
        SimpleNavigationCell _cell = cellRows[indexY].cells[indexX];
        if (_cell.canMove)
            currentCell = _cell;
        else
        {
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    _cell = cellRows[indexY+i].cells[indexX+j];
                    if (_cell.canMove)
                    {
                        currentCell = _cell;
                        break;
                    }
                }
            }
        }
        return currentCell;
    }

    /// <summary>
    /// Вернуть ячейку в которой находится данная целевая точка
    /// </summary>
    /// <param name="targetPos">Текущая позиция</param>
    /// <returns>Текущая ячейка</returns>
    public override NavigationCell GetCurrentCellInEditor(Vector2 targetPos)
    {
        int indexY = Mathf.FloorToInt((targetPos.y - mapDownLeft.y) / cellSize.y);
        int indexX = Mathf.FloorToInt((targetPos.x - mapDownLeft.x) / cellSize.x);
        return cellRows[indexY].cells[indexX];
    }

    /// <summary>
    /// Возвращает маршрут, используя информацию о карте. Вернёт null, если пути не существует
    /// </summary>
    /// <param name="beginPosition">Начальная позиция</param>
    /// <param name="endPosition">Конечная позиция</param>
    /// <param name="optimize">Оптимизировать ли маршрут?</param>
    /// <returns>Массив ячеек</returns>
    public override List<NavigationCell> GetPath(Vector2 beginPosition, Vector2 endPosition, bool optimize)
    {
        List<NavigationCell> _path = new List<NavigationCell>();
        SimpleNavigationCell beginCell = (SimpleNavigationCell)GetCurrentCell(beginPosition), endCell = (SimpleNavigationCell)GetCurrentCell(endPosition);
        
        if (beginCell == null || endCell == null)
            return null;

        Queue<SimpleNavigationCell> cellsQueue = new Queue<SimpleNavigationCell>();
        List<SimpleNavigationCell> checkedCells = new List<SimpleNavigationCell>();
        cellsQueue.Enqueue(beginCell);
        beginCell.visited = true;
        checkedCells.Add(beginCell);
        while (cellsQueue.Count > 0 && endCell.fromCell == null)
        {
            SimpleNavigationCell currentCell = cellsQueue.Dequeue();
            if (currentCell == null)
                continue;
            int indexY = Mathf.FloorToInt((currentCell.cellPosition.y - mapDownLeft.y) / cellSize.y);
            int indexX = Mathf.FloorToInt((currentCell.cellPosition.x - mapDownLeft.x) / cellSize.x);
            //Не буду делать проверку на граничные условия, так как они не могут быть достигнуты (предположим, что никогда не будет рассматриваться клетка на краю карты)
            for (int i = -1; i < 2; i++)
            {
                for (int j =-1;j<2;j++)
                {
                    SimpleNavigationCell _cell = cellRows[indexY+i].cells[indexX+j];
                    if (!_cell.visited && _cell.canMove)
                    {
                        _cell.visited = true;
                        cellsQueue.Enqueue(_cell);
                        _cell.fromCell = currentCell;
                        checkedCells.Add(_cell);
                    }
                }
            }
        }


        if (endCell.fromCell == null)//Невозможно достичь данной точки
        {
            foreach (SimpleNavigationCell _cell in checkedCells)
            {
                _cell.ClearCell();
            }
            return null;
        }

        //Восстановим весь маршрут с последней ячейки
        SimpleNavigationCell pathCell = endCell;
        _path.Insert(0, pathCell);
        while (pathCell.fromCell != null)
        {
            _path.Insert(0, pathCell.fromCell);
            pathCell = (SimpleNavigationCell)pathCell.fromCell;
        }

        foreach (SimpleNavigationCell _cell in checkedCells)
        {
            _cell.ClearCell();
        }

        if (optimize)
        {

            //Удалим все ненужные точки
            for (int i = 0; i < _path.Count - 2; i++)
            {
                NavigationCell checkPoint1 = _path[i], checkPoint2 = _path[i + 1];
                Vector2 movDirection1 = (checkPoint2.cellPosition - checkPoint1.cellPosition).normalized;
                Vector2 movDirection2 = Vector2.zero;
                int index = i + 2;
                NavigationCell checkPoint3 = _path[index];
                while (Vector2.SqrMagnitude(movDirection1 - (checkPoint3.cellPosition - checkPoint2.cellPosition).normalized) < .01f &&
                        index < _path.Count)
                {
                    index++;
                    if (index < _path.Count)
                    {
                        checkPoint2 = checkPoint3;
                        checkPoint3 = _path[index];
                    }
                }
                for (int j = i + 1; j < index - 1; j++)
                {
                    _path.RemoveAt(i + 1);
                }
            }
        }

        return _path;
    }

}


#endregion //map

/// <summary>
/// Класс, представляющий собой группу из навигационных ячеек.
/// Разделение по группам бывает разным, например, разделять ячейки можно по местоположению или по типу.
/// Этот класс используется для оптимизации. Такие группы составляют карту для нелетающих монстров
/// </summary>
[System.Serializable]
public class NavigationGroup
{

    #region dictionaries

    protected Dictionary<NavigationCellIndex, ComplexNavigationCell> cellDictionary = new Dictionary<NavigationCellIndex, ComplexNavigationCell>();

    #endregion //dictionaries

    #region fields

    [SerializeField]public List<ComplexNavigationCell> cells = new List<ComplexNavigationCell>();//Навигационные клетки, что составляют группу
    public Vector2 downLeft, upRight;//Позиции левого нижнего и правого верхнего углов прямоугольника, который содержит в себя данную группу ячеек

    #endregion //fields

    public NavigationGroup()
    {
        cells = new List<ComplexNavigationCell>();
    }

    /// <summary>
    /// Установить размер прямоугольника, содержащего все ячейки этой группы
    /// </summary>
    /// <param name="cellSize">размер одной ячейки</param>
    public void SetSize(Vector2 cellSize)
    {
        float minX = Mathf.Infinity, maxX = Mathf.NegativeInfinity, minY = Mathf.Infinity, maxY = Mathf.NegativeInfinity;

        foreach (ComplexNavigationCell cell in cells)
        {
            if (cell.cellPosition.x - cellSize.x / 2f < minX)
                minX = cell.cellPosition.x - cellSize.x / 2f;
            if (cell.cellPosition.x + cellSize.x / 2f > maxX)
                maxX = cell.cellPosition.x + cellSize.x / 2f;
            if (cell.cellPosition.y - cellSize.y / 2f < minY)
                minY = cell.cellPosition.y - cellSize.y / 2f;
            if (cell.cellPosition.y + cellSize.y / 2f > maxY)
                maxY = cell.cellPosition.y + cellSize.y / 2f;
        }
        downLeft = new Vector2(minX, minY);
        upRight = new Vector2(maxX, maxY);
    }

    /// <summary>
    /// Установить размер прямоугольника, содержащего все ячейки этой группы
    /// </summary>
    /// <param name="_downLeft">Новый нижний левый угол</param>
    /// <param name="_upRight">Новый правый верхний угол</param>
    public void SetSize(Vector2 _downLeft, Vector2 _upRight)
    {
        downLeft = _downLeft;
        upRight = _upRight;
    }

    /// <summary>
    /// Проверка на содержание данной группы ячеек рассматриваемой точки
    /// </summary>
    /// <param name="targetPos">целевая точка</param>
    /// <returns></returns>
    public bool ContainsVector(Vector2 targetPos)
    {
        return (downLeft.x <= targetPos.x && downLeft.y <= targetPos.y && upRight.x >= targetPos.x && upRight.y >= targetPos.y);
    }

    /// <summary>
    /// Составить словарь из списка ячеек
    /// </summary>
    public void MakeDictionary(Vector2 cellSize)
    {
        cellDictionary = new Dictionary<NavigationCellIndex, ComplexNavigationCell>();
        foreach (ComplexNavigationCell navCell in cells)
        {
            NavigationCellIndex navIndex = new NavigationCellIndex(new Vector2((navCell.cellPosition - downLeft).x / cellSize.x, (navCell.cellPosition - downLeft).y / cellSize.y));
            if (!cellDictionary.ContainsKey(navIndex))
                cellDictionary.Add(navIndex, navCell);
        }
    }

    /// <summary>
    /// Вернуть ближайшую к целевой точке ячейку из группы
    /// </summary>
    /// <param name="targetPos">целевая точка</param>
    /// <returns></returns>
    public ComplexNavigationCell GetCurrentCell(Vector2 targetPos, Vector2 cellSize)
    {
        if (cellDictionary != null)
        {
            NavigationCellIndex navIndex = new NavigationCellIndex(new Vector2((targetPos - downLeft).x / cellSize.x, (targetPos - downLeft).y / cellSize.y));
            if (!cellDictionary.ContainsKey(navIndex))
                return null;
            else
                return cellDictionary[navIndex];
        }
        else
        {
            float minDist = Mathf.Infinity;
            ComplexNavigationCell _cell=null;
            foreach (ComplexNavigationCell cell in cells)
            {
                if (Vector2.SqrMagnitude(targetPos - cell.cellPosition) < minDist)
                {
                    minDist = Vector2.SqrMagnitude(targetPos - cell.cellPosition);
                    _cell = cell;
                }
            }

            return _cell;
        }
    }

    /// <summary>
    /// Вернуть ближайшую к целевой точке ячейку из группы
    /// </summary>
    /// <param name="targetPos">целевая точка</param>
    /// <returns></returns>
    public ComplexNavigationCell GetCurrentCellInEditor(Vector2 targetPos)
    {
        float minDist = Mathf.Infinity;
        ComplexNavigationCell _cell=null;
        foreach (ComplexNavigationCell cell in cells)
        {
            if (Vector2.SqrMagnitude(targetPos - cell.cellPosition) < minDist)
            {
                minDist = Vector2.SqrMagnitude(targetPos - cell.cellPosition);
                _cell = cell;
            }
        }

        return _cell;
    }

    /// <summary>
    /// Установить, что все клетки рассматриваемой группы посещены
    /// </summary>
    public void ClearCells()
    {
        foreach (ComplexNavigationCell cell in cells)
            cell.ClearCell();
    }
    
}

#region cells

/// <summary>
/// Базовый класс для различных видов навигационных ячеек. Именно они являются простейшими элементами навигационного маршрута
/// </summary>
[System.Serializable]
public class NavigationCell
{

    #region fields

    public Vector2 cellPosition;//Координаты центра ячейки

    [NonSerialized][HideInInspector]public bool visited = false;//Была ли посещена данная ячейка (используется в алгоритмах поиска путей)
    [NonSerialized][HideInInspector]public NavigationCell fromCell = null;//Из какой клетки мы пришли в данную (используется в алгоритмах поиска путей)

    #endregion //fields

    public NavigationCell(Vector2 _cellPosition)
    {
        cellPosition = _cellPosition;
        visited = false;
        fromCell = null;
    }

    /// <summary>
    /// Очистить клетку (пометить, что она непосещённая)
    /// </summary>
    public virtual void ClearCell()
    {
        visited = false;
        fromCell = null;
    }

}

/// <summary>
/// Простая навигационная клетка. Помимо базовых данных эта клетка содержит информацию о том, можно ли впринципе посетить клетку
/// Такие клетки используются летающими монстрами
/// </summary>
[System.Serializable]
public class SimpleNavigationCell : NavigationCell
{

    #region fields

    public bool canMove;//Можно ли посетить данную клетку

    #endregion //fields

    public SimpleNavigationCell(Vector2 _position): base(_position)
    {
        canMove = false;
    }

}

/// <summary>
/// Класс, представляющий ряд простых навигационных клеток. Используется для сериализации прямоугольного массив навигационных клеток
/// </summary>
[System.Serializable]
public class NavigationCellRow
{

    #region fields

    public List<SimpleNavigationCell> cells=new List<SimpleNavigationCell>();

    #endregion //fields

    public NavigationCellRow()
    {
        cells = new List<SimpleNavigationCell>();
    }

}

/// <summary>
/// Сложная навигационная клетка. Такие клетки используются нелетающими мобами, которые в своём навигационном маршруте могут иметь сложные участки, 
/// которые можно описать только более сложными, чем NavigationCell и SimpleNavigationCell, конструкциями
/// </summary>
[System.Serializable]
public class ComplexNavigationCell : NavigationCell
{

    #region fields

    public int id = -1;//ID клетки
    public NavCellTypeEnum cellType;//Тип клетки

    [HideInInspector][SerializeField]
    public List<NeighborCellStruct> neighbors = new List<NeighborCellStruct>();//Соседние клетки, в которые можно прийти из данной ячейки

    public int cellNumb, groupNumb;

    #endregion //fields

    public ComplexNavigationCell(Vector2 _cellPosition):base(_cellPosition)
    {
        cellType = NavCellTypeEnum.usual;
        neighbors = new List<NeighborCellStruct>();
    }

    public ComplexNavigationCell(Vector2 _cellPosition, NavCellTypeEnum _cellType):base(_cellPosition)
    {
        cellType = _cellType;
        neighbors = new List<NeighborCellStruct>();
    }

    /// <summary>
    /// Возвращает соседа, соответствующего указанной навигационной клетке
    /// </summary>
    /// <returns>Возвращает соответствующую клетке структуру NeighborCellStruct</returns>
    public NeighborCellStruct GetNeighbor(int _groupNumb, int _cellNumb)
    {
        foreach (NeighborCellStruct neighbor in neighbors)
            if (neighbor.cellNumb == _cellNumb && neighbor.groupNumb == _groupNumb)
                return neighbor;
        return new NeighborCellStruct(-1, -1, NavCellTypeEnum.usual);
    }

}

/*
/// <summary>
/// Особый тип навигационных клеток, содержащих информацию об используемых платформах
/// </summary>
[System.Serializable]
public class PlatformNavigationCell: NavigationCell
{
    public int platformID;//идентификатор платформы, которая связана с рассматриваемой навигационной клеткой

    public PlatformNavigationCell(Vector2 _cellPosition, NavCellTypeEnum _cellType): base(_cellPosition,_cellType)
    {
        platformID = 0;
    }

    public PlatformNavigationCell(Vector2 _cellPosition, NavCellTypeEnum _cellType, int _platformID):base(_cellPosition,_cellType)
    {
        platformID = _platformID;
    }

}*/

/// <summary>
/// Структура, содержащая информацию о соседней клетки
/// </summary>
[System.Serializable]
public struct NeighborCellStruct
{
    public int groupNumb;
    public int cellNumb;

    public NavCellTypeEnum connectionType;

    public NeighborCellStruct(int _groupNumb, int _cellNumb, NavCellTypeEnum _connectionType)
    {
        groupNumb = _groupNumb;
        cellNumb = _cellNumb;
        connectionType = _connectionType;
    }

}

/// <summary>
/// Двойной индекс для идентификации ячейки в словаре
/// </summary>
public struct NavigationCellIndex
{
    public int indexX;
    public int indexY;

    public NavigationCellIndex(int _indexX, int _indexY)
    {
        indexX = _indexX;
        indexY = _indexY;
    }

    public NavigationCellIndex(Vector2 position)
    {
        indexX = Mathf.CeilToInt(position.x);
        indexY = Mathf.CeilToInt(position.y);
    }

}

#endregion //cells