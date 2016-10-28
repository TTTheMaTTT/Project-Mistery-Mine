using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Дверь, служащая переходом на следующий уровень
/// </summary>
public class NextLevelDoor : DoorClass
{

    #region consts

    protected const float nextLevelTime = 2.1f;//Время, за которое происходит переход на следующий уровень

    #endregion //consts

    #region parametres

    [SerializeField]
    protected string nextLevelName;//Следующий уровень, на который произойдёт переход

    #endregion //parametres

    /// <summary>
    /// Что произойдёт, если открыть дверь
    /// </summary>
    public override void Open()
    {
        base.Open();
        StartCoroutine(NextLevelProcess());
    }

    /// <summary>
    /// Процесс перехода на следующий уровень
    /// </summary>
    protected IEnumerator NextLevelProcess()
    {
        SpecialFunctions.SetFade(true);
        yield return new WaitForSeconds(nextLevelTime);
        if (nextLevelName != string.Empty)
            SceneManager.LoadScene(nextLevelName);
    }

}
