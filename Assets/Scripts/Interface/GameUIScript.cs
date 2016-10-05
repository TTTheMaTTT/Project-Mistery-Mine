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

        HeroController player = SpecialFunctions.GetPlayer().GetComponent<HeroController>();
        player.healthChangedEvent += HandleHealthChanges;

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
            if (hp > i + 0.5f)
            {
                heartImages[i].sprite = wholeHeart;
            }
            else if (hp > i)
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

    #region eventHandlers

    /// <summary>
    /// Обработать событие "Здоровье изменилось"
    /// </summary>
    protected virtual void HandleHealthChanges(object sender, HealthEventArgs e)
    {
        ConsiderHealth(e.HP);
    }

    #endregion //eventHandlers

}
