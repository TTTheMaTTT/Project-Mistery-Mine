using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Индикатор, ответственный за взаимодействия
/// </summary>
public class Interactor : MonoBehaviour
{

    #region fields

    [SerializeField]
    protected List<string> whatIsInteractable = new List<string>();//С какими объектами впринципе можно провзаимодействовать

    protected List<GameObject> interactions = new List<GameObject>();//С какими объектами можно провзаимодействовать в данный момент

    #endregion //fields

    #region parametres

    protected bool activated = true;

    #endregion //parametres

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        interactions = new List<GameObject>();
        SpecialFunctions.dialogWindow.DialogEventHandler += HandleDialogChange;
    }

    /// <summary>
    /// Функция взаимодействия
    /// </summary>
    public virtual void Interact()
    {
        if (!activated)
            return;
        if (interactions.Count > 0)
        {
            if (interactions[0] != null ? interactions[0].active : false)
            {
                IInteractive interaction = interactions[0].GetComponent<IInteractive>();
                if (interaction != null)
                {
                    interaction.Interact();
                    StartCoroutine(InteractionProcess());
                }
            }
            else
            {
                interactions.RemoveAt(0);
            }
            if (interactions[0] == null ? true : (interactions[0].GetComponent<IInteractive>() == null
                                                 || interactions[0].GetComponent<Collider2D>() == null ? true : !interactions[0].GetComponent<Collider2D>().enabled))
            {
                IInteractive interaction = interactions[0] != null ? interactions[0].GetComponent<IInteractive>() : null;
                if (interaction != null && activated)
                    interaction.SetOutline(false);
                interactions.RemoveAt(0);
                interaction = interactions.Count > 0 ? (interactions[0] != null ? interactions[0].GetComponent<IInteractive>() : null) : null;
                if (interaction != null && activated)
                    interaction.SetOutline(false);
            }
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (whatIsInteractable.Contains(other.gameObject.tag))
        {
            IInteractive interaction = other.gameObject.GetComponent<IInteractive>();
            if (interaction != null ? !interactions.Contains(other.gameObject) && interaction.IsInteractive() : false)
            {
                interactions.Add(other.gameObject);
                if (interactions.Count == 1 && activated)
                {
                    interaction.SetOutline(true);
                }
            }
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (whatIsInteractable.Contains(other.gameObject.tag))
        {
            IInteractive interaction = other.gameObject.GetComponent<IInteractive>();
            if (interaction != null && activated)
            {
                interaction.SetOutline(false);
            }
            if (interactions.Contains(other.gameObject))
            {
                interactions.Remove(other.gameObject);
            }
            if (interactions.Count > 0 && activated)
            {
                interaction = interactions[0].GetComponent<IInteractive>();
                interaction.SetOutline(true);
            }
        }
    }

    /// <summary>
    /// Сменить объект взаимодействия
    /// </summary>
    public virtual void ChangeInteraction()
    {
        if (interactions.Count > 1)
        {
            GameObject changedInter = interactions[0];
            IInteractive inter = changedInter.GetComponent<IInteractive>();
            if (inter != null)
                inter.SetOutline(false);
            interactions.RemoveAt(0);
            interactions.Add(changedInter);
            inter = interactions[0].GetComponent<IInteractive>();
            if (inter != null)
                inter.SetOutline(true);

        }
    }

    /// <summary>
    /// Сбросить список взаимодействий
    /// </summary>
    public void ResetInteractions()
    {
        interactions = new List<GameObject>();
    }

    /// <summary>
    /// Функция, возвращающая степень готовности персонажа к взаимодействию
    /// </summary>
    public bool ReadyForInteraction()
    {
        return (interactions.Count > 0);
    }

    protected IEnumerator InteractionProcess()
    {
        Collider2D col = null;
        yield return new WaitForSeconds(0.1f);
        if (interactions.Count > 0 ? ((interactions[0] != null ? interactions[0].active: false)? ((col = interactions[0].GetComponent<Collider2D>()) == null ? true : !col.enabled) : true) : false)
        {
            interactions.RemoveAt(0);
        }
    }

    protected void SetActivated(bool _activated)
    {
        activated = _activated;
        if (interactions.Count == 0)
            return;
        IInteractive inter = interactions[0].GetComponent<IInteractive>();
        if (inter == null)
            return;
        inter.SetOutline(activated);
    }

    #region eventHandlers

    /// <summary>
    /// Обработать событие "Диалог начался (или закончился)"
    /// </summary>
    void HandleDialogChange(object sender, DialogEventArgs e)
    {
        SetActivated(!e.StopGameProcess || !e.Begin);
    }

    #endregion //eventHandlers

}
