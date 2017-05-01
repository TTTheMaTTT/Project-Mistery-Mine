using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Визуальное отображение призрака девушки
/// </summary>
public class GhostLadyVisual : FlyVisual
{

    #region consts 

    protected const float startHurricaneTime = .31f, stopHurricaneTime = .43f;

    #endregion //consts

    #region parametres

    protected bool inHurricane = false;//Находится ли персонаж внутри урагана

    #endregion //parametres

    /// <summary>
    /// Сформировать словари визуальных действий
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("hurricaneAttack", HurricaneAttack);
        visualFunctions.Add("stalactiteAttack", StalactiteAttack);
        visualFunctions.Add("setBackground", SetBackground);
    }

    /// <summary>
    /// Анимировать атаку
    /// </summary>
    protected override void Attack(string id, int argument)
    {
        if (employment < 8)
        {
            return;
        }
        if (id == string.Empty)
            anim.Play("Attack");
        else
            anim.Play(id);
        if (id == "FastAttack")
            anim.ForceStateNormalizedTime(0f);
        if (id == "DiveAttack")
            sRenderer.sortingLayerName = "Background";
        else
            sRenderer.sortingLayerName = "Default";
        StartVisualRoutine(5, argument / 100f);
    }

    /// <summary>
    /// Рисовать персонажа в слое Бэкграунд
    /// </summary>
    protected void SetBackground(string id, int argument)
    {
        sRenderer.sortingLayerName = "Background";
    }

    /// <summary>
    /// Анимировать ураганную атаку
    /// </summary>
    protected void HurricaneAttack(string id, int argument)
    {
        employment = 2;
        StartCoroutine("HurricaneProcess");
    }

    /// <summary>
    /// Анимировать совершение сталактитной атаки
    /// </summary>
    protected void StalactiteAttack(string id, int argument)
    {
        employment = 2;
        anim.Play("StalactiteAttack");
    }

    /// <summary>
    /// Процесс урагана
    /// </summary>
    /// <returns></returns>
    protected IEnumerator HurricaneProcess()
    {
        anim.Play("StartHurricaneAttack");
        yield return new WaitForSeconds(startHurricaneTime);
        anim.Play("HurricaneAttack");
    }

    /// <summary>
    /// Остановить все отображения
    /// </summary>
    protected override void StopVisualRoutine(string id, int argument)
    {
        base.StopVisualRoutine(id, argument);
        StopCoroutine("HurricaneProcess");
        if (inHurricane)
            anim.Play("StopHurricaneAttack");
    }

}
