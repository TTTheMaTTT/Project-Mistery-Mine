using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт, который переодически заставляет духа говорить реплики
/// </summary>
public class ImpatientSpiritScript : MonoBehaviour
{

    #region consts

    private const float minWaitTime = 15f, maxWaitTime = 30f;

    #endregion //const

    #region fields

    private NPCController npc;
    [SerializeField]List<Dialog> dialogs = new List<Dialog>();

    #endregion //fields

    public void Start()
    {
        npc = transform.parent.GetComponent<NPCController>();
        StartCoroutine(WaitProcess());
    }

    IEnumerator WaitProcess()
    {
        yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        npc.StartDialog(dialogs[Random.Range(0, dialogs.Count)]);
        StartCoroutine(WaitProcess());
    }

}
