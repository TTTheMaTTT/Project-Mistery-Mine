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

    protected Transform hero, npc;
    protected Speech currentSpeech = null;

    #endregion //fields

    #region parametres

    protected float prevScale1, prevScale2;

    #endregion //parametres

    protected void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// Начать диалог
    /// </summary>
    public void BeginDialog(Transform _hero, Transform _npc, Speech speech)
    {
        currentSpeech = speech;
        canvas.enabled = true;
        speechText.text = speech.text;
        hero = _hero;
        npc = _npc;

        HeroController hControl = hero.GetComponent<HeroController>();
        hControl.SetImmobile(true);

        //Повернуть персонажей друг к другу
        prevScale1 = hero.localScale.x;
        prevScale2 = npc.localScale.x;
        if (hero.localScale.x * (npc.position - hero.position).x < 0f)
        {
            hero.localScale += new Vector3(-2f * prevScale1, 0f);
        }
        if (npc.localScale.x * (npc.position- hero.position).x < 0f)
        {
            npc.localScale += new Vector3(-2f * prevScale2, 0f);
        }
    }

    /// <summary>
    /// Функция завершения разговора
    /// </summary>
    protected void StopDialog()
    {
        currentSpeech = null;
        canvas.enabled = false;
        speechText.text = "";

        HeroController hControl = hero.GetComponent<HeroController>();
        hControl.SetImmobile(false);

        //Повернуть персонажей друг к другу
        Vector3 vect1 = hero.localScale;
        Vector3 vect2 = npc.localScale;
        hero.localScale = new Vector3(prevScale1, vect1.y, vect1.z);
        npc.localScale = new Vector3(prevScale2, vect2.y, vect2.z);

        NPCController npcControl;
        if ((npcControl = npc.GetComponent<NPCController>()) != null)
        {
            npcControl.StopTalking();
        }

    }

    /// <summary>
    /// Перейти к следующему этапу диалога
    /// </summary>
    public void NextSpeech()
    {
        if (currentSpeech.nextSpeech != null)
        {
            currentSpeech = currentSpeech.nextSpeech;
            speechText.text = currentSpeech.text;
        }
        else
        {
            StopDialog();
        }
    }

    protected void Initialize()
    {
        currentSpeech = null;
        canvas = GetComponent<Canvas>();

        Transform panel = transform.FindChild("Panel");
        speechText = panel.FindChild("SpeechText").GetComponent<Text>();
        speechText.text = "";
    }

}

