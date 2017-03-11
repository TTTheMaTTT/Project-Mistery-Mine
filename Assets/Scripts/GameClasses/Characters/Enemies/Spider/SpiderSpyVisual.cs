using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Специальное визуальное отображение для паука-лазутчика
/// </summary>
public class SpiderSpyVisual : SpiderVisual
{

    /// <summary>
    /// Визуальное отображение смерти
    /// </summary>
    protected override void Death(string id, int argument)
    {
        transform.SetParent(null);
        StartCoroutine("DeathProcess");
    }

    /// <summary>
    /// Процесс смерти персонажа
    /// </summary>
    IEnumerator DeathProcess()
    {
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

}
