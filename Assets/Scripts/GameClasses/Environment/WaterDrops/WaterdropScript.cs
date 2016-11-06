using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, управляющий капелькой воды
/// </summary>
public class WaterdropScript : MonoBehaviour
{
    #region consts

    protected const float groundRadius = .02f;
    protected const string lName = "ground";

    #endregion //consts

    #region parametres

    protected bool set = false;

    #endregion //parametres

    void Awake()
    {
        StartCoroutine(SetProcess());
    }

    void FixedUpdate()
    {
        if (Physics2D.OverlapCircle(transform.position, groundRadius, LayerMask.GetMask(lName))&& set)
            Destroy(gameObject);
    }

    IEnumerator SetProcess()
    {
        set = false;
        yield return new WaitForSeconds(.2f);
        set = true;
    }

}

