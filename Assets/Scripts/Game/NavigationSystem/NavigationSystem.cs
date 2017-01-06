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

    [SerializeField]public NavigationMap[] maps = new NavigationMap[3];

    public NavigationSystem(string _levelName)
    {
        levelName = _levelName;

        maps[0] = new NavigationMap(NavMapTypeEnum.usual);
        maps[1] = new NavigationMap(NavMapTypeEnum.fly);
        maps[2] = new NavigationMap(NavMapTypeEnum.crawl);

        mapSize = Vector2.zero;
        mapDownLeft = Vector2.zero;
        groupSize = Vector2.zero;
        cellSize = Vector2.zero;
    }

    public NavigationMap GetMap(NavMapTypeEnum mapType)
    {
        switch (mapType)
        {
            case NavMapTypeEnum.usual:
                {
                    return maps[0];
                }
            case NavMapTypeEnum.fly:
                {
                    return maps[1];
                }
            case NavMapTypeEnum.crawl:
                {
                    return maps[2];
                }
            default:
                {
                    return maps[0];
                }
        }
    }

    public static NavigationCell[][] GetNewCells(Vector2 _downLeft, Vector2 _mapSize, Vector2 _cellSize)
    {
        NavigationCell[][] newCells = new NavigationCell[Mathf.FloorToInt(_mapSize.y / _cellSize.y)][];
        int cellColumnSize = Mathf.FloorToInt(_mapSize.y / _cellSize.y), cellRowSize = Mathf.FloorToInt(_mapSize.x / _cellSize.x);
        for (int i = 0; i < cellColumnSize; i++)
        {
            newCells[i] = new NavigationCell[cellRowSize];
            for (int j = 0; j < cellRowSize; j++)
            {
                newCells[i][j] = new NavigationCell(_downLeft + new Vector2(_cellSize.x * (j + 0.5f), _cellSize.y * (i + 0.5f)), NavCellTypeEnum.usual);
            }
        }
        return newCells;
    }

}
