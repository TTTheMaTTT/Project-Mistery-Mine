using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Движущаяся платформа
/// </summary>
public class MovingPlatform : MonoBehaviour, IMechanism
{

    #region consts

    protected const float movEps = .001f;//Точность измерения перемещения

    #endregion //consts

    #region fields

    [SerializeField] protected List<Vector2> platformPositions = new List<Vector2>();
    public List<Vector2> PlatformPositions { get { return platformPositions; } }

    protected Animator anim;
    public Material lineMaterial;//Какой материал используется для отрисовки линии

    [SerializeField][HideInInspector]protected List<LineRenderer> lines=new List<LineRenderer>();//Линии, отображающие маршрут платормы

    #endregion //fields

    #region parametres

    [SerializeField]protected float speed=0.1f;//Скорость платформы    
    [SerializeField]protected int orientation=1;//Направление движения
    [SerializeField]protected bool nonStop;//Останавливаается ли платформа впринципе
    [SerializeField]protected bool changeableDirection = true;//При взаимодействии с платформой, поменяется ли направление движения?

    [SerializeField] protected float lineWidth = .02f;
    public float LineWidth { get { return lineWidth; } }
    [SerializeField] protected float lineRatio = .1f;
    public float LineRatio { get { return lineRatio; } }

    protected bool moving = false;//Движется ли платформа или нет
    protected int currentPosition=0;//Текущая позиция
    protected Vector2 direction = Vector2.zero;//Направление движения платформа

    #endregion //parametres

    protected void Awake()
    {
        Initialize();
    }

    protected void FixedUpdate()
    {
        if (moving && platformPositions.Count>1)
        {
            Vector2 nextPoint = platformPositions[currentPosition + orientation];
            float distance = Mathf.Pow((nextPoint.x - transform.position.x), 2) + Mathf.Pow((nextPoint.y - transform.position.y), 2);
            if (distance < Mathf.Pow(speed * Time.fixedDeltaTime + movEps,2))
            {
                transform.position = nextPoint;
                currentPosition += orientation;
                if (currentPosition == platformPositions.Count - 1 || currentPosition == 0)
                {
                    if (nonStop)
                    {
                        if (Mathf.Approximately(Vector2.Distance(platformPositions[platformPositions.Count - 1], platformPositions[0]), 0f))
                            currentPosition = orientation > 0 ? 0 : platformPositions.Count - 1;
                        else
                            orientation *= -1;
                        moving = true;
                    }
                    else
                        moving = false;
                }
                if (moving)
                {
                    nextPoint = platformPositions[currentPosition + orientation];
                    direction = (nextPoint - platformPositions[currentPosition]).normalized;
                }
            }
            else
                transform.position += new Vector3(direction.x,direction.y,0f) * Time.fixedDeltaTime * speed;
            if (anim != null)
                anim.Play(orientation>0?"MoveForward":"MoveBackward");
        }
        else if (anim != null)
            anim.Play("Idle");
    }

    protected void Initialize()
    {
        if (platformPositions.Count == 0)
        {
            platformPositions.Add(transform.position);
        }
        float minX = Mathf.Infinity;
        currentPosition = 0;
        foreach (Vector2 x in platformPositions)
            if (Vector2.Distance(x, transform.position) < minX)
            {
                minX = Vector2.Distance(x, transform.position);
                currentPosition = platformPositions.IndexOf(x);
            }
        transform.position = platformPositions[currentPosition];
        if (platformPositions.Count != 1)
        {
            if ((currentPosition == platformPositions.Count-1 && orientation == 1) ||
                (currentPosition == 0 && orientation == -1))
            {
                orientation *= -1;
                if (Mathf.Approximately(Vector2.Distance(platformPositions[0], platformPositions[platformPositions.Count - 1]), 0) && nonStop)
                {
                    orientation *= -1;
                    currentPosition = orientation == 1 ? 0 : platformPositions.Count - 1;
                }
            }
            Vector2 nextPoint = platformPositions[currentPosition + orientation];
            direction = (nextPoint-platformPositions[currentPosition]).normalized;
        }

        if (nonStop)
            moving = true;

        anim = GetComponent<Animator>();

    }

    /// <summary>
    /// Активировать механизм
    /// </summary>
    public void ActivateMechanism()
    {
        moving = true;
        if (changeableDirection)
        {
            orientation *= -1;
            bool a1 = Mathf.Approximately(Vector2.Distance(platformPositions[0], transform.position), 0f);
            bool a2 = Mathf.Approximately(Vector2.Distance(platformPositions[platformPositions.Count - 1], transform.position), 0f);
            if (a1 && orientation == -1)
                    
            {
                orientation = 1;
                currentPosition = 0;
            }
            else if (a2 && orientation == 1)
            {
                orientation = -1;
                currentPosition = platformPositions.Count - 1;
            }
            else if (!a1 && !a2)
            {
                currentPosition -= orientation;
            }
        }
        Vector2 nextPoint = platformPositions[currentPosition + orientation];
        direction = (nextPoint - platformPositions[currentPosition]).normalized;

    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Rigidbody2D>() != null)
        {
            other.transform.SetParent(transform);
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Rigidbody2D>() != null)
        {
            other.transform.parent=null;
        }
    }

    public List<LineRenderer> GetLines()
    {
        return lines;
    }

    public void SetLines(List<LineRenderer> _lines)
    {
        lines = _lines;
    }

}

/// <summary>
/// Редактор движущихся платформ
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(MovingPlatform))]
public class MovingPlatformEditor : Editor
{

    List<LineRenderer> lines;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        MovingPlatform mov = (MovingPlatform)target;
        lines = mov.GetLines();
        if (lines == null)
            mov.SetLines(new List<LineRenderer>());
        if (GUILayout.Button("DeleteLines"))
        {
            mov.SetLines(new List<LineRenderer>());
            foreach (LineRenderer line in lines)
                DestroyImmediate(line.gameObject);
        }
        if (GUILayout.Button("CreateLines"))
        {
            foreach (LineRenderer line in lines)
                DestroyImmediate(line.gameObject);
            lines.Clear();
            for (int i = 1; i < mov.PlatformPositions.Count; i++)
            {
                GameObject gLine = new GameObject("line" + (i - 1).ToString());
                GameObject gLines = GameObject.Find("PlatformLines");
                if (gLines != null)
                    gLine.transform.SetParent(gLines.transform);
                LineRenderer line = gLine.AddComponent<LineRenderer>();
                line.sharedMaterial = mov.lineMaterial;
                Vector3 pos1 = mov.PlatformPositions[i - 1], pos2=mov.PlatformPositions[i];
                pos1.z = mov.transform.position.z;
                pos2.z = mov.transform.position.z;
                line.SetPositions(new Vector3[] { pos1, pos2 });
                line.SetWidth(mov.LineWidth, mov.LineWidth);
                AutoLineRender rLine=gLine.AddComponent<AutoLineRender>();
                rLine.SetPoints(mov.LineRatio, pos1, pos2);
                rLine.AutoTile();
                lines.Add(line);
            }
            mov.SetLines(lines);
        }
    }
}
#endif