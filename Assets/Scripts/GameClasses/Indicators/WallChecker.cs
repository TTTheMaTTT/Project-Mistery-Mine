using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WallChecker : MonoBehaviour {

    #region consts

    protected const float checkTime = 1f;

    #endregion //consts

    #region fields

    protected List<GameObject> walls = new List<GameObject>();

    #endregion //fields

    #region parametres

    [SerializeField]
    protected List<string> whatIsWall;

    #endregion //parametres

    protected void Awake ()
    {
        Initialize();
	}

    protected void Initialize()
    {
        walls = new List<GameObject>();
        StartCoroutine(CheckObstacles());
    }

    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (whatIsWall.Contains(LayerMask.LayerToName(other.gameObject.layer)))
        {
            if (!walls.Contains(other.gameObject))
            {
                walls.Add(other.gameObject);
            }
        }
    }

    protected void OnTriggerExit2D(Collider2D other)
    {
        if (whatIsWall.Contains(LayerMask.LayerToName(other.gameObject.layer)))
        {
            if (walls.Contains(other.gameObject))
            {
                walls.Remove(other.gameObject);
            }
        }
    }

    /// <summary>
    /// Функция, с помощью которой определяем, находится ли перед персонажем стена
    /// </summary>
    public bool WallInFront()
    {
        return (walls.Count > 0);
    }

    /// <summary>
    /// Проверка, проходящая раз в секунду, на актуальность учёта препятствий
    /// </summary>
    IEnumerator CheckObstacles()
    {
        yield return new WaitForSeconds(checkTime);
        for (int i = walls.Count - 1; i >= 0; i--)
        {
            Collider2D col=null;
            if (walls[i] != null ? ((col=walls[i].GetComponent<Collider2D>()) == null? true : !col.enabled): true)
                walls.RemoveAt(i);
        }
        StartCoroutine(CheckObstacles());
    }

}
