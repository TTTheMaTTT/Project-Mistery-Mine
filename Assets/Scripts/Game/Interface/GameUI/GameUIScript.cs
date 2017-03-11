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
    protected const float fadeOffset = 3f;//Время до конца, при котором начин/ается мигание иконок

    protected const float buffIconOffset = 10f;//Насколько смещаются изображения коллекции относительно левого края панели баффов
    protected const float buffIconWidth = 20f;//Длина стороны изображения коллекции

    protected const float bossHPMaxWidth = 430f;//Максимальная длина полоски хп босса

    protected const float collectionItemTime = 2f;//Как долго висят экраны с коллекционными предметами?
    protected const float collectionImageWidth = 100f;//Какова длина изображения предмета коллекции?

    #endregion //consts

    #region fields

    [SerializeField]
    protected Sprite wholeHeart, halfHeart, emptyHeart;

    [SerializeField]
    protected List<BuffImage> buffImages = new List<BuffImage>();//Список изображений эффектов
    protected Transform buffPanel;//Панелька на которой размещаются иконки баффов

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
    protected Image damageScreen;//Объект, ответственный за покраснения экрана при получении урона

    protected GameObject bossHealthPanel;
    protected Image bossHP;
    protected Text bossNameText;

    #endregion //fields

    #region parametres

    protected int fadeDirection;
    protected float fadeTextTime = 0f, fadeSecretTextTime = 0f;

    protected DamageScreenType dmgScreenType = DamageScreenType.Nothing;
    protected Color dmgColor = new Color (0f,0f,0f,0f);
    protected  float dmgScreenFadeSpeed = 2f;//Скорость мигания экрана урона

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
        damageScreen.color = Color.Lerp(damageScreen.color, dmgColor, Time.fixedDeltaTime * dmgScreenFadeSpeed);
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

        buffPanel = transform.FindChild("BuffsPanel");
        player.buffAddEvent += HandleBuffAdd;
        player.buffRemoveEvent += HandleBuffRemove;

        Transform questsPanel = transform.FindChild("QuestsPanel");
        questTexts[0] = questsPanel.GetChild(0).GetComponent<Text>();
        questTexts[1] = questsPanel.GetChild(1).GetComponent<Text>();
        questTexts[2] = questsPanel.GetChild(2).GetComponent<Text>();

        fadeScreen = transform.FindChild("FadeScreen").GetComponent<Image>();
        damageScreen = transform.FindChild("DamageScreen").GetComponent<Image>();

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
    /// Возвращает изображение баффа, используя информации об этом баффе
    /// </summary>
    /// <param name="buff">Информация о баффе</param>
    Sprite GetBuffImage(BuffClass buff)
    {
        Sprite bIcon = null;
        foreach (BuffImage bImage in buffImages)
        {
            if (buff.buffName == bImage.buffName)
            {
                bIcon = bImage.buffImage;
                break;
            }
        }
        return bIcon;
    }

    /// <summary>
    /// Установить иконки на правильные позиции
    /// </summary>
    void ConsiderBuffs()
    {
        for (int i = 0; i < buffPanel.childCount; i++)
        {
            RectTransform rTrans = buffPanel.GetChild(i).GetComponent<RectTransform>();
            rTrans.anchoredPosition = Vector2.right * (buffIconOffset+i*buffIconWidth);
        }
    }

    /// <summary>
    /// Процесс, который отвечает за мерцание иконок баффов к концу их времени действия
    /// </summary>
    /// <param name="_time">Время действия баффа</param>
    /// <param name="obj">Иконка, соответствующая баффу</param>
    IEnumerator BuffFadeProcess(float _time, GameObject obj)
    {
        yield return new WaitForSeconds(_time-3f);
        if (obj != null)
            obj.AddComponent<FadeFluctuationScript>();
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
                newObject.layer = LayerMask.NameToLayer("UI");
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
        StopCoroutine("TextMessage");
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
    public void SetSecretMessage(float textTime, string _text="Вы нашли секретное место!")
    {
        if (secretPlaceText == null)
            return;
        secretPlaceText.text = _text;
        StopCoroutine("SecretTextMessage");
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
    /// Процесс управления отображением урона при его получении
    /// </summary>
    IEnumerator GetDamageFadeProcess()
    {
        if ((dmgScreenType & DamageScreenType.LowHP) != DamageScreenType.LowHP && (dmgScreenType & DamageScreenType.Poison) != DamageScreenType.Poison)
        {
            dmgColor = new Color(1f, 0f, 0f, 0.7f);
            dmgScreenFadeSpeed = 100f;

            yield return new WaitForSeconds(.5f);
            ConsiderDamageScreenState();
        }
    }

    /// <summary>
    /// Процесс управления отображением урона при низком хп
    /// </summary>
    IEnumerator LowHPFadeProcess()
    {
        while (true)
        {
            dmgColor = new Color(1f, 0f, 0f, 0.45f);
            yield return new WaitForSeconds(1f);
            dmgColor = new Color(1f, 0f, 0f, 0.7f);
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Процесс управления отображением урона при  отравлении
    /// </summary>
    IEnumerator PoisonedFadeProcess()
    {
        while (true)
        {
            dmgColor = new Color(0f, 1f, .17f, 0.45f);
            yield return new WaitForSeconds(1f);
            dmgColor = new Color(0f, 1f, .17f, 0.7f);
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Вызвать посинение экрана при заморозке
    /// </summary>
    void ConsiderFrozen()
    {
        dmgColor = new Color(0f, 1f, .96f, .7f);
    }

    /// <summary>
    /// Определиться, какое состояниедолжно быть у экрана отображения урона
    /// </summary>
    void ConsiderDamageScreenState()
    {
        dmgScreenFadeSpeed = 2f;
        StopCoroutine("LowHPFadeProcess");
        StopCoroutine("PoisonedFadeProcess");
        if ((dmgScreenType & DamageScreenType.LowHP) == DamageScreenType.LowHP)
            StartCoroutine("LowHPFadeProcess");
        else if ((dmgScreenType & DamageScreenType.Poison) == DamageScreenType.Poison)
            StartCoroutine("PoisonedFadeProcess");
        else if ((dmgScreenType & DamageScreenType.Frozen) == DamageScreenType.Frozen)
            ConsiderFrozen();
        else
            dmgColor = new Color(0f, 0f, 0f, 0f);
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
        if (e.HP < 5f)
            dmgScreenType |= DamageScreenType.LowHP; 
        else
            dmgScreenType = (dmgScreenType & DamageScreenType.Frozen) | (dmgScreenType & DamageScreenType.Poison);
        ConsiderDamageScreenState();
        if (e.HPDelta < 0f)
        {
            StartCoroutine("GetDamageFadeProcess");
            SpecialFunctions.camControl.ShakeCamera();
        }
    }

    /// <summary>
    /// Обработать событие "Инвентарь изменился"
    /// </summary>
    protected virtual void HandleEquipmentChanges(object sender, EquipmentEventArgs e)
    {
        if (e.CurrentWeapon!=null)
            weaponImage.sprite = e.CurrentWeapon.itemImage;
    }

    /// <summary>
    /// Обработать событие "Запас воздуха изменился"
    /// </summary>
    protected virtual void HandleSuffocate(object sender, SuffocateEventArgs e)
    {
        ConsiderBreath(e.AirSupply);
    }

    /// <summary>
    /// Обработать событие "На персонажа действует новый бафф"
    /// </summary>
    protected virtual void HandleBuffAdd(object sender, BuffEventArgs e)
    {
        BuffClass buff = e.Buff;
        Sprite bImage = GetBuffImage(buff);
        if (bImage == null)
            return;
        GameObject _icon = new GameObject(buff.buffName + "Icon");
        _icon.transform.parent = buffPanel;
        _icon.transform.localPosition = Vector3.zero;
        _icon.layer = LayerMask.NameToLayer("UI");
        RectTransform iconRect = _icon.AddComponent<RectTransform>();
        iconRect.localScale = Vector3.one;
        iconRect.anchorMax = .5f * Vector2.up;
        iconRect.anchorMin = .5f * Vector2.up;
        iconRect.anchoredPosition = Vector2.right * (buffIconOffset + buffIconWidth * (buffPanel.childCount - 1));
        iconRect.sizeDelta = new Vector2(buffIconWidth, buffIconWidth);
        Image _img = _icon.AddComponent<Image>();
        _img.sprite = bImage;
        float bTime = buff.duration - Time.fixedTime + buff.beginTime;
        if (bTime > 3f)
            StartCoroutine(BuffFadeProcess(bTime, _icon));
        if (!e.BattleEffect)
            SetSecretMessage(2f, SpecialFunctions.gameController.GetBuffText(buff.buffName));
        if (buff.buffName == "FrozenProcess")
            dmgScreenType |= DamageScreenType.Frozen;
        else if (buff.buffName == "PoisonProcess")
            dmgScreenType |= DamageScreenType.Poison;
        ConsiderDamageScreenState();

    }

    /// <summary>
    /// Обработать событие "На персонажа больше не действует бафф"
    /// </summary>
    protected virtual void HandleBuffRemove(object sender, BuffEventArgs e)
    {
        BuffClass buff = e.Buff;
        GameObject bIcon = null;
        for (int i = 0; i < buffPanel.childCount; i++)
        {
            if (buffPanel.GetChild(i).gameObject.name.Contains(buff.buffName))
            {
                bIcon = buffPanel.GetChild(i).gameObject;
                break;
            }
        }
        if (bIcon != null)
        {
            bIcon.transform.parent = null;
            Destroy(bIcon);
        }
        ConsiderBuffs();
        if (buff.buffName == "FrozenProcess")
            dmgScreenType = (dmgScreenType & DamageScreenType.LowHP)|(dmgScreenType & DamageScreenType.Poison);
        else if (buff.buffName == "PoisonProcess")
            dmgScreenType = (dmgScreenType & DamageScreenType.LowHP) | (dmgScreenType & DamageScreenType.Frozen);
        ConsiderDamageScreenState();
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

/// <summary>
/// Специальная структура, содержащая название баффа и его изображение
/// </summary>
[System.Serializable]
public struct BuffImage
{
    public string buffName;//Название баффа
    public Sprite buffImage;//Изображение баффа

    public BuffImage(string _bName, Sprite _bImage)
    {
        buffName = _bName;
        buffImage = _bImage;
    }

}

/// <summary>
/// Типы урона, которые могут повлиять на цвет экрана повреждений
/// </summary>
[Flags]
public enum DamageScreenType : byte
{
    Nothing = 0x01,
    LowHP = 0x02,
    Frozen = 0x04,
    Poison = 0x08
}