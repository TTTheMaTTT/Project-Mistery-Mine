using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Экран загрузки игры
/// </summary>
public class LoadingScreenScript : MonoBehaviour
{

    #region fields

    public static LoadingScreenScript instance;

    private GameObject img;
    private Animator anim;

    #endregion //fields

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        img = transform.FindChild("Panel").FindChild("Image").gameObject;
        anim=img.GetComponent<Animator>();
    }

    /// <summary>
    /// Загрузить следующую сцену
    /// </summary>
    /// <param name="levelName"></param>
    public void LoadScene(string levelName)
    {
        StartCoroutine(LoadSceneRoutine(levelName));
    }

    /// <summary>
    /// Карутин, что переносит игру на новый уровень 
    /// </summary>
    /// <param name="levelName">Название уровня</param>
    /// <returns></returns>
    private IEnumerator LoadSceneRoutine(string levelName)
    {
        //SpecialFunctions.PauseGame();
        ShowLoadingScreen();
        AsyncOperation operation = SceneManager.LoadSceneAsync(levelName);

        while (!operation.isDone)
        {
            yield return null;
        }

        HideLoadingScreen();
    }

    /// <summary>
    /// Показать экран загрузки
    /// </summary>
    private void ShowLoadingScreen()
    {
        GetComponent<Canvas>().enabled = true;
        img.SetActive(true);
        anim.SetTimeUpdateMode(UnityEngine.Experimental.Director.DirectorUpdateMode.UnscaledGameTime);
        anim.PlayInFixedTime("Animation");
    }

    private void HideLoadingScreen()
    {
        GetComponent<Canvas>().enabled = false;
        img.SetActive(false);
    }

}
