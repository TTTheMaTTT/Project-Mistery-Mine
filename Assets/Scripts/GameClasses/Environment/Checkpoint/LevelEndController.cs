using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Скрипт, управляющий объектом, что завершает уровень
/// </summary>
public class LevelEndController : MonoBehaviour
{

    #region consts

    protected const float nextLevelTime = 2.1f;//Время, за которое происходит переход на следующий уровень

    #endregion //consts

    #region parametres

    public string nextLevelName;

    public int checkpointNumber = 0;

    #endregion //parametres

    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<HeroController>() != null)
        {
            StartCoroutine(NextLevelProcess());
        }
    }

    /// <summary>
    /// Процесс перехода на следующий уровень
    /// </summary>
    protected IEnumerator NextLevelProcess()
    {
        PlayerPrefs.SetInt("Checkpoint Number", checkpointNumber);
        PlayerPrefs.SetFloat("Hero Health", SpecialFunctions.player.GetComponent<HeroController>().GetHealth());
        SpecialFunctions.gameController.SaveGame(checkpointNumber,true, nextLevelName);
        SpecialFunctions.SetFade(true);
        yield return new WaitForSeconds(nextLevelTime);
        if (nextLevelName != string.Empty)
            SpecialFunctions.gameController.CompleteLevel(nextLevelName);
    }

}
