using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, контролирующий НПС
/// </summary>
public class NPCController : MonoBehaviour, IInteractive
{

    #region consts

    protected const float disableTalkTime = .2f;//Время, в течение которого нельзя снова заговорить с персонажем

    #endregion //consts

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

    protected bool canTalk=true;//Может ли персонаж разговаривать
    public bool CanTalk { get { return canTalk; } set { canTalk = value; } }

    #endregion //fields

    #region parametres

    protected bool spoken=false;

    [SerializeField]protected DialogModEnum speechMod;
    [SerializeField]protected int dialogArgument1, dialogArgument2;

    [SerializeField][HideInInspector]protected int id;

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
        if (canTalk)
        {
            Talk();
            canTalk = false;
        }
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
            Dialog dialog=null;
            switch (speechMod)
            {
                case DialogModEnum.one:
                    {
                        dialog = dialogs[dialogArgument1];
                        break;
                    }
                case DialogModEnum.random:
                    {
                        if (dialogArgument1 == 0 || dialogArgument2 == 0)
                            dialog = dialogs[UnityEngine.Random.Range(0,dialogs.Count)];
                        else
                            dialog = dialogs[UnityEngine.Random.Range(dialogArgument1,dialogArgument2)];
                        break;
                    }
                case DialogModEnum.usual:
                    {
                        dialog = dialogs[0];
                        break;
                    }
            }
            SpecialFunctions.gameController.StartDialog(this, dialog);
        }
        if (!spoken)
        {
            spoken = true;
            SpecialFunctions.statistics.ConsiderStatistics(this);
        }
    }

    /// <summary>
    /// Прекратить разговор
    /// </summary>
    public virtual void StopTalking()
    {
        if (anim!=null)
            anim.Play("Idle");
        canTalk = false;
        StartCoroutine(NoTalkingProcess());
    }

    /// <summary>
    /// Процесс, в течение которого нельзя разговаривать
    /// </summary>
    protected IEnumerator NoTalkingProcess()
    {
        yield return new WaitForSeconds(disableTalkTime);
        canTalk = true;

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

    /// <summary>
    /// Вернуть id персонажа
    /// </summary>
    public int GetID()
    {
        return id;
    }

    /// <summary>
    /// Выставить объекту его id
    /// </summary>
    /// <param name="_id"></param>
    public void SetID(int _id)
    {
        id = _id;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif //UNITY_EDITOR
    }

    /// <summary>
    /// Настроить персонажа в соответствии с сохранёнными данными
    /// </summary>
    public void SetData(InterObjData _intObjData)
    {
        NPCData npcData = (NPCData)_intObjData;
        if (npcData != null)
        {
            List<Dialog> _dialogs = dialogs;
            dialogs = new List<Dialog>();
            for (int i = 0; i < npcData.dialogs.Count; i++)
            {
                Dialog dialog = _dialogs.Find(x => (x.dialogName == npcData.dialogs[i]));
                if (dialog != null)
                    dialogs.Add(dialog);
            }
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif //UNITY_EDITOR
    }

    /// <summary>
    /// Вернуть сохраняемые данные персонажа
    /// </summary>
    public InterObjData GetData()
    {
        return new NPCData(id, dialogs);
    }

}
