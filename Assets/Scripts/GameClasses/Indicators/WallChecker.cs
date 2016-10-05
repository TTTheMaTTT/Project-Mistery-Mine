using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WallChecker : MonoBehaviour {

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
    /// <returns></returns>
    public bool WallInFront()
    {
        return (walls.Count > 0);
    }

}
