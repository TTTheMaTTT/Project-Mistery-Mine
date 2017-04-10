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
    protected const float talkDistance = 2f;//Максимальное расстояние, на котором ещё может произойти диалог

    #endregion //consts

    #region delegates

    public delegate void storyActionDelegate(StoryAction _action);

    #endregion //delegates

    #region dictionaries

    private Dictionary<string, storyActionDelegate> storyActionBase = new Dictionary<string, storyActionDelegate>(); //Словарь сюжетных действий
    public Dictionary<string, storyActionDelegate> StoryActionBase { get { return storyActionBase; } }

    #endregion //dictionaries

    #region fields

    protected Animator anim;

    [SerializeField]
    protected List<Dialog> dialogs = new List<Dialog>();//Диалоги, что могут произойти с этим персонажем
    public List<Dialog> Dialogs { get { return dialogs; } }

    protected bool canTalk = true;//Может ли персонаж разговаривать
    //public bool CanTalk { get { return canTalk; } set {if (value) StartTalking(); else StopTalking(); } }

    public List<DropClass> NPCDrop = new List<DropClass>();//Что может вывалить НПС, при выполнении его задания (или при иных условиях)
    protected SpriteRenderer sRenderer;

    #endregion //fields

    #region parametres

    protected bool spoken = false;

    [SerializeField]protected DialogModEnum speechMod;
    [SerializeField]protected int dialogArgument1, dialogArgument2;

    [SerializeField][HideInInspector]protected int id;

    protected Color outlineColor = Color.yellow;//Цвет контура
    protected bool possibleTalk = true;//Возможно ли впринципе говорить с персонажем
    [SerializeField]protected bool canTurn = true;//Поворачивается ли НПС к персонажу для разговора?
    public bool CanTurn { get { return canTurn; } }

    [NonSerialized][HideInInspector]public bool waitingForDialog=false;//Ждёт ли НПС того, чтобы воспроизвести диалог
    [HideInInspector]public List<Dialog> waitDialogs = new List<Dialog>();//Диалоги, которые ожидают своего воспроизведения
    public bool considerDistance = false;

    #endregion //parametres

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void Update()
    {
        if (considerDistance && waitingForDialog)
        {
            if (Vector2.SqrMagnitude(SpecialFunctions.player.transform.position - transform.position) < talkDistance * talkDistance)
            {
                if (waitDialogs.Count > 0)
                    foreach (Dialog _dialog in waitDialogs)
                        SpecialFunctions.dialogWindow.AddDialogInQueue(new DialogQueueElement(_dialog, this));
            }
            else
            {
                if (waitDialogs.Count == 0)
                    waitDialogs = SpecialFunctions.dialogWindow.RemoveDialogsFromQueue(this);
            }
        }
    }

    /*protected virtual void OnDestroy()
    {
        List<Dialog> removeDialogs = SpecialFunctions.dialogWindow.RemoveDialogsFromQueue(this);
    }*/

    protected virtual void Initialize()
    {
        waitDialogs = new List<Dialog>();
        if (GetComponent<CharacterController>() != null)
            anim = GetComponentInChildren<Animator>();
        else
            anim = GetComponent<Animator>();
        sRenderer = GetComponent<SpriteRenderer>();
        FormDictionaries();
    }

    protected virtual void FormDictionaries()
    {
        storyActionBase = new Dictionary<string, storyActionDelegate>();

        storyActionBase.Add("speechAction", SpeechAction);
        storyActionBase.Add("dropAction", GiveDrop);
        storyActionBase.Add("animate", StoryAnimate);
        storyActionBase.Add("destroy", StoryDestroy);
    }

    /// <summary>
    /// Начать диалог (в отличие от функции talk, эта функция не может вызваться при взаимодействии, а только при использовании сюжетного действия)
    /// </summary>
    protected virtual void StartDialog(Dialog _dialog)
    {
        if (!SpecialFunctions.gameController.StartDialog(_dialog))
        {
            waitingForDialog = true;
            if (considerDistance && Vector2.SqrMagnitude(SpecialFunctions.player.transform.position - transform.position) < talkDistance * talkDistance)
                waitDialogs.Add(_dialog);
            else
                SpecialFunctions.dialogWindow.AddDialogInQueue(new DialogQueueElement(_dialog, this));
        }
    }

    /// <summary>
    /// Начать диалог с заданным названием (в отличие от функции talk, эта функция не может вызваться при взаимодействии, а только при использовании сюжетного действия)
    /// </summary>
    public virtual void StartDialog(string _dialogName)
    {
        Dialog _dialog = dialogs.Find(x => x.dialogName == _dialogName);
        if (_dialog != null)
            if (!SpecialFunctions.gameController.StartDialog(_dialog))
            {
                waitingForDialog = true;
                if (considerDistance && Vector2.SqrMagnitude(SpecialFunctions.player.transform.position - transform.position) < talkDistance * talkDistance)
                    waitDialogs.Add(_dialog);
                else
                    SpecialFunctions.dialogWindow.AddDialogInQueue(new DialogQueueElement(_dialog, this));
            }
    }

    /// <summary>
    /// Начать диалог через некоторое время
    /// </summary>
    IEnumerator StartDialogProcess(Dialog _dialog, float _time)
    {
        yield return new WaitForSeconds(_time);
        StartDialog(_dialog);
    }

    /// <summary>
    /// Поговорить
    /// </summary>
    protected virtual void Talk()
    {
        if (dialogs.Count > 0)
        {
            if (anim != null)
                anim.Play("Talk");
            Dialog dialog = null;
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
                            dialog = dialogs[UnityEngine.Random.Range(0, dialogs.Count)];
                        else
                            dialog = dialogs[UnityEngine.Random.Range(dialogArgument1, dialogArgument2)];
                        break;
                    }
                case DialogModEnum.usual:
                    {
                        dialog = dialogs[0];
                        break;
                    }
            }
            if (!SpecialFunctions.gameController.StartDialog(this, dialog))
            {
                waitingForDialog = true;
                if (considerDistance && Vector2.SqrMagnitude(SpecialFunctions.player.transform.position - transform.position) < talkDistance * talkDistance)
                    waitDialogs.Add(dialog);
                else
                    SpecialFunctions.dialogWindow.AddDialogInQueue(new DialogQueueElement(dialog, this));
            }
        }
        if (!spoken)
        {
            spoken = true;
            SpecialFunctions.statistics.ConsiderStatistics(this);
        }
    }

    /// <summary>
    /// Начать разговор через некоторое время
    /// </summary>
    /// <param name="_time">Время, через которое начнётся диалог</param>
    protected IEnumerator StartTalkProcess(float _time)
    {
        yield return new WaitForSeconds(_time);
        Talk();
    }

    /// <summary>
    /// Начать разговор
    /// </summary>
    public virtual void StartTalking()
    {
        StartCoroutine(StartTalkingProcess());
    }

    /// <summary>
    /// Прекратить разговор
    /// </summary>
    public virtual void StopTalking()
    {
        if (anim != null)
            anim.Play("Idle");
        canTalk = false;
        StartCoroutine(NoTalkingProcess());
        //SetOutline(true);
    }

    /// <summary>
    /// Процесс, в течение которого нельзя разговаривать
    /// </summary>
    protected IEnumerator NoTalkingProcess()
    {
        yield return new WaitForSeconds(disableTalkTime);
        canTalk = true;
    }

    protected IEnumerator StartTalkingProcess()
    {
        yield return new WaitForSeconds(.1f);
        if (anim != null)
            anim.Play("Talk");

    }

    /// <summary>
    /// Вернуть список всех реплик
    /// </summary>
    public List<string> GetSpeeches()
    {
        List<string> newSpeeches = new List<string>();
        for (int i = 0; i < dialogs.Count; i++)
            for (int j = 0; j < dialogs[i].speeches.Count; j++)
                newSpeeches.Add(dialogs[i].speeches[j].speechName);
        return newSpeeches;
    }

    /// <summary>
    /// Вернуть названия анимаций
    /// </summary>
    public List<string> GetAnimations()
    {
        List<string> newAnimations = new List<string>();
        anim = GetComponent<Animator>();
        if (anim == null)
            return newAnimations;
        AnimationClip[] animClips = anim.runtimeAnimatorController.animationClips;
        foreach (AnimationClip animClip in animClips)
            newAnimations.Add(animClip.name);
        return newAnimations;
    }

    #region storyActions

    /// <summary>
    /// Диалоговое действие
    /// </summary>
    public void SpeechAction(StoryAction _action)
    {
        Dialog _dialog = null;
        _dialog = dialogs.Find(x => (x.dialogName == _action.id2));
        if (_dialog != null)
        {
            if (_action.id1 == "change speech")
                dialogs[0] = _dialog;
            else if (_action.id1 == "talk")
            {
                dialogs[0] = _dialog;
                StartCoroutine(StartTalkProcess(_action.argument / 10f));
            }
            else if (_action.id1 == "startDialog")
                StartCoroutine(StartDialogProcess(_dialog, _action.argument / 10f));
            else if (_action.id1 == "setTalkPossibility")
                possibleTalk = _action.argument > 0;
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

    /// <summary>
    /// Анимирование персонажа в результате скриптового события
    /// </summary>
    public void StoryAnimate(StoryAction _action)
    {
        if (anim != null)
            anim.Play(_action.id1);
    }

    /// <summary>
    /// Уничтожение объекта в результате скриптового события
    /// </summary>
    public void StoryDestroy(StoryAction _action)
    {
        Destroy(gameObject);
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
            waitingForDialog = npcData.waiting;
            foreach (string dialogName in npcData.waitDialogs)
            {
                Dialog _dialog = dialogs.Find(x => x.dialogName == dialogName);
                if (_dialog != null)
                    waitDialogs.Add(_dialog);
            }
            if (transform.parent != null ? transform.parent.GetComponent<DialogObject>() : false)
                transform.parent.position = npcData.position;//Частые случаи, когда НПС находится внутри другого объекта, 
                                                            //и он должен быть в координатном нуле относительно него
            else
                transform.position = npcData.position;
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
        return new NPCData(id, dialogs,gameObject.name, transform.position, waitingForDialog, waitDialogs);
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

    /// <summary>
    /// Можно ли провзаимодействовать с НПС в данный момент?
    /// </summary>
    public virtual bool IsInteractive()
    {
        return SpecialFunctions.battleField.enemiesCount == 0 && possibleTalk && SpecialFunctions.dialogWindow.CurrentDialog == null;
    }

    #endregion //IInteractive

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public virtual List<string> actionNames()
    {
        return new List<string>() { "speechAction", "dropAction", "animate", "destroy"};
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() {
                                                    { "speechAction", new List<string> {"change speech", "talk", "startDialog", "setTalkPossibility" } },
                                                    {"dropAction", new List<string> {"","one" } },
                                                    { "animate", GetAnimations() } ,
                                                    {"destroy",new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() {
                                                    { "speechAction", dialogs.ConvertAll<string>(x => x.dialogName)},
                                                    { "dropAction", NPCDrop.ConvertAll<string>(x=>x.gameObject.name)},
                                                    { "animate", new List<string>() },
                                                    {"destroy",new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь id-шников, настраивающих функцию проверки
    /// </summary>
    public virtual Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>() { { "", new List<string>()},
                                                        { "compare", new List<string>()},
                                                        { "compareSpeech", GetSpeeches()},
                                                        { "compareHistoryProgress",SpecialFunctions.statistics.HistoryBase.stories.ConvertAll(x=>x.storyName)}};
    }

    /// <summary>
    /// Возвращает ссылку на сюжетное действие, соответствующее данному имени
    /// </summary>
    public StoryAction.StoryActionDelegate GetStoryAction(string s)
    {
        if (storyActionBase.ContainsKey(s))
            return storyActionBase[s].Invoke;
        else
            return null;
    }

    #endregion //IHaveStory

}
