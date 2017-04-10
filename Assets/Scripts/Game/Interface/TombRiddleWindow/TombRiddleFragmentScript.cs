using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    #endregion //fields

    #region parametres

    private int fragmentValue = 1;
    private bool turning = false;//Поворачивается ли фрагмент в данный момент

    #endregion //parametres

    public void InitializeFragment()
    {
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Повернуть фрагмент
    /// </summary>
    public void Turn()
    {

    }

    /// <summary>
    /// Сменить ориентацию фрагмента
    /// </summary>
    public void ChangeOrientation(int _value)
    {

    }

    /// <summary>
    /// Процесс поворота фрагмента
    /// </summary>
    IEnumerator TurnProcess()
    {
        turning = true;
        int nextValue = fragmentValue + 1;
        if (nextValue == 5) nextValue = 1;
        anim.Play(fragmentValue.ToString() + "-" + nextValue.ToString());
        yield return new WaitForSeconds(turnTime);
        anim.Play(nextValue.ToString());
        fragmentValue = nextValue;
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

}
