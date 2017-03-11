using UnityEngine;
using UnityEngine.UI;
using System.Collections;


/// <summary>
/// Этот скрипт заставляет периодически затухать и проявляться объект с компонентом Image
/// </summary>
public class FadeFluctuationScript : MonoBehaviour
{

    #region consts

    protected const float fadeSpeed = 5f;
    protected const float fadeTime = .5f;//Время, за которое происходит затухание или проявление Image

    #endregion //consts

    #region fields

    private Image img=null;

    #endregion //fields

    #region parametres

    private int fadeDirection = 1;

    #endregion //parametres

    void Start ()
    {
        img = GetComponent<Image>();
        if (img!=null)
            StartCoroutine(FadeProcess());
	}
	
	void Update ()
    {
        if (img!=null)
            img.color = Color.Lerp(img.color, new Color(img.color.r, img.color.g, img.color.b, fadeDirection == 1 ? 0f :
                                                                    fadeDirection == -1 ? 1f :
                                                                    img.color.a), Time.deltaTime * fadeSpeed);
    }

    /// <summary>
    /// Процесс затухания или проявления экрана
    /// </summary>
    IEnumerator FadeProcess()
    {
        while (true)
        {
            yield return new WaitForSeconds(fadeTime);
            fadeDirection *= -1;
        }
    }

}
