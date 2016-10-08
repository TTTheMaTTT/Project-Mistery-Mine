using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Скрипт, управляющий диалоговым окном
/// </summary>
public class DialogWindowScript : MonoBehaviour
{

    #region fields

    protected Canvas canvas;

    protected Text speechText;
    protected Image portrait;

    protected Transform hero;
    protected NPCController npc;

    protected Speech currentSpeech = null;
    public Speech CurrentSpeech { get { return currentSpeech; }
                                  set { currentSpeech = value; if (value != null) { speechText.text = value.text; portrait.sprite = value.portrait; npc.SpeechSaid(currentSpeech.speechName); }
                                                               else { speechText.text = null;  portrait.sprite = null; } } }

    #endregion //fields

    #region parametres

    protected float prevScale1, prevScale2;

    #endregion //parametres

    protected void Awake()
    {
        Initialize();
    }

    void Update()
    {
        if (canvas.enabled)
        {
            if (Input.GetButtonDown("Attack"))
                NextSpeech();
        }
    }

    /// <summary>
    /// Начать диалог
    /// </summary>
    public void BeginDialog(Transform _hero, NPCController _npc, Speech speech)
    {
        npc = _npc;
        CurrentSpeech = speech;
        canvas.enabled = true;
        hero = _hero;

        HeroController hControl = hero.GetComponent<HeroController>();
        hControl.SetImmobile(true);

        //Повернуть персонажей друг к другу
        prevScale1 = hero.localScale.x;
        prevScale2 = npc.transform.localScale.x;
        if (hero.localScale.x * (npc.transform.position - hero.position).x < 0f)
        {
            hero.localScale += new Vector3(-2f * prevScale1, 0f);
        }
        if (npc.transform.localScale.x * (npc.transform.position- hero.position).x < 0f)
        {
            npc.transform.localScale += new Vector3(-2f * prevScale2, 0f);
        }

        if (currentSpeech.pause)
        {
            SpecialFunctions.PauseGame();
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
        if (currentSpeech.nextSpeech==null)
            CurrentSpeech = currentSpeech.nextSpeech;
    }

    protected void Initialize()
    {
        canvas = GetComponent<Canvas>();

        Transform panel = transform.FindChild("Panel");
        speechText = panel.FindChild("SpeechText").GetComponent<Text>();
        portrait = transform.FindChild("Portrait").GetComponent<Image>();
        CurrentSpeech = null;
    }

}

