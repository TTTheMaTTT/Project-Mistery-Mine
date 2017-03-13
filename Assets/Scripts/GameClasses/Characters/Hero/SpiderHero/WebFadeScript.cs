using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт, который заставляет паутину постепенно исчезать, если прикрепить его к паутине
/// </summary>
public class WebFadeScript : MonoBehaviour
{

    #region consts

    private const float fadeSpeed = 2f;
    private const float lifeTime = 5f;

    #endregion //consts

    #region fields

    private LineRenderer webRenderer;

    #endregion //fields

    #region parametres

    private Color fadeColor = new Color(1f, 1f, 1f, 0f);

    #endregion //parametres

    void Awake()
    {
        webRenderer = GetComponent<LineRenderer>();
        StartCoroutine(DestroyProcess());
    }

	void Update ()
    {
        Color newColor = Color.Lerp(webRenderer.startColor, fadeColor, Time.deltaTime * fadeSpeed);
        webRenderer.SetColors(newColor, newColor);
	}

    IEnumerator DestroyProcess()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

}
