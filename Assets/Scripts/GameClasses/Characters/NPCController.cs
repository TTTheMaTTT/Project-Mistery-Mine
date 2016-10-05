using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, контролирующий НПС
/// </summary>
public class NPCController : MonoBehaviour, IInteractive
{

    #region fields

    protected Animator anim;

    [SerializeField]
    protected List<Speech> speeches=new List<Speech>();//Реплики, которые может произнести этот персонаж

    #endregion //fields

    protected virtual void Awake ()
    {
        Initialize();    
	}
	
	protected virtual void Update () {
	
	}

    protected virtual void Initialize()
    {
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Функция взаимодействия с объектом
    /// </summary>
    public virtual void Interact()
    {
        Talk();
    }

    /// <summary>
    /// Поговорить
    /// </summary>
    protected virtual void Talk()
    {
        if (speeches.Count > 0)
        {
            anim.Play("Talk");
            GameObject.FindGameObjectWithTag("gameController").GetComponent<GameController>().StartDialog(transform, speeches[0]);
        }
    }

    public virtual void StopTalking()
    {
        anim.Play("Idle");
    }

}
