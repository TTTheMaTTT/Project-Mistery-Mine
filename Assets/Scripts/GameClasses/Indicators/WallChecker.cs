using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WallChecker : MonoBehaviour {

    #region consts

    //protected const float checkTime = 1f;

    #endregion //consts

    #region fields

    //protected List<GameObject> walls = new List<GameObject>();

    #endregion //fields

    #region parametres

    [SerializeField]
    protected List<string> whatIsWall;
    public List<string> WhatIsWall { get { return whatIsWall; } }

    [SerializeField]protected Vector2 size;//Размер проверяемой области
    public Vector2 Size { get { return size; } set { size = value; } }
    [SerializeField]protected Vector2 defaultPosition;//Место расположения проверяемой области, когда объект, использующий этот индикатор, никак не повёрнут и смотрит вправо.
    public Vector2 DefaultPosition { get { return defaultPosition; } set { defaultPosition = value; } }
    protected Vector2 position;

    protected bool wallInFront = false;//Находится ли в проверяемой области объект с слоем из списка WhatIsWall?
    public bool WallInFront { get { return wallInFront; } set { wallInFront = value; } }

    float angle = 0f;

    #endregion //parametres

    protected void Awake ()
    {
        position = new Vector2(defaultPosition.x*Mathf.Sign(transform.lossyScale.x), defaultPosition.y);
	}

    /*
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

    public virtual void ClearList()
    {
        walls = new List<GameObject>();
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

    /// <summary>
    /// Убрать из списка воспринимаемых коллайдеров заданный тип
    /// </summary>
    /// <param name="wallType">Название слоя, на котором находятся убираемые коллайдеры</param>
    public void RemoveWallType(string wallType)
    {
        whatIsWall.Remove(wallType);
        for (int i = 0; i < walls.Count; i++)
            if (walls[i].layer == LayerMask.NameToLayer(wallType))
            {
                walls.RemoveAt(i);
                i--;
            }
    }

    */

    public virtual void FixedUpdate()
    {
        Vector2 pos = transform.position;
        wallInFront = Physics2D.OverlapBox(pos + position, size, angle, LayerMask.GetMask(whatIsWall.ToArray()));
    }

    /// <summary>
    /// Установить позицию рассматриваемой области в зависимости от ориентации используюшего индикатор объекта.
    /// </summary>
    /// <param name="angle">На какой угол повернут главный объект</param>
    /// <param name="scaleX">В какую сторону смотрит главный объект</param>
    public virtual void SetPosition(float angle, float scaleX)
    {
        Vector2 vectX = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 vectY = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
        position = scaleX * vectX * defaultPosition.x + vectY * defaultPosition.y;
    }

    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.activeObject == gameObject)
        {
            float angle = transform.eulerAngles.z / 180f * Mathf.PI;
            Vector2 vectX = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))/2f;
            Vector2 vectY = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle))/2f;
            Vector2 pos = transform.position;
            Vector2 _pos = pos + (defaultPosition.x * vectX *2f* Mathf.Sign(transform.lossyScale.x) + defaultPosition.y * vectY*2f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_pos + size.x * vectX + size.y * vectY, _pos + size.x * vectX - size.y * vectY);
            Gizmos.DrawLine(_pos + size.x * vectX - size.y * vectY, _pos - size.x * vectX - size.y * vectY);
            Gizmos.DrawLine(_pos - size.x * vectX - size.y * vectY, _pos - size.x * vectX + size.y * vectY);
            Gizmos.DrawLine(_pos - size.x * vectX + size.y * vectY, _pos + size.x * vectX + size.y * vectY);
        }
#endif //UNITY_EDITOR
    }

}
