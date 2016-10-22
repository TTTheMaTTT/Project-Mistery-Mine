using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Головоломка, связанная с взаимодействием с рычагами
/// </summary>
public class LeverRiddle : MonoBehaviour
{

    #region eventHandlers

    public EventHandler<StoryEventArgs> RiddleSolvedEvent;

    #endregion //eventHandlers

    #region fields

    [SerializeField] protected List<LeverScript> levers = new List<LeverScript>();//Список всех рычагов, необходимых для решения данной головоломки

    #endregion //fields

    public virtual void Awake()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        foreach (LeverScript lever in levers)
        {
            if (lever != null)
                lever.LeverActionEvent += OnLeverActivated;
        }
    }

    #region events

    protected virtual void OnLeverActivated(object sender, EventArgs e)
    {
        SpecialFunctions.StartStoryEvent(this, RiddleSolvedEvent, new StoryEventArgs());   
    }

    #endregion //events

}
