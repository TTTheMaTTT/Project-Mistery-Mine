using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, контролирующий НПС
/// </summary>
public class NPCController : MonoBehaviour, IInteractive
{

    #region delegates

    public delegate void storyActionDelegate(StoryAction _action);

    #endregion //delegates

    #region dictionaries

    private Dictionary<string, storyActionDelegate> storyActionBase = new Dictionary<string, storyActionDelegate>(); //Словарь сюжетных действий
    public Dictionary<string, storyActionDelegate> StoryActionBase { get { return storyActionBase; } }

    #endregion //dictionaries

    #region eventHandlers

    public EventHandler<StoryEventArgs> SpeechSaidEvent;

    #endregion //eventHandlers

    #region fields

    protected Animator anim;

    [SerializeField]
    protected List<Speech> speeches=new List<Speech>();//Реплики, которые может произнести этот персонаж

    #endregion //fields

    protected virtual void Awake ()
    {
        Initialize();    
	}
	
	protected virtual void Update ()
    {
	
	}

    protected virtual void Initialize()
    {
        anim = GetComponent<Animator>();
    }

    protected virtual void FormDictionaries()
    {
        storyActionBase = new Dictionary<string, storyActionDelegate>();

        storyActionBase.Add("speechAction", SpeechAction);
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
            SpecialFunctions.gameController.StartDialog(this, speeches[0]);
        }
    }

    public virtual void StopTalking()
    {
        anim.Play("Idle");
    }

    public void SpeechSaid(string speechName)
    {
        SpecialFunctions.StartStoryEvent(this, SpeechSaidEvent, new StoryEventArgs(speechName,0));
    }

    #region storyActions

    /// <summary>
    /// Диалоговое действие
    /// </summary>
    public void SpeechAction(StoryAction _action)
    {
        Speech _speech = speeches.Find(x => (x.speechName == _action.id2));
        if (_speech != null)
        {
            if (_action.id1 == "change speech")
            {
                speeches[0] = _speech;
            }
            else if (_action.id1 == "talk")
            {
                speeches[0] = _speech;
                Talk();
            }
        }
    }

    #endregion //storyActions

}
