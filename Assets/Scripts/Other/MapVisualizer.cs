using UnityEngine;
using System.Collections;

/// <summary>
/// Объект, ответственный за визуализацию карт
/// </summary>
public class MapVisualizer : MonoBehaviour
{
    #region fields

    private NavigationMap map;
    public NavigationMap Map { get { return map; } set { map = value; } }

    #endregion //fields

    #region parametres

    private Vector2 cellSize;
    public Vector2 CellSize { set { cellSize = value; } }

    #endregion //parametres

    void OnDrawGizmos()
    {

        if (map!=null)
        {

            #region mapDraw

            switch (map.mapType)
            {
                case NavMapTypeEnum.usual:
                    {
                        foreach (NavigationGroup navGroup in map.cellGroups)
                        {
                            foreach (NavigationCell navCell in navGroup.cells)
                            {
                                DrawCell(navCell.cellPosition, cellSize, navCell.cellType == NavCellTypeEnum.usual ? Color.green :
                                                                            navCell.cellType == NavCellTypeEnum.movPlatform ? Color.cyan :
                                                                            Color.yellow);
                            }
                        }
                        break;
                    }
                case NavMapTypeEnum.fly:
                    {
                        foreach (NavigationGroup navGroup in map.cellGroups)
                        {
                            foreach (NavigationCell navCell in navGroup.cells)
                            {
                                DrawCell(navCell.cellPosition, cellSize, Color.green);
                            }
                        }
                        /*if (map.cellGroups.Count > 0)
                        {
                            NavigationGroup currentGroup = map.cellGroups.Find(x => x.cells.Count > 0);
                            if (currentGroup != null)
                                DrawCell(currentGroup.cells[0].cellPosition, cellSize, Color.green);
                        }*/
                        break;
                    }
                case NavMapTypeEnum.crawl:
                    {
                        foreach (NavigationGroup navGroup in map.cellGroups)
                        {
                            foreach (NavigationCell navCell in navGroup.cells)
                            {
                                DrawCell(navCell.cellPosition, cellSize, navCell.cellType == NavCellTypeEnum.usual ? Color.green :
                                                                         navCell.cellType == NavCellTypeEnum.jump ? Color.red : Color.black);
                            }
                        }
                        break;
                    }
            }
        }

        #endregion //mapDraw
    }

    /// <summary>
    /// Нарисовать ячейку
    /// </summary>
    /// <param name="_cellCenter">центр ячейки</param>
    /// <param name="_cellSize">размер ячейки</param>
    /// <param name="_cellColor">цвет ячейки</param>
    static void DrawCell(Vector2 _cellCenter, Vector2 _cellSize, Color _cellColor)
    {
        Gizmos.color = _cellColor;
        Gizmos.DrawLine(_cellCenter + new Vector2(_cellSize.x / 2f, _cellSize.y / 2f),
                                             _cellCenter + new Vector2(_cellSize.x / 2f, -_cellSize.y / 2f));
        Gizmos.DrawLine(_cellCenter + new Vector2(_cellSize.x / 2f, -_cellSize.y / 2f),
                                                           _cellCenter + new Vector2(-_cellSize.x / 2f, -_cellSize.y / 2f));
        Gizmos.DrawLine(_cellCenter + new Vector2(-_cellSize.x / 2f, -_cellSize.y / 2f),
                                                           _cellCenter + new Vector2(-_cellSize.x / 2f, _cellSize.y / 2f));
        Gizmos.DrawLine(_cellCenter + new Vector2(-_cellSize.x / 2f, _cellSize.y / 2f),
                                                           _cellCenter + new Vector2(_cellSize.x / 2f, _cellSize.y / 2f));
    }

}
