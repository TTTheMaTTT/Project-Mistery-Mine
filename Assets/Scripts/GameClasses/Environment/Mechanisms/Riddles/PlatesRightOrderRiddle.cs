using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Головоломка, которая заключается в том, что нужно в правильном порядке нажать на платформы
/// </summary>
public class PlatesRightOrderRiddle : Riddle
{

    #region fields

    [SerializeField]protected List<LeverScript> levers=new List<LeverScript>();//Платформы, которые нужны для решения головоломки
    [SerializeField]protected List<GameObject> mechanisms = new List<GameObject>();//Вспомогательные элементы
    [SerializeField]protected GameObject riddleSolvedMech;//Механизм, который активируется при решении этой головоломки

    #endregion //fields

    public void Awake()
    {
        progress = 0;
    }

    /// <summary>
    /// Решить головоломку
    /// </summary>
    public override void SolveRiddle()
    {
        base.SolveRiddle();
        IMechanism mech = riddleSolvedMech.GetComponent<IMechanism>();
        if (mech != null)
        {
            mech.ActivateMechanism();
        }
    }

    /// <summary>
    /// Сбросить состояние головоломки до начального
    /// </summary>
    public override void ResetRiddle()
    {
        for (int i = 0; i < progress; i++)
        {
            IMechanism mech = mechanisms[i].GetComponent<IMechanism>();
            if (mech!=null)
                mech.ActivateMechanism();
        }
        progress = 0;
    }

    /// <summary>
    /// Активировать механизм
    /// </summary>
    public override void ActivateMechanism()
    {
        int progress1 = levers.Count;
        if (progress1 != progress)
        {
            for (progress1 = levers.Count; progress1 > progress; progress1--)
            {
                if (levers[progress1 - 1].Activated)
                    break;
            }
            if (progress1 == progress + 1)
            {
                progress++;
                IMechanism mech = mechanisms[progress - 1].GetComponent<IMechanism>();
                if (mech!=null)
                    mech.ActivateMechanism();
                if (progress == levers.Count)
                    SolveRiddle();
            }
            else if (progress1>progress+1)
                ResetRiddle();
        }
    }

}
