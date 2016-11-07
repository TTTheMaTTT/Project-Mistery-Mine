using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт объекта, что создаёт новые капли воды
/// </summary>
public class WaterdropSpawner : MonoBehaviour
{
    #region fields

    [SerializeField]protected GameObject waterdrop;

    #endregion //fields

    #region parametres

    [SerializeField]protected float phase=0f;
    [SerializeField]protected float period = 4f;

    #endregion //parametres

    protected void Awake()
    {
        StartCoroutine(DropProcessWithPhase());
    }

    /// <summary>
    /// Учесть задержку, и начать спавн объектов
    /// </summary>
    protected IEnumerator DropProcessWithPhase()
    {
        yield return new WaitForSeconds(phase);
        StartCoroutine(DropProcess());
    }

    /// <summary>
    /// Процесс спавна, учитывающий период
    /// </summary>
    protected IEnumerator DropProcess()
    {
        Instantiate(waterdrop, transform.position, transform.rotation);
        yield return new WaitForSeconds(period);
        StartCoroutine(DropProcess());
    }

}
