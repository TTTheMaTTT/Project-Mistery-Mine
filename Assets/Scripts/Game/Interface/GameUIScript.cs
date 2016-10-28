using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, управляющий игровым интерфейсом
/// </summary>
public class GameUIScript : MonoBehaviour
{

    #region consts

    protected const float fadeSpeed = 1f;//Скорость затухания
    protected const float fadeTime = 2f;//Время, за которое происходит затухание или проявление экрана

    #endregion //consts

    #region fields

    [SerializeField]
    protected Sprite wholeHeart, halfHeart, emptyHeart;

    protected List<Image> heartImages=new List<Image>();

    protected Transform breathPanel;

    protected Text[] questTexts = new Text[3];//Строчки, рассказывающие об активных квестах
    
    protected Image weaponImage;

    protected GameObject textPanel;//В этом окошечке будет выводится информация о процессе игры
    protected Text infoText;

    protected Image fadeScreen;//Объект, ответственный за затемнение, происходящее в переходах между уровнями

    #endregion //fields

    #region parametres

    protected int fadeDirection;
    protected float fadeTextTime=0f;

    #endregion //parametres

    void Awake()
    {
        Initialize();
    }

    void FixedUpdate()
    {
        fadeScreen.color=Color.Lerp(fadeScreen.color,new Color(0f,0f,0f,fadeDirection==1? 0f: 
                                                                        fadeDirection==-1? 1f: 
                                                                        fadeScreen.color.a),Time.fixedDeltaTime*fadeSpeed);
    }

    void Initialize()
    {
        heartImages = new List<Image>();
        Transform panel = transform.FindChild("Panel");
        for (int i = 0; i < panel.childCount; i++)
        {
            heartImages.Add(panel.GetChild(i).GetComponent<Image>());
        }

        HeroController player = SpecialFunctions.player.GetComponent<HeroController>();
        player.healthChangedEvent += HandleHealthChanges;

        weaponImage = transform.FindChild("WeaponPanel").FindChild("WeaponImage").GetComponent<Image>();
        player.equipmentChangedEvent += HandleEquipmentChanges;

        Transform questsPanel = transform.FindChild("QuestsPanel");
        questTexts[0] = questsPanel.GetChild(0).GetComponent<Text>();
        questTexts[1] = questsPanel.GetChild(1).GetComponent<Text>();
        questTexts[2] = questsPanel.GetChild(2).GetComponent<Text>();

        fadeScreen = transform.FindChild("FadeScreen").GetComponent<Image>();

        textPanel = transform.FindChild("TextPanel").gameObject;
        infoText = textPanel.transform.FindChild("InfoText").GetComponent<Text>();
        infoText.text = "";
        textPanel.SetActive(false);

        breathPanel = transform.FindChild("BreathPanel");
        player.suffocateEvent += HandleSuffocate;
        ConsiderBreath(10);

        ConsiderHealth(player.Health);
    }

    /// <summary>
    /// Учитывая текущее количество здоровье, правильно отобразить сердечки
    /// </summary>
    void ConsiderHealth(float hp)
    {
        int i = 0;
        for (i=0; i < heartImages.Count; i++)
        {
            if (hp > (i + 0.5f)*4)
            {
                heartImages[i].sprite = wholeHeart;
            }
            else if (hp > i*4)
            {
                heartImages[i].sprite = halfHeart;
            }
            else
            {
                heartImages[i].sprite = emptyHeart;
                break;
            }
        }
        for (int j = i + 1; j < heartImages.Count; j++)
        {
            heartImages[j].sprite = emptyHeart;
        }
    }

    /// <summary>
    /// Учитывая текущий запас воздуха, отобразить его на экране
    /// </summary>
    void ConsiderBreath(int airSupply)
    {
        if (airSupply == 10)
        {
            breathPanel.gameObject.SetActive(false);
        }
        else
        {
            breathPanel.gameObject.SetActive(true);
            for (int i = 0; i < 10; i++)
            {
                if (i < airSupply)
                {
                    breathPanel.GetChild(i).gameObject.SetActive(true);
                }
                else
                {
                    breathPanel.GetChild(i).gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Учесть, какие квесты на данный момент активны
    /// </summary>
    public void ConsiderQuests(List<string> activeQuests)
    {
        for (int i = 0; i < 3; i++)
        {
            questTexts[i].text = "";
            if (i >= activeQuests.Count)
                continue;
            else
                questTexts[i].text = activeQuests[i];
        }
    }

    /// <summary>
    /// выставить текст на экран
    /// </summary>
    public void SetText(string _info, float textTime)
    {
        infoText.text = _info;
        StopCoroutine(TextProcess(fadeTextTime));
        fadeTextTime = textTime;
        StartCoroutine(TextProcess(fadeTextTime));
    }

    /// <summary>
    /// Процесс появления и исчезания текста
    /// </summary>
    IEnumerator TextProcess(float textTime)
    {
        textPanel.SetActive(true);
        yield return new WaitForSeconds(textTime);
        textPanel.SetActive(false);
    }

    /// <summary>
    /// Начать затухание экрана
    /// </summary>
    public void FadeIn()
    {
        fadeDirection = -1;
        StartCoroutine(FadeProcess());
    }

    /// <summary>
    /// Начать проявление экрана
    /// </summary>
    public void FadeOut()
    {
        fadeDirection = 1;
        StartCoroutine(FadeProcess());
    }

    /// <summary>
    /// Мгновенно сделать игровой экран тёмным
    /// </summary>
    public void SetDark()
    {
        fadeScreen.color = Color.black;
    }

    /// <summary>
    /// Процесс затухания или проявления экрана
    /// </summary>
    IEnumerator FadeProcess()
    {
        yield return new WaitForSeconds(fadeTime);
        fadeScreen.color = new Color(0f, 0f, 0f, fadeDirection == -1 ? 1f : 0f);
        fadeDirection = 0;
    }

    #region eventHandlers

    /// <summary>
    /// Обработать событие "Здоровье изменилось"
    /// </summary>
    protected virtual void HandleHealthChanges(object sender, HealthEventArgs e)
    {
        ConsiderHealth(e.HP);
    }

    /// <summary>
    /// Обработать событие "Инвентарь изменился"
    /// </summary>
    protected virtual void HandleEquipmentChanges(object sender, EquipmentEventArgs e)
    {
        if (e.Item.itemImage!=null)
            weaponImage.sprite = e.Item.itemImage;
    }

    /// <summary>
    /// Обработать событие "Запас воздуха изменился"
    /// </summary>
    protected virtual void HandleSuffocate(object sender, SuffocateEventArgs e)
    {
        ConsiderBreath(e.AirSupply);
    }

    #endregion //eventHandlers

}
