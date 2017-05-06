using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, управляющий диалоговым окном
/// </summary>
public class DialogWindowScript : MonoBehaviour
{

    #region consts

    protected const float noInputTime = .5f;//сколько времени нельзя будет ввести пропуск диалога

    #endregion //consts

    #region dictionaries

    protected Dictionary<int, DialogObject> dialogObjectsDictionary = new Dictionary<int, DialogObject>();//Словарь всех диалоговых объектов

    #endregion //dictionaries

    #region eventHandlers

    public EventHandler<DialogEventArgs> DialogEventHandler;//Обработчик события об изменении состояния диалога
    public EventHandler<StoryEventArgs> SpeechSaidEvent;//Обработчик события "Была сказана реплика"

    #endregion //eventHandlers

    #region fields

    protected Canvas canvas;

    protected Text speechText;
    protected Image portrait;
    protected GameObject speechPanel;
    protected GameObject answerPanel;
    protected UIPanel answerUIPanel;
    protected DialogAnswerButton answerButton1, answerButton2, answerButton3;
    protected Text answerText;

    protected Transform hero;
    protected NPCController npc;
    protected CameraController cam;

    protected Dialog currentDialog = null;
    public Dialog CurrentDialog { get { return currentDialog; } }

    protected Speech currentSpeech = null;
    public Speech CurrentSpeech { get { return currentSpeech; }
        set { currentSpeech = value; if (value != null) ExecuteSpeech(value);
            else { speechText.text = null; portrait.sprite = null; cam.ChangeCameraMod(CameraModEnum.player); } } }

    protected List<DialogObject> dialogObjs = new List<DialogObject>();

    private List<DialogQueueElement> dialogQueue = new List<DialogQueueElement>();//Очередь из нереализованных диалогов (да да, это список, а не очередь, но ведём мы с этим списком, как с очередью, за исключением 
                                                                                 //в некоторых случаях удобны его свойстав, как списка
    public List<DialogQueueElement> DialogQueue { get { return dialogQueue; }}

    #endregion //fields

    #region parametres

    public bool activated { get { return canvas.enabled; } }

    protected float prevScale;
    protected bool noInput = false;//Если true, то диалог пропустить нельзя

    [SerializeField]
    private int dialogObjectMaxID = 0;//Максимальный id объекта - участника диалога
    public int DialogObjectMaxID { get { if (dialogObjectMaxID == 0) SetDialogMaxID(); return dialogObjectMaxID; } }

    protected bool waitingForInput = false;//Ожидает ли диалоговое окно нажатие на клавишу, чтобы продолжить диалог
    protected float fadeSpeed = 0f;//Скорость затухания экрана
    protected float waitingTime = 0f;//Время ожидания следующей реплики

    #endregion //parametres

    protected void Awake()
    {
        Initialize();
    }

    void Update()
    {
        if (canvas.enabled)
        {
            if (waitingForInput)
            {
                Event e = Event.current;
                if ((Input.anyKeyDown || InputCollection.instance.GetButtonDown("Submit"))?
                    !InputCollection.instance.GetButtonDown("Horizontal") &&
                    !InputCollection.instance.GetButtonDown("Vertical") &&
                    !InputCollection.instance.GetButtonDown("Menu") && !noInput && InterfaceWindow.openedWindow == null : false)
                    NextSpeech();
            }

            if (answerPanel.active)
                if (UIElementScript.activePanel != answerUIPanel ? InterfaceWindow.openedWindow == null : false)
                {
                    answerUIPanel.SetActive();
                    if (UIElementScript.activeElement)
                    {
                        UIElementScript.activeElement.SetInactive();
                        UIElementScript.activeElement = null;
                    }
                }
            

        }
    }

    /// <summary>
    /// Проверить все объекты на сцене и определить максимальный id диалоговых объектов
    /// </summary>
    void SetDialogMaxID()
    {
        CheckPlayerDialogObject();
        DialogObject[] dialogObjs = FindObjectsOfType<DialogObject>();
        foreach (DialogObject dObject in dialogObjs)
            if (dialogObjectMaxID - 1 < dObject.ID)
                dialogObjectMaxID = dObject.ID + 1;
#if UNITY_EDITOR
        UnityEditor.SerializedObject serDialogWindow = new UnityEditor.SerializedObject(this);
        serDialogWindow.FindProperty("dialogObjectMaxID").intValue = dialogObjectMaxID;
        serDialogWindow.ApplyModifiedProperties();
#endif
    }

    /// <summary>
    /// Запрос на новый id диалогового объекта
    /// </summary>
    /// <returns></returns>
    public int GetDialogID()
    {
        if (dialogObjectMaxID == 0)
            SetDialogMaxID();
        dialogObjectMaxID++;
#if UNITY_EDITOR
        UnityEditor.SerializedObject serDialogWindow = new UnityEditor.SerializedObject(this);
        serDialogWindow.FindProperty("dialogObjectMaxID").intValue = dialogObjectMaxID;
        serDialogWindow.ApplyModifiedProperties();
#endif
        return dialogObjectMaxID - 1;
    }

    /// <summary>
    /// Проверить, находится ли объект в словаре диалоговых объектов, и если нет, то занести
    /// </summary>
    public void CheckDialogObject(DialogObject _dobj)
    {
        if (dialogObjectsDictionary != null ? dialogObjectsDictionary.ContainsKey(_dobj.ID) : false)
            dialogObjectsDictionary.Add(_dobj.ID, _dobj);
    }

    /// <summary>
    /// Создать словарь диалоговых объектов, ключами к которым будут их id
    /// </summary>
    void InitializeDialogDictionary()
    {
        if (dialogObjectMaxID == 0)
            SetDialogMaxID();
        dialogObjectsDictionary = new Dictionary<int, DialogObject>();
        DialogObject[] dObjects = FindObjectsOfType<DialogObject>();
        foreach (DialogObject dObj in dObjects)
            if (!dialogObjectsDictionary.ContainsKey(dObj.ID))
                dialogObjectsDictionary.Add(dObj.ID, dObj);
    }

    /// <summary>
    /// Проверить, есть ли у героя компонент участника диалога с id=0
    /// </summary>
    void CheckPlayerDialogObject()
    {
        GameObject player = SpecialFunctions.Player;
        DialogObject dialogObj = player.GetComponent<DialogObject>();
        if (dialogObj == null)
        {
            dialogObj = player.AddComponent<DialogObject>();
            dialogObj.Initialize();
            if (dialogObjectMaxID == 0)
                dialogObjectMaxID++;
        }
    }

    /// <summary>
    /// Начать диалог (Обычно эта функция вызывается, когда герой сам подходит к НПС и начинает с ним разговор)
    /// </summary>
    public bool BeginDialog(NPCController _npc, Dialog dialog)
    {
        if (currentDialog != null)
            return false;
        _npc.waitingForDialog = true;
        ExecuteDialog(_npc, dialog);
        return true;
    }

    /// <summary>
    /// Воспроизвести диалог
    /// </summary>
    /// <param name="_npc">НПС, с которым происходит диалог</param>
    /// <param name="dialog">Воспроизводимый диалог</param>
    public void ExecuteDialog(NPCController _npc, Dialog dialog)
    {
        if (_npc != null ? _npc.waitingForDialog : false)
        {
            npc = _npc;
            //Повернуть НПС к герою
            prevScale = npc.transform.localScale.x;
            if (npc.CanTurn? npc.transform.lossyScale.x * (hero.position - npc.transform.position).x < 0f: false)
                npc.transform.localScale += new Vector3(-2f * prevScale, 0f);
            npc.waitingForDialog = NPCHasDialogFromQueue(npc);
        }
        currentDialog = dialog;
        currentDialog.stage = 0;
        answerPanel.SetActive(false);

        GetAllDialogObjectsInDialog(dialog);

        foreach (DialogObject _dObject in dialogObjs)
        {
            if (_dObject == null)
                continue;
            _dObject.SetImmobile(dialog.stopGameProcess);
            _dObject.SetTalking(true);
        }

        CurrentSpeech = dialog.speeches[0];

        canvas.enabled = true;

        if (currentDialog.pause)
        {
            SpecialFunctions.PauseGame();
            SpecialFunctions.totalPaused = true;
        }

        if (currentDialog.sentPatrolHome)
            SpecialFunctions.gameController.SetPatrollingEnemiesToHome();

        StartCoroutine(NoInputProcess());
        OnDialogChange(new DialogEventArgs(true, dialog.stopGameProcess));
    }

    /// <summary>
    /// Начать диалог
    /// </summary>
    public bool BeginDialog(Dialog dialog)
    {
        if (currentDialog != null)
            return false;
        ExecuteDialog(null, dialog);
        return true;
    }

    /// <summary>
    /// Функция завершения разговора
    /// </summary>
    public void StopDialog()
    {
        CurrentSpeech = null;
        canvas.enabled = false;

        GetAllDialogObjectsInDialog(currentDialog);
        foreach (DialogObject _dObject in dialogObjs)
        {
            if (_dObject == null)
                continue;
            if (currentDialog.stopGameProcess)
                _dObject.SetImmobile(false);
            _dObject.SetTalking(false);
        }

        if (npc != null)
        {
            //Повернуть НПС в изначальную ориентацию(если это он инициализировал диалог)
            Vector3 vect = npc.transform.localScale;
            if (npc.CanTurn)
                npc.transform.localScale = new Vector3(prevScale, vect.y, vect.z);
            npc.StopTalking();
        }

        SpecialFunctions.totalPaused = false;
        SpecialFunctions.PlayGame();
        SpecialFunctions.SetDefaultFadeSpeed();
        OnDialogChange(new DialogEventArgs(false, currentDialog.stopGameProcess));
        currentDialog = null;
        npc = null;
        dialogObjs.Clear();

        //Реализовать диалоги, стоящие в очереди
        if (dialogQueue.Count > 0)
        {
            DialogQueueElement nextDialog = dialogQueue[0];
            dialogQueue.RemoveAt(0);
            ExecuteDialog(nextDialog.npc, nextDialog.dialog);
        }
    }

    /// <summary>
    /// Убрать диалоги данного НПС из очереди на исполнение
    /// </summary>
    public List<Dialog> RemoveDialogsFromQueue(NPCController _npc)
    {
        List<Dialog> _dialogs = new List<Dialog>();
        for (int i = dialogQueue.Count - 1; i >= 0; i--)
        {
            DialogQueueElement dElement = dialogQueue[i];
            if (dElement.npc == _npc)
            {
                dialogQueue.RemoveAt(i);
                _dialogs.Insert(0, dElement.dialog);
            }
        }
        return _dialogs;

    }

    /// <summary>
    /// Добавить диалог в очередь диалогов
    /// </summary>
    public void AddDialogInQueue(DialogQueueElement _dElement)
    {
        dialogQueue.Add(_dElement);
        //Если нет активного диалога, то сразу же реализовать очередь
        if (currentDialog==null)
        {
            DialogQueueElement dElement = dialogQueue[0];
            dialogQueue.RemoveAt(0);
            ExecuteDialog(dElement.npc, dElement.dialog);
        }
    }

    /// <summary>
    /// Есть ли диалоги данного НПС в очереди на исполнение?
    /// </summary>
    public bool NPCHasDialogFromQueue(NPCController _npc)
    {
        return dialogQueue.Find(x => x.npc == _npc) != null;
    }

    /// <summary>
    /// Возвращает список всех диалоговых объектов, которые используются для воспроизведения выбранного диалога
    /// </summary>
    /// <param name="_dialog">Выбранный диалог</param>
    public List<DialogObject> GetAllDialogObjectsInDialog(Dialog _dialog)
    {
        dialogObjs.Clear();
        List<int> dialogIDs = new List<int>();
        //Первого, кого мы внесём в список - это самого героя - он участник всех диалогов
        dialogObjs.Add(SpecialFunctions.Player.GetComponent<DialogObject>());
        dialogIDs.Add(0);

        foreach (int _dialogID in _dialog.dialogParticipants)
            AddDialogObjectInList(dialogIDs, _dialogID);

        //Теперь пройдёмся по всем параметрам диалога и найдём все диалоговые объекты
        foreach (Speech _speech in _dialog.speeches)
        {
            AddDialogObjectInList(dialogIDs, _speech.camObjectID);

            foreach (SpeechChangePositionClass positionData in _speech.changePositionData)
                AddDialogObjectInList(dialogIDs, positionData.dialogID);

            foreach (SpeechAnimationClass animationData in _speech.animationData)
                AddDialogObjectInList(dialogIDs, animationData.dialogID);

            foreach (SpeechChangeOrientationClass orientationData in _speech.changeOrientationData)
                AddDialogObjectInList(dialogIDs, orientationData.dialogID);
        }

        return dialogObjs;
    }

    /// <summary>
    /// Добавить диалоговый объект в список текущего диалога, если он удовлетворяет всем условиям на использование
    /// </summary>
    /// <param name="_dialogIDs">Список id-шников, у которых соответсвующие им диалоговые объекты УЖЕ находятся в списке</param>
    /// <param name="_dObjID">id объекта, который мы хотим добавить в список</param>
    void AddDialogObjectInList(List<int> _dialogIDs, int _dObjID)
    {
        if (!_dialogIDs.Contains(_dObjID) && dialogObjectsDictionary.ContainsKey(_dObjID))
        {
            _dialogIDs.Add(_dObjID);
            dialogObjs.Add(dialogObjectsDictionary[_dObjID]);
        }
    }

    /// <summary>
    /// Добавить диалоговый объект в словарь
    /// </summary>
    /// <param name="_dObj">Диалоговый объект, что добавляется в словарь</param>
    public void AddDialogObjectInDictionary(DialogObject _dObj)
    {
        if (!dialogObjectsDictionary.ContainsKey(_dObj.ID))
            dialogObjectsDictionary.Add(_dObj.ID, _dObj);
    }

    /// <summary>
    /// Процесс, в течение которого нельзя будет пропускать реплику диалога
    /// </summary>
    IEnumerator NoInputProcess()
    {
        noInput = true;
        if (SpecialFunctions.totalPaused)
            yield return new WaitForSecondsRealtime(noInputTime);
        else
            yield return new WaitForSeconds(noInputTime);
        noInput = false;
    }

    /// <summary>
    /// Процесс ожидания
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitingProcess()
    {
        if (SpecialFunctions.totalPaused)
            yield return new WaitForSecondsRealtime(waitingTime);
        else
            yield return new WaitForSeconds(waitingTime);
        NextSpeech();
    }

    /// <summary>
    /// Процесс затухание,  
    /// </summary>
    IEnumerator FadeInOutProcess()
    {
        SpecialFunctions.SetFade(true);
        if (SpecialFunctions.totalPaused)
            yield return new WaitForSecondsRealtime(waitingTime/2f);
        else
            yield return new WaitForSeconds(waitingTime/2f);
        SpecialFunctions.SetFade(false);
    }

    /// <summary>
    /// Перейти к следующему этапу диалога
    /// </summary>
    public void NextSpeech()
    {
        currentDialog.stage++;
        int stage = currentDialog.stage;
        if (currentDialog.speeches.Count > stage)
            CurrentSpeech = currentDialog.speeches[stage];
        else
        {
            CurrentSpeech = null;
            StopDialog();
        }
    }

    /// <summary>
    /// Функция, которая активирует все действия, связанные с указанной репликой
    /// </summary>
    public void ExecuteSpeech(Speech _speech)
    {

        #region initialization

        if (_speech.speechMod == SpeechModEnum.usual)
        {
            waitingForInput = true;
            StartCoroutine("NoInputProcess");
            answerPanel.SetActive(false);
        }
        else if (_speech.speechMod == SpeechModEnum.answer)
        {
            waitingForInput = false;
            speechPanel.SetActive(false);
            answerPanel.SetActive(true);
            answerButton1.InitializeAnswerButton(_speech.answer1);
            if (_speech.answer3!=null?_speech.answer3.nextDialog != null:false)
            {
                answerButton2.gameObject.SetActive(true);
                answerButton2.InitializeAnswerButton(_speech.answer2);
                answerButton3.InitializeAnswerButton(_speech.answer3);
                answerButton2.GetComponent<UIElementScript>().uiIndex = new UIElementIndex(1, 0);
                answerButton3.GetComponent<UIElementScript>().uiIndex = new UIElementIndex(2, 0);
            }
            else
            {
                answerButton2.gameObject.SetActive(false);
                answerButton3.InitializeAnswerButton(_speech.answer2);
                answerButton3.GetComponent<UIElementScript>().uiIndex = new UIElementIndex(1, 0);
                answerButton2.GetComponent<UIElementScript>().uiIndex = new UIElementIndex(-10, -10);
            }
            if (_speech.hasText)
                answerText.text = _speech.speechText.mlText.GetText(SettingsScript.language);
            else
                answerText.text = "";
        }
        else
        {
            answerPanel.SetActive(false);
            waitingForInput = false;
            waitingTime = _speech.waitTime;
            StartCoroutine("WaitingProcess");
            if (_speech.speechMod != SpeechModEnum.wait)
            {
                fadeSpeed = _speech.fadeSpeed;
                SpecialFunctions.SetFadeSpeed(fadeSpeed);
            }
            if (_speech.speechMod == SpeechModEnum.waitFadeIn)
                SpecialFunctions.SetFade(true);
            else if (_speech.speechMod == SpeechModEnum.waitFadeOut)
                SpecialFunctions.SetFade(false);
            else if (_speech.speechMod == SpeechModEnum.waitFadeInOut)
                StartCoroutine("FadeInOutProcess");
        }

        #endregion //initialization

        #region cam

        CameraModEnum camMod = _speech.camMod;
        bool instantMotion = !(camMod == CameraModEnum.move || camMod == CameraModEnum.objMove || camMod == CameraModEnum.playerMove);
        if (camMod == CameraModEnum.objMove || camMod == CameraModEnum.obj)
        {
            if (dialogObjectsDictionary.ContainsKey(_speech.camObjectID))
            {
                DialogObject dObj = dialogObjectsDictionary[_speech.camObjectID];
                if (dObj != null? dObj.gameObject!= null : false)
                    cam.ChangeCameraTarget(dObj.gameObject, instantMotion);
            }
        }
        else if (camMod == CameraModEnum.position || camMod == CameraModEnum.move)
            cam.ChangeCameraTarget(_speech.camPosition, instantMotion);
        else
            cam.ChangeCameraMod(camMod);

        #endregion //cam

        #region hasText

        if (_speech.speechMod != SpeechModEnum.answer && _speech.hasText)
        {
            portrait.sprite = _speech.speechText.portrait;
            speechText.text = _speech.speechText.mlText.GetText(SettingsScript.language);
            speechPanel.SetActive(true);
        }
        else
            speechPanel.SetActive(false);

        #endregion //hasText

        #region hasChangePosition

        if (_speech.hasPositionChange)
        {
            foreach (SpeechChangePositionClass changePosition in _speech.changePositionData)
                if (dialogObjectsDictionary.ContainsKey(changePosition.dialogID))
                {
                    DialogObject dObj = dialogObjectsDictionary[changePosition.dialogID];
                    if (dObj != null ? dObj.gameObject != null : false)
                        dObj.SetPosition(changePosition.position);
                }
        }

        #endregion //hasChangePosition

        #region hasChangeOrientation

        if (_speech.hasOrientationChange)
        {
            foreach (SpeechChangeOrientationClass changeOrientation in _speech.changeOrientationData)
                if (dialogObjectsDictionary.ContainsKey(changeOrientation.dialogID))
                {
                    DialogObject dObj = dialogObjectsDictionary[changeOrientation.dialogID];
                    if (dObj != null ? dObj.gameObject != null : false)
                        dObj.SetOrientation(changeOrientation.orientation);
                }
        }

        #endregion //hasChangeOrientation

        #region hasAnimation

        if (_speech.hasAnimation)
        {
            foreach (SpeechAnimationClass sAnimation in _speech.animationData)
                if (dialogObjectsDictionary.ContainsKey(sAnimation.dialogID))
                {
                    DialogObject dObj = dialogObjectsDictionary[sAnimation.dialogID];
                    if (dObj != null ? dObj.gameObject != null : false)
                        dObj.Animate(new AnimationEventArgs(sAnimation.animationName, sAnimation.id, sAnimation.argument));
                }
        }

        #endregion //hasAnimation

        SpeechSaid(_speech.speechName);

    }

    protected void Initialize()
    {
        canvas = GetComponent<Canvas>();

        InitializeDialogDictionary();
        Transform panel = transform.FindChild("SpeechPanel");
        speechPanel = panel.gameObject;
        speechText = panel.FindChild("SpeechText").GetComponent<Text>();
        portrait = panel.FindChild("PortraitImage").FindChild("Portrait").GetComponent<Image>();

        hero = SpecialFunctions.Player.transform;
        cam = SpecialFunctions.CamController;
        CurrentSpeech = null;
        dialogObjs = new List<DialogObject>();
        waitingForInput = false;

        answerPanel = transform.FindChild("AnswerPanel").gameObject;
        answerButton1 = answerPanel.transform.FindChild("Answer1").GetComponent<DialogAnswerButton>();
        answerButton2 = answerPanel.transform.FindChild("Answer2").GetComponent<DialogAnswerButton>();
        answerButton3 = answerPanel.transform.FindChild("Answer3").GetComponent<DialogAnswerButton>();
        answerText = answerPanel.transform.FindChild("AnswerText").GetComponent<Text>();
        answerPanel.SetActive(false);
        answerUIPanel = answerPanel.GetComponent<UIPanel>();
        speechPanel.SetActive(false);

        dialogQueue = new List<DialogQueueElement>();

    }

    #region events

    void OnDialogChange(DialogEventArgs e)
    {
        if (DialogEventHandler != null)
            DialogEventHandler(this, e);
    }

    /// <summary>
    /// Событие "Сказана реплика"
    /// </summary>
    /// <param name="speechName"></param>
    public void SpeechSaid(string speechName)
    {
        SpecialFunctions.StartStoryEvent(this, SpeechSaidEvent, new StoryEventArgs(speechName, 0));
    }

    #endregion //events

}

/// <summary>
/// Специальный класс, который составляет диалоговую очередь
/// </summary>
public class DialogQueueElement
{
    public Dialog dialog;//Диалог
    public NPCController npc;//НПС, который должен начать этот диалог

    public DialogQueueElement(Dialog _dialog, NPCController _npc)
    {
        dialog = _dialog;
        npc = _npc;
    }

}