using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, управляющий диалоговым окном
/// </summary>
public class DialogWindowScript : MonoBehaviour
{

    #region consts

    protected const int maxNoInput = 30;//сколько кадров нельзя будет ввести пропуск диалога

    #endregion //consts

    #region fields

    protected Canvas canvas;

    protected Text speechText;
    protected Image portrait;

    protected Transform hero;
    protected NPCController npc;
    protected CameraController cam;

    protected Dialog currentDialog = null;
    protected List<Dialog> reserveDialogs = new List<Dialog>();//Список диалогов, которые надо проиграть по порядку 
                                                               //(пополняется, когда диалоговому окну запрашивается открыть новый диалог) 
                                                               //и очищается, когда появляется возможность начать новый диалог
    protected List<NPCController> reserveNPCs = new List<NPCController>();//Тот же самый список, но для НПС, с которыми педстоит пообщаться

    protected Speech currentSpeech = null;
    public Speech CurrentSpeech { get { return currentSpeech; }
                                  set { currentSpeech = value; if (value != null) { speechText.text = value.text; portrait.sprite = value.portrait;
                                                                                    npc.SpeechSaid(currentSpeech.speechName);
                                                                                    cam.ChangeCameraMod(value.moveCam?CameraModEnum.move: CameraModEnum.playerMove);
                                                                                    cam.ChangeCameraTarget(value.moveCam ? value.camPosition : hero.position); }

                                                               else { speechText.text = null;  portrait.sprite = null; cam.ChangeCameraMod(CameraModEnum.player);
                                                                                                                        cam.ChangeCameraTarget(hero.position);} } }

    #endregion //fields

    #region parametres

    protected float prevScale1, prevScale2;
    protected int noInput = -1;//Если true, то диалог пропустить нельзя

    #endregion //parametres

    protected void Awake()
    {
        Initialize();
    }

    void Update()
    {
        if (canvas.enabled)
        {
            Event e = Event.current;
            if (Input.anyKeyDown && !Input.GetButtonDown("Horizontal") && !Input.GetButtonDown("Vertical") && !Input.GetButtonDown("Cancel") && noInput==-1)
                NextSpeech();
        }
        else if (reserveDialogs.Count>0)
        {
            NPCController newNPC = reserveNPCs[0];
            newNPC.StartTalking();
            Dialog newDialog = reserveDialogs[0];
            reserveDialogs.RemoveAt(0);
            reserveNPCs.RemoveAt(0);
            BeginDialog(newNPC, newDialog);
        }
        if (noInput > -1)
            noInput++;
        if (noInput == maxNoInput)
            noInput = -1;
    }

    /// <summary>
    /// Начать диалог
    /// </summary>
    public void BeginDialog(NPCController _npc, Dialog dialog)
    {
        if (!canvas.enabled)
        {
            npc = _npc;
            currentDialog = dialog;
            currentDialog.stage = 0;

            CurrentSpeech = dialog.speeches[0];

            canvas.enabled = true;

            HeroController hControl = hero.GetComponent<HeroController>();
            hControl.SetImmobile(true);

            //Повернуть персонажей друг к другу
            prevScale1 = hero.localScale.x;
            prevScale2 = npc.transform.localScale.x;
            if (hero.localScale.x * (npc.transform.position - hero.position).x < 0f)
            {
                hero.localScale += new Vector3(-2f * prevScale1, 0f);
            }
            if (npc.transform.localScale.x * (npc.transform.position - hero.position).x < 0f)
            {
                npc.transform.localScale += new Vector3(-2f * prevScale2, 0f);
            }

            if (currentDialog.pause)
            {
                SpecialFunctions.PauseGame();
            }
            noInput = 0;
        }
        else//Занести диалог и НПС в резерв, чтобы пообщатся с ним потом
        {
            if (_npc != npc)
                _npc.StopTalking();
            reserveDialogs.Add(dialog);
            reserveNPCs.Add(_npc);
        }
    }

    /// <summary>
    /// Функция завершения разговора
    /// </summary>
    protected void StopDialog()
    {
        canvas.enabled = false;

        HeroController hControl = hero.GetComponent<HeroController>();
        hControl.SetImmobile(false);

        //Повернуть персонажей друг к другу
        Vector3 vect1 = hero.localScale;
        Vector3 vect2 = npc.transform.localScale;
        hero.localScale = new Vector3(prevScale1, vect1.y, vect1.z);
        npc.transform.localScale = new Vector3(prevScale2, vect2.y, vect2.z);

        NPCController npcControl;
        if ((npcControl = npc.GetComponent<NPCController>()) != null)
        {
            npcControl.StopTalking();
        }

        SpecialFunctions.PlayGame();

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

    protected void Initialize()
    {
        canvas = GetComponent<Canvas>();

        reserveDialogs = new List<Dialog>();
        reserveNPCs = new List<NPCController>();

        Transform panel = transform.FindChild("Panel");
        speechText = panel.FindChild("SpeechText").GetComponent<Text>();
        portrait = transform.FindChild("PortraitImage").FindChild("Portrait").GetComponent<Image>();

        hero = SpecialFunctions.player.transform;
        cam = SpecialFunctions.camControl;
        CurrentSpeech = null;
    }

}

