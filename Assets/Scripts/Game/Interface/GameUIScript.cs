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

    #region fields

    [SerializeField]
    protected Sprite wholeHeart, halfHeart, emptyHeart;

    protected List<Image> heartImages=new List<Image>();

    protected Transform breathPanel;

    protected Text[] questTexts = new Text[3];//Строчки, рассказывающие об активных квестах
    
    protected Image weaponImage;

    #endregion //fields

    void Awake()
    {
        Initialize();
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
