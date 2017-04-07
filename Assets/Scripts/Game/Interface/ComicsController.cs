using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ComicsController : MonoBehaviour
{

    #region consts

    private const float fadeTime = 2f;//Время затухания
    private const float fadeSpeed = 5f;
    private const float pageTime = 10f;//Время показа одной страницы

    #endregion //consts

    #region fields

    private Image comicsPageImage;
    [SerializeField]private List<Sprite> comicsPages=new List<Sprite>();

    #endregion //fields

    #region parametres

    private int currentPageNumber = 0;//Номер текущей страницы
    [SerializeField]private string nextLevelName;//Название сцены, на которую произойдёт переход по окончанию комикса
    private Color targetColor = Color.white;

    #endregion //parametres

    void Start ()
    {
        currentPageNumber = 0;
        comicsPageImage = transform.FindChild("ComicsPanel").FindChild("ComicsPage").GetComponent<Image>();
        ShowPage();
        SpecialFunctions.PlayGame();
	}
	
	void Update ()
    {
        comicsPageImage.color = Color.Lerp(comicsPageImage.color, targetColor, Time.deltaTime * fadeSpeed);
        if (Input.GetButtonDown("Jump"))
            NextPage();
	}

    /// <summary>
    /// Показать текущую страницу
    /// </summary>
    void ShowPage()
    {
        if (currentPageNumber >= comicsPages.Count)
            SceneManager.LoadScene(nextLevelName);
        else
            StartCoroutine("PageProcess");
    }

    /// <summary>
    /// Процесс отображения страницы комикса
    /// </summary>
    /// <returns></returns>
    IEnumerator PageProcess()
    {
        comicsPageImage.sprite = comicsPages[currentPageNumber];
        targetColor = Color.white;
        yield return new WaitForSeconds(fadeTime);
        comicsPageImage.color = Color.white;
        yield return new WaitForSeconds(pageTime);
        targetColor = new Color(1f, 1f, 1f, 0f);
        yield return new WaitForSeconds(fadeTime);
        comicsPageImage.color = new Color(1f, 1f, 1f, 0f);
        currentPageNumber++;
        ShowPage();
    }

    /// <summary>
    /// Сразу перейти на следующую страницу
    /// </summary>
    void NextPage()
    {
        StopCoroutine("PageProcess");
        currentPageNumber++;
        ShowPage();
        comicsPageImage.color = Color.white;
    }

}
