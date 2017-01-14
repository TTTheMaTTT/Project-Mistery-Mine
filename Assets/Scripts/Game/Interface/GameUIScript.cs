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

    protected const float bossHPMaxWidth = 430f;//Максимальная длина полоски хп босса

    protected const float collectionItemTime = 2f;//Как долго висят экраны с коллекционными предметами?
    protected const float collectionImageWidth = 100f;//Какова длина изображения предмета коллекции

    #endregion //consts

    #region fields

    [SerializeField]
    protected Sprite wholeHeart, halfHeart, emptyHeart;

    protected List<Image> heartImages=new List<Image>();

    protected Transform breathPanel;

    protected Text[] questTexts = new Text[3];//Строчки, рассказывающие об активных квестах
    
    protected Image weaponImage;

    protected GameObject textPanel;//В этом окошечке будет выводится информация о процессе игры
    protected GameObject messagePanel;//В этом окошечке выводится информация, переданная от других персонажей или игровых объектов
    protected Text messageText;
    protected GameObject secretPlacePanel;//В этом окошечке выводится информация, о том, что герой нашёл секретное место
    protected Text secretPlaceText;

    protected GameObject collectorScreen;//Панель, на которой отображается информация о собранных коллекциях
    protected GameObject oneItemScreen;//Экран, в котором показывается найденный коллекционный предмет
    protected GameObject collectionScreen;//Экран, в котором показывается, к каким коллекциям этот предмет принадлежит

    protected Image fadeScreen;//Объект, ответственный за затемнение, происходящее в переходах между уровнями

    protected GameObject bossHealthPanel;
    protected Image bossHP;
    protected Text bossNameText;

    #endregion //fields

    #region parametres

    protected int fadeDirection;
    protected float fadeTextTime = 0f, fadeSecretTextTime = 0f;

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

        HeroController player = SpecialFunctions.Player.GetComponent<HeroController>();
        player.healthChangedEvent += HandleHealthChanges;

        weaponImage = transform.FindChild("WeaponPanel").FindChild("WeaponImage").GetComponent<Image>();
        player.equipmentChangedEvent += HandleEquipmentChanges;
        weaponImage.sprite = player.CurrentWeapon.itemImage;

        Transform questsPanel = transform.FindChild("QuestsPanel");
        questTexts[0] = questsPanel.GetChild(0).GetComponent<Text>();
        questTexts[1] = questsPanel.GetChild(1).GetComponent<Text>();
        questTexts[2] = questsPanel.GetChild(2).GetComponent<Text>();

        fadeScreen = transform.FindChild("FadeScreen").GetComponent<Image>();

        textPanel = transform.FindChild("TextPanel").gameObject;
        messagePanel = textPanel.transform.FindChild("MessagePanel").gameObject;
        messageText = messagePanel.transform.FindChild("MessageText").GetComponent<Text>();
        messageText.text = "";
        messagePanel.SetActive(false);
        secretPlacePanel = textPanel.transform.FindChild("SecretPlacePanel").gameObject;
        secretPlaceText = secretPlacePanel.transform.FindChild("SecretPlaceText").GetComponent<Text>();
        secretPlaceText.text = "";

        collectorScreen = transform.FindChild("CollectionsPanel").gameObject;
        oneItemScreen = collectorScreen.transform.FindChild("OneItemScreen").gameObject;
        collectionScreen = collectorScreen.transform.FindChild("CollectionItemsScreen").gameObject;
        oneItemScreen.SetActive(false);
        collectionScreen.SetActive(false);
        collectorScreen.SetActive(false);

        breathPanel = transform.FindChild("BreathPanel");
        player.suffocateEvent += HandleSuffocate;
        ConsiderBreath(10);

        ConsiderHealth(player.Health);

        bossHealthPanel = transform.FindChild("BossHealthPanel").gameObject;
        bossHP = bossHealthPanel.transform.FindChild("BossHP").GetComponent<Image>();
        bossNameText = bossHealthPanel.GetComponentInChildren<Text>();
        bossHealthPanel.SetActive(false);

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
    /// Функция, что учитывает информацию о собираемых поверхностях
    /// </summary>
    public void ConsiderCollections(ItemClass _item, List<ItemCollection> _collections)
    {
        StartCoroutine(CollectionProcess(_item, _collections));
    }

    /// <summary>
    /// Процесс отображения новой информации о коллекциях
    /// </summary>
    public IEnumerator CollectionProcess(ItemClass _item, List<ItemCollection> _collections)
    {
        SpecialFunctions.PauseGame();
        collectorScreen.SetActive(true);
        oneItemScreen.SetActive(false);
        collectionScreen.SetActive(false);
        yield return new WaitForSecondsRealtime(collectionItemTime / 2f);

        Image _img = oneItemScreen.transform.FindChild("CollectionItemImage").GetComponent<Image>();
        _img.sprite = _item.itemImage;
        Text _text = oneItemScreen.transform.FindChild("ItemNameText").GetComponent<Text>();
        _text.text = _item.itemName;
        oneItemScreen.SetActive(true);
        yield return new WaitForSecondsRealtime(collectionItemTime);

        oneItemScreen.SetActive(false);
        collectionScreen.SetActive(true);
        foreach (ItemCollection _collection in _collections)
        {
            for (int i = 0; i < collectionScreen.transform.childCount; i++)//Сначала удалим все объекты на экране, что являются изображениями
            {
                GameObject child = collectionScreen.transform.GetChild(i).gameObject;
                if (child.GetComponent<Image>() != null)
                {
                    DestroyImmediate(child);
                }
            }
            //Теперь расставим объекты на нужные позиции и зададим им параметры
            _text = collectionScreen.transform.FindChild("ItemsCountText").GetComponent<Text>();
            float xPosition = -collectionImageWidth / 2f * _collection.collection.Count;
            int secretsFoundCount = 0;
            _text.GetComponent<RectTransform>().localPosition = new Vector3(xPosition,0f,0f);
            for (int i = 0; i < _collection.collection.Count; i++)
            {
                xPosition += collectionImageWidth;
                GameObject newObject = new GameObject("ItemImage" + i.ToString());
                newObject.transform.SetParent(collectionScreen.transform);
                RectTransform rTrans = newObject.AddComponent<RectTransform>();
                rTrans.localPosition = new Vector3(xPosition, 0f, 0f);
                rTrans.localScale = new Vector3(1f, 1f, 1f);
                _img=newObject.AddComponent<Image>();
                if (_collection.collection[i].itemFound)
                {
                    secretsFoundCount++;
                    _img.sprite = _collection.collection[i].item.itemImage;
                }
            }
            _text.text = secretsFoundCount.ToString() + "/" + _collection.collection.Count.ToString();
            yield return new WaitForSecondsRealtime(collectionItemTime);
        }
        oneItemScreen.SetActive(false);
        collectionScreen.SetActive(false);
        collectorScreen.SetActive(false);

        SpecialFunctions.PlayGame();
    }

    /// <summary>
    /// выставить текст на экран сообщений
    /// </summary>
    public void SetMessage(string _info, float textTime)
    {
        messageText.text = _info;
        StopCoroutine(TextMessage(fadeTextTime));
        fadeTextTime = textTime;
        StartCoroutine(TextMessage(fadeTextTime));
    }

    /// <summary>
    /// Процесс появления и исчезания текста на экране сообщений
    /// </summary>
    IEnumerator TextMessage(float textTime)
    {
        messagePanel.SetActive(true);
        yield return new WaitForSeconds(textTime);
        messagePanel.SetActive(false);
    }

    /// <summary>
    /// выставить текст на экране сообщений о секретах
    /// </summary>
    public void SetSecretMessage(float textTime)
    {
        secretPlaceText.text = "Вы нашли секретное место!";
        StopCoroutine(SecretTextMessage(fadeSecretTextTime));
        fadeSecretTextTime = textTime;
        StartCoroutine(SecretTextMessage(fadeSecretTextTime));
    }

    /// <summary>
    /// Процесс появления и исчезания текста на экране сообщений
    /// </summary>
    IEnumerator SecretTextMessage(float textTime)
    {
        secretPlacePanel.SetActive(true);
        yield return new WaitForSeconds(textTime);
        secretPlacePanel.SetActive(false);
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

    /// <summary>
    /// Выключить панель с хп босса
    /// </summary>
    public void SetInactiveBossPanel()
    {
        StartCoroutine(BossPanelInactiveProcess());
    }

    protected IEnumerator BossPanelInactiveProcess()
    {
        yield return new WaitForSeconds(1f);
        bossHealthPanel.SetActive(false);
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

    /// <summary>
    /// Обработать событие "Здоровье босса изменилось"
    /// </summary>
    public virtual void HandleBossHealthChanges(object sender, BossHealthEventArgs e)
    {
        bossHealthPanel.SetActive(true);
        Vector2 size = bossHP.GetComponent<RectTransform>().sizeDelta;
        bossHP.GetComponent<RectTransform>().sizeDelta = new Vector2(bossHPMaxWidth * e.HP / e.MaxHP,size.y);
        bossNameText.text = e.BossName;
    }

    #endregion //eventHandlers

}
