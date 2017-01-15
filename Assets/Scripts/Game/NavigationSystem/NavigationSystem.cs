using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, содержащий информацию о навигационной система уровня
/// </summary>
public class NavigationSystem : ScriptableObject
{

    public string levelName;//К какому уровню относится данная навигационная система?

    public Vector2 mapSize;//Размер карты уровня
    public Vector2 mapDownLeft;//Положение левого нижнего угла карты уровня
    public Vector2 groupSize;//Размер группы навигационных ячеек
    public Vector2 cellSize;//Размер одной ячейки

    public NavigationMatrixMap flyMap;//Карта летающих существ
    public NavigationBunchedMap crawlMap;//Карта ползающих существ
    public NavigationBunchedMap usualMap;//Карта гуманоидов

    public NavigationSystem(string _levelName)
    {
        levelName = _levelName;

        flyMap = new NavigationMatrixMap(NavMapTypeEnum.fly);
        crawlMap = new NavigationBunchedMap(NavMapTypeEnum.crawl);
        usualMap = new NavigationBunchedMap(NavMapTypeEnum.usual);

        mapSize = Vector2.zero;
        mapDownLeft = Vector2.zero;
        groupSize = Vector2.zero;
        cellSize = Vector2.zero;
    }

    /// <summary>
    /// Проинициализировать словари навигационных групп внутри карт
    /// </summary>
    public void InitializeDictionaries()
    {
        crawlMap.MakeDictionary();
        usualMap.MakeDictionary();
    }

    /// <summary>
    /// Вернуть карту запрашиваемого типа
    /// </summary>
    /// <param name="mapType">тип карты</param>
    /// <returns>Навигационная карта</returns>
    public NavigationMap GetMap(NavMapTypeEnum mapType)
    {
        switch (mapType)
        {
            case NavMapTypeEnum.usual:
                {
                    return usualMap;
                }
            case NavMapTypeEnum.fly:
                {
                    return flyMap;
                }
            case NavMapTypeEnum.crawl:
                {
                    return crawlMap;
                }
            default:
                {
                    return usualMap;
                }
        }
    }

    /// <summary>
    /// Возвращает прямоугольный массив из навигационных клеток, используя параметры карты
    /// </summary>
    /// <param name="_downLeft">координаты левой нижней точки карты</param>
    /// <param name="_mapSize">размеры карты</param>
    /// <param name="_cellSize">размер навигационной ячейки</param>
    /// <returns></returns>
    public static ComplexNavigationCell[][] GetNewCells(Vector2 _downLeft, Vector2 _mapSize, Vector2 _cellSize)
    {
        ComplexNavigationCell[][] newCells = new ComplexNavigationCell[Mathf.FloorToInt(_mapSize.y / _cellSize.y)][];
        int cellColumnSize = Mathf.FloorToInt(_mapSize.y / _cellSize.y), cellRowSize = Mathf.FloorToInt(_mapSize.x / _cellSize.x);
        for (int i = 0; i < cellColumnSize; i++)
        {
            newCells[i] = new ComplexNavigationCell[cellRowSize];
            for (int j = 0; j < cellRowSize; j++)
            {
                newCells[i][j] = new ComplexNavigationCell(_downLeft + new Vector2(_cellSize.x * (j + 0.5f), _cellSize.y * (i + 0.5f)), NavCellTypeEnum.usual);
            }
        }
        return newCells;
    }

}
