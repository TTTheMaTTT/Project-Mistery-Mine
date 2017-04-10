using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт, управляющий одним фрагментом ключа в головоломке двери в гробницу
/// </summary>
public class TombRiddleFragmentScript : MonoBehaviour
{

    #region consts

    private const float turnTime = 1f;//Время прокручивания

    #endregion //consts

    #region fields

    private Animator anim;

    public static TombRiddleWindowScript tombRiddleWindow;
    private TombRiddleFragmentScript nextFragment;//На какой ещё фрагмент прокрутиться при нажатии на данный фрагмент
    private Image img;

    #endregion //fields

    #region parametres

    private int fragmentValue = 1;
    private bool turning = false;//Поворачивается ли фрагмент в данный момент
    public bool Turning { get { return turning; } }
    private bool activated=false;

    #endregion //parametres

    public void InitializeFragment()
    {
        anim = GetComponent<Animator>();
        anim.SetTimeUpdateMode(UnityEngine.Experimental.Director.DirectorUpdateMode.UnscaledGameTime);
        img = transform.GetChild(0).GetComponent<Image>();
    }

    /// <summary>
    /// Повернуть фрагмент
    /// </summary>
    public void Turn()
    {
        if (turning || nextFragment.Turning)
            return;
        ChangeOrientation();
        nextFragment.ChangeOrientation();
    }

    /// <summary>
    /// Сменить ориентацию фрагмента
    /// </summary>
    public void ChangeOrientation()
    {
        if (turning || activated)
            return;
        StartCoroutine("TurnProcess");
    }

    /// <summary>
    /// Сразу установить ориентацию фрагменту
    /// </summary>
    public void SetOrientation(int _value)
    {
        if (turning)
        {
            turning = false;
            StopCoroutine("TurnProcess");
        }

        anim.Play(_value.ToString());
        fragmentValue = _value;
    }

    /// <summary>
    /// Процесс поворота фрагмента
    /// </summary>
    IEnumerator TurnProcess()
    {
        turning = true;
        int nextValue = fragmentValue + 1;
        if (nextValue == 5) nextValue = 1;
        anim.SetTimeUpdateMode(UnityEngine.Experimental.Director.DirectorUpdateMode.UnscaledGameTime);
        anim.PlayInFixedTime(fragmentValue.ToString() + "-" + nextValue.ToString());
        yield return new WaitForSecondsRealtime(turnTime);
        anim.Play(nextValue.ToString());
        fragmentValue = nextValue;
        tombRiddleWindow.FragmentValueChanged(this, fragmentValue);
        turning = false;
    }

    /// <summary>
    /// Установить следующий по счёту фрагмент, относительно данного
    /// </summary>
    /// <param name="_nextFragment"></param>
    public void SetNextFragment(TombRiddleFragmentScript _nextFragment)
    {
        nextFragment = _nextFragment;
    }

    public void Activate()
    {
        activated = true;
        anim.PlayInFixedTime("Activate");
        img.color = new Color(0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// Подсветить кнопку
    /// </summary>
    /// <param name="highlighted"></param>
    public void Highlight(bool highlighted)
    {
        img.color = highlighted && !activated ? Color.yellow : new Color(0f, 0f, 0f, 0f);
    }

}
