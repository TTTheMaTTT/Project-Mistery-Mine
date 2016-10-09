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
    protected List<Dialog> dialogs=new List<Dialog>();//Диалоги, что могут произойти с этим персонажем

    #endregion //fields

    #region parametres

    protected bool spoken=false;

    #endregion //parametres

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
        FormDictionaries();
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
        if (dialogs.Count > 0)
        {
            if (anim!=null)
                anim.Play("Talk");
            SpecialFunctions.gameController.StartDialog(this, dialogs[0]);
        }
        if (!spoken)
        {
            spoken = true;
            SpecialFunctions.statistics.ConsiderStatistics(this);
        }
    }

    public virtual void StopTalking()
    {
        if (anim!=null)
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
        Dialog _dialog = null;
        if (_action.argument > 0)
            _dialog = dialogs[_action.argument];
        else
            _dialog = dialogs.Find(x => (x.dialogName == _action.id2));
        if (_dialog != null)
        {
            if (_action.id1 == "change speech")
            {
                dialogs[0] = _dialog;
            }
            else if (_action.id1 == "talk")
            {
                dialogs[0] = _dialog;
                Talk();
            }
        }
    }

    #endregion //storyActions

}
