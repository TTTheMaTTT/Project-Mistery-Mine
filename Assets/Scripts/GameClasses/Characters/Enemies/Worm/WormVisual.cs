using UnityEngine;
using System.Collections;

/// <summary>
/// Визуальное отображение червя
/// </summary>
public class WormVisual : GroundMoveVisual
{

    #region consts

    protected const float groundInTime = 1f;//Время входа либо выхода из земли

    #endregion //consts

    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("groundInteraction", GroundInteraction);
    }

    /// <summary>
    /// Функция, отвечающая за перемещение персонажа на земле
    /// </summary>
    protected override void GroundMove(string id, int argument)
    {
        if (employment <= 6)
        {
            return;
        }
        if (id == "upState")
            anim.Play("UpState");
        else if (Mathf.Abs(rigid.velocity.x) > minSpeed)
            anim.Play("Run");
        else
            anim.Play("Idle");
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
        anim.Play(id);
        StartVisualRoutine(5, argument / 10f);
    }

    /// <summary>
    /// Анимировать взаимодействие с землёй
    /// </summary>
    /// <param name="id">Какое именно взаимодейтсвие произвести</param>
    protected virtual void GroundInteraction(string id, int argument)
    {
        if (id != "from")
            StartCoroutine(VisualOffProcess());
        if (id == "in")
            anim.Play("IntoGround");
        else if (id == "from")
            anim.Play("FromGround");
        else if (id == "up")
            anim.Play("UpStateEnd");
        StartVisualRoutine(5, groundInTime);
    }

    /// <summary>
    /// Процесс, после которого выключается визуальное отображение
    /// </summary>
    protected virtual IEnumerator VisualOffProcess()
    {
        yield return new WaitForSeconds(groundInTime);
        StopCoroutine("VisualProcess");
        employment = maxEmployment;
        gameObject.SetActive(false);
    }

}