using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Индикатор часового, который замечает героя только через продолжительное время. Если заметит - конец игры
/// </summary>
public class SentryIndicator : MonoBehaviour
{

    #region consts

    private const float endLevelTime = 5f;

    #endregion //consts

    #region fields

    private SpiderController spider;

    #endregion //fields

    #region parametres

    Transform trans;

    [SerializeField] private float
        radius = 1f,//Радиус обзора
        noticeTime = 3f,//Время, 
        suspicionTime = 1f;
    private bool inside = false;

    #endregion //parametres

    void Awake()
    {
        trans = transform;
        spider = transform.parent.parent.GetComponent<SpiderController>();
    }

    void FixedUpdate()
    {
        float sqDistance = Vector2.SqrMagnitude(trans.position - SpecialFunctions.Player.transform.position);
        if (inside ? sqDistance > radius : false)
        {
            inside = false;
            StopNotice();
        }
        else if (!inside ? sqDistance <= radius : false)
        {
            inside = true;
            StartCoroutine("NoticeProcess");
        }
    }

    /// <summary>
    /// Процесс, в течении которого индикатор замечает игрока
    /// </summary>
    IEnumerator NoticeProcess()
    {
        yield return new WaitForSeconds(suspicionTime);
        if (spider != null)
        {
            spider.StopPatrol();
            spider.Turn((OrientationEnum)Mathf.RoundToInt(Mathf.Sign(SpecialFunctions.player.transform.position.x - trans.position.x)));
        }
        yield return new WaitForSeconds(noticeTime - suspicionTime);
        SpecialFunctions.SetText(6f, new MultiLanguageText("Вас обнаружили","You were detected", "", "", ""));
        StartCoroutine("EndLevelProcess");

    }

    void StopNotice()
    {
        if (spider != null && spider.Behavior==BehaviorEnum.calm)
            spider.Patrol();
        StopCoroutine("NoticeProcess");
    }

    /// <summary>
    /// Процесс окончания игры
    /// </summary>
    protected IEnumerator EndLevelProcess()
    {
        SpecialFunctions.SetFade(true);
        SpecialFunctions.Player.GetComponent<HeroController>().SetImmobile(true);
        yield return new WaitForSeconds(endLevelTime);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.activeObject == gameObject)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif //UNITY_EDITOR
    }

}
