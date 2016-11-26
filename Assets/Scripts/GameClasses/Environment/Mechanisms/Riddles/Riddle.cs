using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Скрипт, описывающий игровую головоломку
/// </summary>
public class Riddle : MonoBehaviour, IMechanism
{

    #region eventHandlers

    public EventHandler<StoryEventArgs> RiddleSolvedEvent;//Событие о решении загадки

    #endregion //eventHandlers

    #region parametres

    protected int progress;//Каков прогресс в решении загадки

    [SerializeField][HideInInspector]protected int id;

    #endregion //parametres

    /// <summary>
    /// Решить загадку
    /// </summary>
    public virtual void SolveRiddle()
    {
        SpecialFunctions.StartStoryEvent(this,RiddleSolvedEvent, new StoryEventArgs());
    }

    /// <summary>
    /// Сбросить прогресс головоломки до начального значаения
    /// </summary>
    public virtual void ResetRiddle()
    {
        progress = 0;
    }

    /// <summary>
    /// Активировать механизм
    /// </summary>
    public virtual void ActivateMechanism()
    {
        SolveRiddle();
    }

    /// <summary>
    /// Вернуть ID
    /// </summary>
    public virtual int GetID()
    {
        return id;
    }

    /// <summary>
    /// Выставить id
    /// </summary>
    public virtual void SetID(int _id)
    {
        id = _id;
    }

    /// <summary>
    /// Возвращает данные о головоломке
    /// </summary>
    public virtual InterObjData GetData()
    {
        RiddleData rData = new RiddleData(id, true, progress);
        return rData;
    }

    /// <summary>
    /// Загрузить данные о головоломке
    /// </summary>
    public virtual void SetData(InterObjData _intObjData) 
    {
        RiddleData rData = (RiddleData)_intObjData;
        if (rData != null)
        {
            progress = rData.progress;
        }
    }

}
