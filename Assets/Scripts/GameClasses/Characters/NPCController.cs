using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, контролирующий НПС
/// </summary>
public class NPCController : MonoBehaviour, IInteractive, IHaveStory
{

    #region consts

    protected const float disableTalkTime = .2f;//Время, в течение которого нельзя снова заговорить с персонажем
    private const float pushForceY = 50f, pushForceX = 25f;//С какой силой НПС выбрасывает дроп

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

    public List<DropClass> NPCDrop = new List<DropClass>();//Что может вывалить НПС, при выполнении его задания (или при иных условиях)
    protected SpriteRenderer sRenderer;

    #endregion //fields

    #region parametres

    protected bool spoken=false;

    [SerializeField]protected DialogModEnum speechMod;
    [SerializeField]protected int dialogArgument1, dialogArgument2;

    [SerializeField][HideInInspector]protected int id;

    protected Color outlineColor = Color.yellow;//Цвет контура

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
        sRenderer = GetComponent<SpriteRenderer>();
        FormDictionaries();
    }

    protected virtual void FormDictionaries()
    {
        storyActionBase = new Dictionary<string, storyActionDelegate>();

        storyActionBase.Add("speechAction", SpeechAction);
        storyActionBase.Add("dropAction", GiveDrop);
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
    /// Начать разговор
    /// </summary>
    public virtual void StartTalking()
    {
        if (anim != null)
            anim.Play("Talk");
        SetOutline(false);
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
        SetOutline(true);
    }

    /// <summary>
    /// Процесс, в течение которого нельзя разговаривать
    /// </summary>
    protected IEnumerator NoTalkingProcess()
    {
        yield return new WaitForSeconds(disableTalkTime);
        canTalk = true;

    }

    /// <summary>
    /// Событие "Сказана реплика"
    /// </summary>
    /// <param name="speechName"></param>
    public void SpeechSaid(string speechName)
    {
        SpecialFunctions.StartStoryEvent(this, SpeechSaidEvent, new StoryEventArgs(speechName,0));
    }

    /// <summary>
    /// Вернуть список всех реплик
    /// </summary>
    public List<string> GetSpeeches()
    {
        List<string> newSpeeches=new List<string>();
        for (int i = 0; i < dialogs.Count; i++)
            for (int j = 0; j < dialogs[i].speeches.Count; j++)
                newSpeeches.Add(dialogs[i].speeches[j].speechName);
        return newSpeeches;
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

    /// <summary>
    /// НПС отдаёт предмет
    /// </summary>
    public void GiveDrop(StoryAction _action)
    {
        if (_action.id1 == "one")
        {
            if (_action.id2 != string.Empty)
            {
                DropClass drop = NPCDrop.Find(x => (x.gameObject.name == _action.id2));
                if (drop != null)
                {
                    GameObject _drop = Instantiate(drop.gameObject, transform.position + Vector3.up * .05f, transform.rotation) as GameObject;
                    if (_drop.GetComponent<Rigidbody2D>() != null)
                    {
                        _drop.GetComponent<Rigidbody2D>().AddForce(new Vector2(UnityEngine.Random.RandomRange(-pushForceX, pushForceX), pushForceY));
                    }
                }
            }

            else if (_action.argument < NPCDrop.Count)
            {
                GameObject _drop = Instantiate(NPCDrop[_action.argument].gameObject, transform.position + Vector3.up * .05f, transform.rotation) as GameObject;
                if (_drop.GetComponent<Rigidbody2D>() != null)
                {
                    _drop.GetComponent<Rigidbody2D>().AddForce(new Vector2(UnityEngine.Random.RandomRange(-pushForceX, pushForceX), pushForceY));
                }
            }
        }
        else
        {
            foreach (DropClass drop in NPCDrop)
            {
                GameObject _drop = Instantiate(drop.gameObject, transform.position + Vector3.up * .05f, transform.rotation) as GameObject;
                if (_drop.GetComponent<Rigidbody2D>() != null)
                {
                    _drop.GetComponent<Rigidbody2D>().AddForce(new Vector2(UnityEngine.Random.RandomRange(-pushForceX, pushForceX), pushForceY));
                }
            }
        }
    }

    #endregion //storyActions

    #region IHaveID

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

    #endregion //IHaveID

    #region IInteractive

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
    /// Отрисовать контур, если происзодит взаимодействие (или убрать этот контур)
    /// </summary>
    public virtual void SetOutline(bool _outline)
    {
        if (sRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            sRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_Outline", _outline ? 1f : 0);
            mpb.SetColor("_OutlineColor", outlineColor);
            sRenderer.SetPropertyBlock(mpb);
        }
    }

    #endregion //IInteractive

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public virtual List<string> actionNames()
    {
        return new List<string>() { "speechAction", "dropAction"};
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() {
                                                    { "speechAction", new List<string> {"change speech", "talk" } },
                                                    {"dropAction", new List<string> {"","one" } } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() {
                                                    { "speechAction", dialogs.ConvertAll<string>(x => x.dialogName)},
                                                    { "dropAction", NPCDrop.ConvertAll<string>(x=>x.gameObject.name)} };
    }

    /// <summary>
    /// Вернуть словарь id-шников, настраивающих функцию проверки
    /// </summary>
    public virtual Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>() { { "", new List<string>()},
                                                        { "compare", new List<string>()},
                                                        { "compareSpeech", GetSpeeches()} };
    }

    #endregion //IHaveStory

}
