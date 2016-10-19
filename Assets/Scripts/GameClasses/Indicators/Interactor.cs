using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Индикатор, ответственный за взаимодействия
/// </summary>
public class Interactor : MonoBehaviour {

    #region consts

    protected const float ladderOffsetY = 0.1f, ladderOffsetX = .1f;//Максимальное относительное положение лестницы, чтобы на неё ещё можно было взобраться

    #endregion //consts

    #region fields

    [SerializeField]
    protected List<string> whatIsInteractable = new List<string>();//С какими объектами впринципе можно провзаимодействовать

    protected List<GameObject> interactions = new List<GameObject>();//С какими объектами можно провзаимодействовать в данный момент

    protected GameObject ladder = null;
    public GameObject Ladder { get { return ladder; } }

    #endregion //fields

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        interactions = new List<GameObject>();
    }

    /// <summary>
    /// Функция взаимодействия
    /// </summary>
    public virtual void Interact()
    {
        if (interactions.Count > 0)
        {
            if (interactions[0] != null)
            {
                IInteractive interaction = interactions[0].GetComponent<IInteractive>();
                interaction.Interact();
            }
            if (interactions[0] == null ? true : interactions[0].GetComponent<IInteractive>() == null)
            {
                interactions.RemoveAt(0);
            }
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (whatIsInteractable.Contains(other.gameObject.tag))
        {
            IInteractive interaction = other.gameObject.GetComponent<IInteractive>();
            if (interaction != null ? !interactions.Contains(other.gameObject) : false)
            {
                interactions.Add(other.gameObject);
            }
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (whatIsInteractable.Contains(other.gameObject.tag))
        {
            IInteractive interaction = other.gameObject.GetComponent<IInteractive>();
            if (interaction != null ? interactions.Contains(other.gameObject) : false)
            {
                interactions.Remove(other.gameObject);
            }
        }
        else if (ladder != null? other.gameObject == ladder:false)
        {
            ladder = null;
        }
    }

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.tag == "ladder")
        {
            Vector2 offset = other.transform.position - transform.position;
            if ((Mathf.Abs(offset.x) < ladderOffsetX) && (Mathf.Abs(offset.y) < ladderOffsetY))
            {
                ladder = other.gameObject;
            }
        }
    }

    /// <summary>
    /// Функция, возвращающая степень готовности персонажа к взаимодействию
    /// </summary>
    public bool ReadyForInteraction()
    {
        return (interactions.Count > 0);
    }

}
