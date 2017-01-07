﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, представляющий собой навигационную карту уровня. Указывает мобам (определённого типа), как им надо передвигаться
/// </summary>
[System.Serializable]
public class NavigationMap
{

    #region fields

    public List<NavigationGroup> cellGroups = new List<NavigationGroup>();//Группы клеток, что входять в данную карту
    public NavMapTypeEnum mapType;//Тип карты

    #endregion //fields

    public NavigationMap(NavMapTypeEnum _mapType)
    {
        mapType = _mapType;
        cellGroups = new List<NavigationGroup>();
    }

    /// <summary>
    /// Создать группы ячеек, используя информацию о размере и положении всей карты и о дефолтном размере групп 
    /// </summary>
    /// <param name="downLeft">Положение нижней левой точки карты</param>
    /// <param name="mapSize">Размер карты</param>
    /// <param name="groupSize">Размер части карты, соответствующей группе ячеек</param>
    public void CreateGroups(Vector2 downLeft, Vector2 mapSize, Vector2 groupSize)
    {
        cellGroups = new List<NavigationGroup>();
        float posX = downLeft.x, posY = downLeft.y;
        Vector2 upRight = new Vector2(downLeft.x + mapSize.x, downLeft.y + mapSize.y);
        while (posY < upRight.y)
        {
            posX = downLeft.x;
            while (posX < upRight.x)
            {
                NavigationGroup navGroup = new NavigationGroup();
                navGroup.SetSize(new Vector2(posX, posY), new Vector2(posX + groupSize.x, posY + groupSize.y));
                cellGroups.Add(navGroup);
                posX += groupSize.x;
            }
            posY += groupSize.y;
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
    /// Пересчитать размеры всех групп
    /// </summary>
    /// <param name="cellSize">размер клетки</param>
    public void ResetGroupSizes(Vector2 cellSize)
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
            _cell = _group.GetCurrentCell(targetPos);
        return _cell;
    }

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

        ClearMap();
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
    /// Вернуть ближайшую к целевой точке ячейку из группы
    /// </summary>
    /// <param name="targetPos">целевая точка</param>
    /// <returns></returns>
    public NavigationCell GetCurrentCell(Vector2 targetPos)
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