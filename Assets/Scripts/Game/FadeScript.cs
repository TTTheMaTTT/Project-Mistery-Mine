using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт, который заставляет объект постепенно исчезать
/// </summary>
public class FadeScript : MonoBehaviour
{

    #region consts

    private const float activeTime = 2.5f;
    private const float fadeSpeed = 3f;

    #endregion //consts

    #region fields

    private SpriteRenderer sprite;

    #endregion //fields

    #region parametres

    [HideInInspector]
    bool activated = false;

    #endregion //parametres

    void Update()
    {
        if (activated)
            sprite.color = Color.Lerp(sprite.color, new Color(1f, 1f, 1f, 0f), Time.fixedDeltaTime * fadeSpeed);
    }

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    public void Activate()
    {
        activated = true;
        StartCoroutine(SetInactiveProcess());
    }

    IEnumerator SetInactiveProcess()
    {
        yield return new WaitForSeconds(activeTime);
        gameObject.SetActive(false);
    }

}
