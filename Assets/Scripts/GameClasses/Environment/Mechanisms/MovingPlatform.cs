using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    protected Animator anim;

    #endregion //fields

    #region parametres

    [SerializeField]protected float speed=0.1f;//Скорость платформы    
    [SerializeField]protected int orientation=1;//Направление движения
    [SerializeField]protected bool nonStop;//Останавливаается ли платформа впринципе
    [SerializeField]protected bool changeableDirection = true;//При взаимодействии с платформой, поменяется ли направление движения?

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
            if (currentPosition>0 && orientation==1 || currentPosition<platformPositions.Count-1 && orientation==-1)
            {
                currentPosition -= orientation;
            }
        }
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

}
