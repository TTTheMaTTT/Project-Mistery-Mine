using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Визуальное отображение героя в обличии паука
/// </summary>
public class SpiderHeroVisual : HeroVisual
{

    #region consts

    protected const float spiderOffset = .08f;
    protected const float webRatio = .1f;
    protected const float webWidth = .01f;
    protected const float blinkSpeed = 3f;

    #endregion //consts

    #region fields

    public Material webMaterial;//Материал, которым рендерится паутина
    protected GameObject currentWeb;//Паутина, на которой находится паук в данный момент
    protected LineRenderer webRenderer;//Компонент, ответственный за рендеринг паутины
    protected AutoLineRender autoRenderer;//Компонент, ответственный за правильные размеры паутины
    protected SpriteRenderer spriteRenderer;
    protected GameObject CurrentWeb
    {
        set
        {
            currentWeb = value;
            if (value)
            {
                webRenderer = currentWeb.GetComponent<LineRenderer>();
                if (webRenderer == null)
                    webRenderer = currentWeb.AddComponent<LineRenderer>();
                autoRenderer = currentWeb.GetComponent<AutoLineRender>();
                if (autoRenderer == null)
                    autoRenderer = currentWeb.AddComponent<AutoLineRender>();
            }
            else
            {
                webRenderer = null;
                autoRenderer = null;
            }

        }
    }

    #endregion //fields

    #region parametres

    protected Vector2 webConnectionPoint = Vector2.zero;//Точка, к которой крепится паутина
    protected bool blink = false;//Мерцает ли паук?
    protected Color targetColor = Color.white;

    #endregion //parametres

    protected override void Initialize()
    {
        base.Initialize();
        spriteRenderer = GetComponent<SpriteRenderer>();
        CurrentWeb = null;
        blink = false;
    }

    protected virtual void FixedUpdate()
    {
        if (blink)
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.fixedDeltaTime * blinkSpeed);
    }

    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("webMove", WebMove);
        visualFunctions.Add("setWebMove", SetWebMove);
        visualFunctions.Add("startCeilBlink", StartCeilBlink);
        visualFunctions.Add("stopCeilBlink", StopCeilBlink);
        visualFunctions.Add("goAway", GoAway);
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
        if (Mathf.Abs(rigid.velocity.sqrMagnitude) > minSpeed*minSpeed)
        {
            anim.Play("Run");
        }
        else
        {
            anim.Play("Idle");
        }
    }

    /// <summary>
    /// Функция, отвечающая за перемещение паука по паутине
    /// </summary>
    protected virtual void WebMove(string id, int argument)
    {
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("WebMove"))
            anim.Play("WebMove");
        if (Mathf.Abs(rigid.velocity.y) >= minSpeed)
        {
            anim.speed = 1f;
            ChangeSpiderWebLength();
        }
        else
            anim.speed = 0f;
    }

    /// <summary>
    /// Перейти в режим визуализации перемещения по паутине, или выйти из него
    /// </summary>
    protected virtual void SetWebMove(string id, int argument)
    {
        if (argument == 1)
        {
            anim.Play("WebMove");
            webConnectionPoint = (Vector2)transform.position + Vector2.up * spiderOffset;
            CreateSpiderWeb(webConnectionPoint);
        }
        else
        {
            RemoveSpiderWeb();
            anim.speed = 1f;
        }
    }

    /// <summary>
    /// Изменить длину паутины
    /// </summary>
    protected void ChangeSpiderWebLength()
    {
        if (currentWeb == null)
            return;
        webRenderer.SetPositions(new Vector3[] { webConnectionPoint, (Vector2)transform.position + Vector2.up * spiderOffset });
        autoRenderer.SetPoints(webRatio, webRenderer.GetPosition(0), webRenderer.GetPosition(1));
    }

    /// <summary>
    /// Создать новую паутину
    /// </summary>
    protected void CreateSpiderWeb(Vector2 webPosition)
    {
        if (currentWeb != null)
            RemoveSpiderWeb();
        CurrentWeb = new GameObject("Web");
        currentWeb.transform.position = webConnectionPoint;
        webRenderer.sharedMaterial = webMaterial;
        webRenderer.SetPositions(new Vector3[] { webConnectionPoint, webConnectionPoint });
        webRenderer.SetWidth(webWidth, webWidth);
        autoRenderer.SetPoints(webRatio, webRenderer.GetPosition(0), webRenderer.GetPosition(1));
    }

    /// <summary>
    /// Перестать плести паутину и начать процесс её исчезновения
    /// </summary>
    protected void RemoveSpiderWeb()
    {
        currentWeb.AddComponent<WebFadeScript>();
        CurrentWeb = null;
    }

    /// <summary>
    /// Анимировать уход
    /// </summary>
    protected virtual void GoAway(string id, int argument)
    {
        anim.Play("GoAway");
        StartVisualRoutine(6, 6f);
    }

    /// <summary>
    /// Начать мигание, связанное с стоянием на потолке
    /// </summary>
    protected virtual void StartCeilBlink(string id, int argument)
    {
        StartCoroutine("CeilBlinkProcess");
    }

    /// <summary>
    /// Прекратить мигание на потолке
    /// </summary>
    protected virtual void StopCeilBlink(string id, int argument)
    {
        blink = false;
        spriteRenderer.color = Color.white;
        StopCoroutine("CeilBlinkProcess");
    }

    /// <summary>
    /// Процесс мигания, которое связано с превышением времени стояния на потолке
    /// </summary>
    protected virtual IEnumerator CeilBlinkProcess()
    {
        blink = true;
        for (int i = 0; i < 2; i++)
        {
            targetColor = new Color(1f, 1f, 1f, .2f);
            yield return new WaitForSeconds(.25f);
            targetColor = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(.25f);
        }
        blink = false;
        spriteRenderer.color = Color.white;
    }

}
