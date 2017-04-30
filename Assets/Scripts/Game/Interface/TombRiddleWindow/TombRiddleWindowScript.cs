using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт, управляющий окном головоломки двери в гробницу
/// </summary>
public class TombRiddleWindowScript : InterfaceWindow
{

    #region fields

    private TombDoorClass tombDoor;//Дверь, которая отпирается данной головоломкой
    private List<TombRiddleFragmentScript> fragments;//Список фрагментов головоломки

    #endregion //fields

    #region parametres

    private List<int> startFragmentValues = new List<int> { 1, 4, 4, 3 };//Значения фрагментов в начальный момент времени (по дефолту)
    private List<int> fragmentValues = new List<int> {1, 4, 4, 3 };//Зачения фрагментов

    private bool dontCloseWindow = false;//Запрещает закрытие этого окна

    #endregion //parametres

    public override void Initialize()
    {
        base.Initialize();
        tombDoor = FindObjectOfType<TombDoorClass>();
        fragments = new List<TombRiddleFragmentScript>();
        Transform fragmentsTrans = transform.FindChild("Panel").FindChild("Fragments");
        for (int i = 0; i < fragmentsTrans.childCount; i++)
            fragments.Add(fragmentsTrans.GetChild(i).GetComponent<TombRiddleFragmentScript>());
        for (int i = 0; i < 4; i++)
        {
            int nextIndex = i + 1;
            if (nextIndex > 3) nextIndex = 0;
            fragments[i].InitializeFragment();
            fragments[i].SetNextFragment(fragments[nextIndex]); 
        }
        TombRiddleFragmentScript.tombRiddleWindow = this;
    }

    /// <summary>
    /// Открыть окно с загадкой
    /// </summary>
    public override void OpenWindow()
    {
        base.OpenWindow();
        Restart();
    }

    public override void CloseWindow()
    {
        if (dontCloseWindow)
            return;
        base.CloseWindow();
    }


    /// <summary>
    /// Обновить информацию о значениях фрагмента, в соответствии с новой информацией о них
    /// </summary>
    /// <param name="_fragment">Измнённый фрагмент</param>
    /// <param name="newValue">Значение, которое он приобрёл</param>
    public void FragmentValueChanged(TombRiddleFragmentScript _fragment, int newValue)
    {
        fragmentValues[fragments.IndexOf(_fragment)] = newValue;
        if (fragmentValues[0] == 1 && fragmentValues[1] == 2 && fragmentValues[2] == 3 && fragmentValues[3] == 4)
            StartCoroutine(ActivationProcess());
    }

    IEnumerator ActivationProcess()
    {
        dontCloseWindow = true;
        foreach (TombRiddleFragmentScript fragment in fragments)
            fragment.Activate();
        yield return new WaitForSecondsRealtime(1f);
        dontCloseWindow = false;
        CloseWindow();
        tombDoor.Open();
    }

    /// <summary>
    /// Сбросить все значения фрагментов до изначальных
    /// </summary>
    public void Restart()
    {
        for (int i = 0; i < 4; i++)
        {
            fragments[i].SetOrientation(startFragmentValues[i]);
            fragmentValues[i] = startFragmentValues[i];
        }
    }

}

