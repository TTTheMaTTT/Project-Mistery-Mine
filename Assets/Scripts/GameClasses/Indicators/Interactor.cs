using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Индикатор, ответственный за взаимодействия
/// </summary>
public class Interactor : MonoBehaviour {

    #region fields

    [SerializeField]
    protected List<string> whatIsInteractable = new List<string>();//С какими объектами впринципе можно провзаимодействовать

    protected List<IInteractive> interactions = new List<IInteractive>();//С какими объектами можно провзаимодействовать в данный момент

    #endregion //fields

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void Initialize ()
    {
        interactions = new List<IInteractive>();
	}

    /// <summary>
    /// Функция взаимодействия
    /// </summary>
    public virtual void Interact()
    {
        if (interactions.Count > 0)
        {
            interactions[0].Interact();
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (whatIsInteractable.Contains(other.gameObject.tag))
        {
            IInteractive interaction = other.gameObject.GetComponent<IInteractive>();
            if (interaction != null ? !interactions.Contains(interaction) : false)
            {
                interactions.Add(interaction);
            }
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (whatIsInteractable.Contains(other.gameObject.tag))
        {
            IInteractive interaction = other.gameObject.GetComponent<IInteractive>();
            if (interaction != null ? interactions.Contains(interaction) : false)
            {
                interactions.Remove(interaction);
            }
        }
    }

}
