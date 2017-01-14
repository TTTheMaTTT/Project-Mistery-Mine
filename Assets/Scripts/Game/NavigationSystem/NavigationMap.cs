using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, представляющиый собой навигационную карту уровня. Указывает мобам (определённого типа), как им надо передвигаться
/// </summary>
[System.Serializable]
public class NavigationMap
{

    #region dictionaries

    public Dictionary<NavigationCellIndex, NavigationGroup> groupDictionary = new Dictionary<NavigationCellIndex, NavigationGroup>();

    #endregion //dictionaries

    #region fields

    public List<NavigationGroup> cellGroups = new List<NavigationGroup>();//Группы клеток, что входять в данную карту
    public NavMapTypeEnum mapType;//Тип карты

    #endregion //fields

    #region parametres

    [HideInInspector]public Vector2 mapSize;//Размер карты уровня
    [HideInInspector]public Vector2 mapDownLeft;//Положение левого нижнего угла карты уровня
    [HideInInspector]public Vector2 cellSize;//Размер ячейки
    [HideInInspector]public Vector2 groupSize;//Размер группы

    #endregion //parametres

    public NavigationMap(NavMapTypeEnum _mapType)
    {
        mapType = _mapType;
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
                NavigationCell _cell = cellGroups[i].cells[j];
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
        foreach (NavigationGroup navGroup  in cellGroups)
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
    public void ClearMap()
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
    public NavigationCell GetCurrentCell(Vector2 targetPos)
    {
        NavigationCell _cell = null;
        NavigationGroup _group = GetCurrentGroup(targetPos);
        if (_group != null)
            _cell = _group.GetCurrentCell(targetPos, cellSize);
        if (_cell==null)
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
    public NavigationCell GetCurrentCellInEditor(Vector2 targetPos)
    {
        NavigationCell _cell = null;
        NavigationGroup _group = GetCurrentGroupInEditor(targetPos);
        if (_group != null)
            _cell = _group.GetCurrentCellInEditor(targetPos);
        return _cell;
    }

    /// <summary>
    /// НАйти ячейку с заданными идентификационными номерами
    /// </summary>
    /// <param name="groupNumb">номер группы</param>
    /// <param name="cellNumb">номер ячейки</param>
    /// <returns>искомая ячейка</returns>
    public NavigationCell GetCell(int groupNumb, int cellNumb)
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
    public List<NavigationCell> GetPath(Vector2 beginPosition, Vector2 endPosition, bool optimize)
    {
        List<NavigationCell> _path = new List<NavigationCell>();
        NavigationCell beginCell = GetCurrentCell(beginPosition), endCell=GetCurrentCell(endPosition);

        if (beginCell == null || endCell == null)
            return null;

        //ClearMap();
        List<NavigationGroup> clearedGroups=new List<NavigationGroup>();//Список "очищенных групп" (со стёртой информации о посещённости ячеек)
        NavigationGroup clearedGroup = cellGroups[beginCell.groupNumb];
        clearedGroup.ClearCells();
        clearedGroups.Add(clearedGroup);
        clearedGroup = cellGroups[endCell.groupNumb];
        if (!clearedGroups.Contains(clearedGroup))
        {
            clearedGroup.ClearCells();
            clearedGroups.Add(clearedGroup);
        }

        Queue<NavigationCell> cellsQueue = new Queue<NavigationCell>();
        cellsQueue.Enqueue(beginCell);
        beginCell.visited = true;
        while (cellsQueue.Count > 0 && endCell.fromCell==null)
        {
            NavigationCell currentCell = cellsQueue.Dequeue();
            if (currentCell == null)
                return null;
            List<NavigationCell> neighbourCells = currentCell.neighbors.ConvertAll<NavigationCell>(x => GetCell(x.groupNumb, x.cellNumb));
            foreach (NavigationCell cell in neighbourCells)
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
                if (cell!=null?!cell.visited:false)
                {
                    cell.visited = true;
                    cellsQueue.Enqueue(cell);
                    cell.fromCell = currentCell;
                }
            }
        }

        if (endCell.fromCell==null)//Невозможно достичь данной точки
            return null;

        //Восстановим весь маршрут с последней ячейки
        NavigationCell pathCell = endCell;
        _path.Insert(0, pathCell);
        while (pathCell.fromCell != null)
        {
            _path.Insert(0, pathCell.fromCell);
            pathCell = pathCell.fromCell;
        }

        if (optimize)
        {

            //Удалим все ненужные точки
            for (int i = 0; i < _path.Count-2; i++)
            {
                NavigationCell checkPoint1 = _path[i], checkPoint2 = _path[i + 1];
                if (checkPoint1.cellType == NavCellTypeEnum.jump || checkPoint1.cellType==NavCellTypeEnum.movPlatform)
                    continue;
                if (checkPoint1.cellType != checkPoint2.cellType)
                    continue;
                Vector2 movDirection1 = (checkPoint2.cellPosition - checkPoint1.cellPosition).normalized;
                Vector2 movDirection2 = Vector2.zero;
                int index = i + 2;
                NavigationCell checkPoint3 = _path[index];
                while (Vector2.SqrMagnitude(movDirection1 - (checkPoint3.cellPosition - checkPoint2.cellPosition).normalized) < .01f &&
                       checkPoint1.cellType == checkPoint3.cellType &&
                       index < _path.Count)
                {
                    index++;
                    if (index < _path.Count)
                    {
                        checkPoint2 = checkPoint3;
                        checkPoint3 = _path[index];
                    }
                }
                for (int j = i + 1; j < index-1; j++)
                {
                    _path.RemoveAt(i + 1);
                }
            }
        }

        return _path;
    }

}

/// <summary>
/// Класс, представляющий собой группу из навигационных ячеек.
/// Разделение по группам бывает разным, например, разделять ячейки можно по местоположению или по типу.
/// Этот класс используется для оптимизации
/// </summary>
[System.Serializable]
public class NavigationGroup
{

    #region dictionaries

    protected Dictionary<NavigationCellIndex, NavigationCell> cellDictionary = new Dictionary<NavigationCellIndex, NavigationCell>();

    #endregion //dictionaries

    #region fields

    [SerializeField]public List<NavigationCell> cells = new List<NavigationCell>();
    public Vector2 downLeft, upRight;//Позиции левого нижнего и правого верхнего углов прямоугольника, который содержит в себя данную группу ячеек

    #endregion //fields

    public NavigationGroup()
    {
        cells = new List<NavigationCell>();
    }

    /// <summary>
    /// Установить размер прямоугольника, содержащего все ячейки этой группы
    /// </summary>
    /// <param name="cellSize">размер одной ячейки</param>
    public void SetSize(Vector2 cellSize)
    {
        float minX = Mathf.Infinity, maxX = Mathf.NegativeInfinity, minY = Mathf.Infinity, maxY = Mathf.NegativeInfinity;

        foreach (NavigationCell cell in cells)
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
        cellDictionary = new Dictionary<NavigationCellIndex, NavigationCell>();
        foreach (NavigationCell navCell in cells)
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
    public NavigationCell GetCurrentCell(Vector2 targetPos, Vector2 cellSize)
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
            NavigationCell _cell=null;
            foreach (NavigationCell cell in cells)
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
    public NavigationCell GetCurrentCellInEditor(Vector2 targetPos)
    {
        float minDist = Mathf.Infinity;
        NavigationCell _cell=null;
        foreach (NavigationCell cell in cells)
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
        foreach (NavigationCell cell in cells)
            cell.ClearCell();
    }
    
}

#region cells

/// <summary>
/// Класс, представляющий собой навигационную ячейку. Мобы перемещаются между ячейками, поэтому нужно знать, как они связаны
/// </summary>
[System.Serializable]
public class NavigationCell
{

    #region fields

    public int id=-1;//ID клетки
    public Vector2 cellPosition;//Координаты центра ячейки
    public NavCellTypeEnum cellType;//Тип клетки

    [HideInInspector][SerializeField]public List<NeighborCellStruct> neighbors = new List<NeighborCellStruct>();//Соседние клетки, в которые можно прийти из данной ячейки

    [NonSerialized][HideInInspector]public bool visited = false;//Была ли посещена данная ячейка (используется в алгоритмах поиска путей)
    [NonSerialized][HideInInspector]public NavigationCell fromCell = null;//Из какой клетки мы пришли в данную (используется в алгоритмах поиска путей)

    public int cellNumb, groupNumb;

    #endregion //fields

    public NavigationCell(Vector2 _cellPosition, NavCellTypeEnum _cellType)
    {
        cellPosition = _cellPosition;
        cellType = _cellType;
        neighbors = new List<NeighborCellStruct>();
        visited = false;
        fromCell = null;
    }

    /// <summary>
    /// Очистить клетку (пометить, что она непосещённая)
    /// </summary>
    public void ClearCell()
    {
        visited = false;
        fromCell = null;
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