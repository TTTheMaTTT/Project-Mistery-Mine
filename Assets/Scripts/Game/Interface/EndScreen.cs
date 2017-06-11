using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Скрипт управляющий экраном, показываемым в конце игры
/// </summary>
public class EndScreen : MonoBehaviour, ILanguageChangeable
{

    #region consts

    private const string mainMenuName = "MainMenu", firstLevelName = "cave_lvl1";
    private const float endScreenTime = .1f;//Сколько времени висит этот экран перед тем, как перейти в главное меню
    private const float endStatisticsFadeInTime = 3f, endStatisticsTime=5f, endStatisticsFadeOutTime=3f;//Времена, в течение которых висит статистика игры

    private const float defaultFadeSpeed = 3f;

    #endregion //consts

    #region fields

    [SerializeField]private List<FadeImagesInfoClass> fadeInfos=new List<FadeImagesInfoClass>();//В этом поле занесена информация, о том, что будет показываться в титрах

    private List<SpriteRenderer> fadeInSprites=new List<SpriteRenderer>(), fadeOutSprites=new List<SpriteRenderer>();
    private List<Text> fadeInTexts=new List<Text>(), fadeOutTexts=new List<Text>();

    private Text gratitudeText, deathCountText, gameTimeText;

    #endregion //fields

    #region parametres

    string datapath;

    private int currentPhase = 0;
    private FadeImagesInfoClass currentFadeInfo;
    private float fadeSpeed = defaultFadeSpeed, fadeInAlpha=0f, fadeOutAlpha=1f;

    private string savesInfoPath;

    [SerializeField]
    private List<MultiLanguageTextInfo> languageChanges = new List<MultiLanguageTextInfo>();//Переводы текстов

    private MultiLanguageText gameTimeMLText = new MultiLanguageText("Времени затрачено: ", "Time spent: ", "Витрачено часу: ", "Spędzony czas: ", "");
    private MultiLanguageText deathCountMLText = new MultiLanguageText("Количество смертей: ", "Number of deaths: ", "Кількість смертей: ", "Ilość śmierci: ", "");

    private float totalGameTime;
    private int deathCount;
    private bool showStatistics = false;

    private bool noInput = false;

    #endregion //parametres

    void Start ()
    {

        SpecialFunctions.PlayGame();

        noInput = false;
        showStatistics = false;

        gratitudeText = transform.FindChild("GratitudeText").GetComponent<Text>();
        gameTimeText = transform.FindChild("GameTimeText").GetComponent<Text>();
        deathCountText = transform.FindChild("DeathCountText").GetComponent<Text>();

        datapath= (Application.streamingAssetsPath) + "/Profile";
        int profileNumber = PlayerPrefs.GetInt("Profile Number");

        
        GameData gData = Serializator.DeXml(datapath + profileNumber.ToString() + ".xml");
        if (gData != null)
        {
            totalGameTime = gData.gGData.gameTime + gData.gGData.gameAddTime;
            deathCount = gData.gGData.deathCount;
            Serializator.SaveXml(null, datapath + profileNumber.ToString() + ".xml");
        }

        deathCountText.text = deathCountMLText.GetText(SettingsScript.language) + deathCount.ToString();
        int hours = Mathf.FloorToInt(totalGameTime / 3600f);
        int minutes = Mathf.FloorToInt((totalGameTime - hours * 3600f) / 60f);
        int seconds = Mathf.FloorToInt((totalGameTime - hours * 3600f - minutes * 60f));
        TimeSpan tSpan = new TimeSpan(hours, minutes, seconds);
        gameTimeText.text = gameTimeMLText.GetText(SettingsScript.language) + tSpan.ToString();

        fadeInSprites = new List<SpriteRenderer>();
        fadeOutSprites = new List<SpriteRenderer>();
        fadeInTexts = new List<Text>();
        fadeOutTexts = new List<Text>();
        currentFadeInfo = null;
        if (fadeInfos.Count > 0)
            ManageFadeInfo(fadeInfos[0]);
        else
            StartCoroutine("FinalStatisticsProcess");
    }

    void Update()
    {
        fadeInAlpha = Mathf.Lerp(fadeInAlpha, 1f, Time.deltaTime * fadeSpeed);
        fadeOutAlpha = Mathf.Lerp(fadeOutAlpha, 0f, Time.deltaTime * fadeSpeed);

        for (int i = 0; i < fadeInSprites.Count; i++)
            fadeInSprites[i].color = new Color(1f, 1f, 1f, fadeInAlpha);
        for (int i = 0; i < fadeOutSprites.Count; i++)
            fadeOutSprites[i].color = new Color(1f, 1f, 1f, fadeOutAlpha);

        for (int i = 0; i < fadeInTexts.Count; i++)
            fadeInTexts[i].color = new Color(1f, 1f, 1f, fadeInAlpha);
        for (int i = 0; i < fadeOutTexts.Count; i++)
            fadeOutTexts[i].color = new Color(1f, 1f, 1f, fadeOutAlpha);

        if (!noInput)
            if (InputCollection.instance.GetButtonDown("Jump"))
                Interupt();

    }

    /// <summary>
    /// Правильным образом обработать все объекты, показываемые в данной фазе титров
    /// </summary>
    void ManageFadeInfo(FadeImagesInfoClass fInfo)
    {
        if (currentFadeInfo != null)
        {
            foreach (GameObject _obj in currentFadeInfo.fadeOutObjects)
                _obj.SetActive(false);
            foreach (Text _text in currentFadeInfo.fadeOutTexts)
                _text.gameObject.SetActive(false);
        }

        currentFadeInfo = fInfo;
        fadeInSprites = currentFadeInfo.GetFadeInSpriteRenderers();
        fadeOutSprites = currentFadeInfo.GetFadeOutSpriteRenderers();
        fadeInTexts = currentFadeInfo.fadeInTexts;
        fadeOutTexts = currentFadeInfo.fadeOutTexts;

        foreach (GameObject _obj in currentFadeInfo.fadeInObjects)
            _obj.SetActive(true);

        foreach (Text _text in fadeInTexts)
        {
            Color col = _text.color;
            _text.color = new Color(col.r, col.g, col.b, 0f);
            _text.gameObject.SetActive(true);
        }

        foreach (SpriteRenderer _sprite in fadeInSprites)
            _sprite.color = new Color(1f, 1f, 1f, 0f);

        fadeInAlpha = 0f;
        fadeOutAlpha = 1f;

        fadeSpeed = currentFadeInfo.fadeSpeed <= 0f ? defaultFadeSpeed : currentFadeInfo.fadeSpeed;
        StartCoroutine("PhaseProcess", currentFadeInfo.workTime);

    }

    /// <summary>
    /// Процесс перехода между фазами
    /// </summary>
    /// <param name="_time">Время, в течение которого происходит переход между фазами</param>
    IEnumerator PhaseProcess(float _time)
    {
        yield return new WaitForSeconds(_time);

        currentPhase++;
        if (currentPhase < fadeInfos.Count)
            ManageFadeInfo(fadeInfos[currentPhase]);
        else
            StartCoroutine("FinalStatisticsProcess");
    }

    /// <summary>
    /// Процесс, в течение которого показывается финальная статистика
    /// </summary>
    IEnumerator FinalStatisticsProcess()
    {
        showStatistics = true;

        if (currentFadeInfo != null)
        {
            foreach (GameObject _obj in currentFadeInfo.fadeOutObjects)
                _obj.SetActive(false);
            foreach (Text _text in currentFadeInfo.fadeOutTexts)
                _text.gameObject.SetActive(false);
        }

        fadeInAlpha = 0f;
        fadeInSprites = new List<SpriteRenderer>();
        fadeOutSprites = new List<SpriteRenderer>();
        fadeOutTexts = new List<Text>();
        fadeInTexts = new List<Text> { gratitudeText, gameTimeText, deathCountText };

        fadeSpeed = defaultFadeSpeed;

        foreach (Text _text in fadeInTexts)
        {
            Color col = _text.color;
            _text.color = new Color(col.r, col.g, col.b, 0f);
            _text.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(endStatisticsFadeInTime);
        foreach (Text _text in fadeInTexts)
        {
            Color col = _text.color;
            _text.color = new Color(col.r, col.g, col.b, 1f);
        }
        fadeInTexts = new List<Text>();
        yield return new WaitForSeconds(endStatisticsTime);
        fadeOutTexts = new List<Text> { gratitudeText, gameTimeText, deathCountText };
        fadeOutAlpha = 1f;
        yield return new WaitForSeconds(endStatisticsFadeOutTime);
        ManageSaves();

    }

    /// <summary>
    /// Прерывание титров кнопкой прерывания
    /// </summary>
    void Interupt()
    {
        if (!showStatistics)
        {
            showStatistics = true;
            StopCoroutine("PhaseProcess");
            if (currentFadeInfo != null)
            {
                foreach (GameObject _obj in currentFadeInfo.fadeInObjects)
                    _obj.SetActive(false);

                foreach (GameObject _obj in currentFadeInfo.fadeOutObjects)
                    _obj.SetActive(false);

                foreach (Text _text in currentFadeInfo.fadeInTexts)
                    _text.gameObject.SetActive(false);

                foreach (Text _text in currentFadeInfo.fadeOutTexts)
                    _text.gameObject.SetActive(false);

            }

            fadeInTexts = new List<Text>();
            fadeInSprites = new List<SpriteRenderer>();
            fadeOutSprites = new List<SpriteRenderer>();
            fadeOutTexts = new List<Text>();

            StartCoroutine("FinalStatisticsProcess");
        }
        else
        {
            StopCoroutine("FinalStatisticsProcess");
            ManageSaves();
        }

    }

    /// <summary>
    /// Удалить сохранения пройденной игры.
    /// </summary>
    void ManageSaves()
    {

        noInput = true;

        savesInfoPath = (Application.dataPath) + "/StreamingAssets/SavesInfo.xml";
        SavesInfo savesInfo = Serializator.DeXmlSavesInfo(savesInfoPath);
        int profileNumber = PlayerPrefs.GetInt("Profile Number");
        SaveInfo sInfo = savesInfo.saves[profileNumber];
        sInfo.saveTime = System.DateTime.Now.ToString();
        sInfo.hasData = true;
        savesInfo.currentProfileNumb = profileNumber;
        sInfo.loadSceneName = firstLevelName;
        Serializator.SaveXmlSavesInfo(savesInfo, savesInfoPath);
        StartCoroutine(EndScreenProcess());
    }

    /// <summary>
    /// Процесс показа экрана ожидания в конце игры
    /// </summary>
    IEnumerator EndScreenProcess()
    {
        yield return new WaitForSeconds(endScreenTime);
        SceneManager.LoadScene(mainMenuName);
    }

    /// <summary>
    /// Применить языковые изменения
    /// </summary>
    /// <param name="_language">Язык, на который переходит окно</param>
    public virtual void MakeLanguageChanges(LanguageEnum _language)
    {
        foreach (MultiLanguageTextInfo _languageChange in languageChanges)
            _languageChange.text.text = _languageChange.mLanguageText.GetText(_language);
    }

}


/// <summary>
/// Специальный класс, хранящий информацию о том, какие изображения и тексты должны проявиться на экране
/// </summary>
[System.Serializable]
public class FadeImagesInfoClass
{
    public string fadeInfoName;

    public List<GameObject> fadeInObjects = new List<GameObject>(), fadeOutObjects = new List<GameObject>();//Игровые объекты, которые должны проявиться и затухнуть
    public List<Text> fadeInTexts = new List<Text>(), fadeOutTexts = new List<Text>();//Тексты, которые должны проявиться и затухнуть

    public float fadeSpeed = -1f;//Скорость затухания. Если -1, то берётся некоторая дефолтная скорость
    public float workTime;//Сколько времени будет работать данный представитель этого класса

    public List<SpriteRenderer> GetFadeInSpriteRenderers()
    {
        List<SpriteRenderer> sRenderers = new List<SpriteRenderer>();
        foreach (GameObject _obj in fadeInObjects)
            sRenderers.AddRange(GetSpriteRenderersFromGameObject(_obj));
        return sRenderers;
    }

    public List<SpriteRenderer> GetFadeOutSpriteRenderers()
    {
        List<SpriteRenderer> sRenderers = new List<SpriteRenderer>();
        foreach (GameObject _obj in fadeOutObjects)
            sRenderers.AddRange(GetSpriteRenderersFromGameObject(_obj));
        return sRenderers;
    }

    List<SpriteRenderer> GetSpriteRenderersFromGameObject(GameObject _obj)
    {
        List<SpriteRenderer> sRenderers = new List<SpriteRenderer>();
        SpriteRenderer _sRenderer = _obj.GetComponent<SpriteRenderer>();

        if (_sRenderer != null)
            sRenderers.Add(_sRenderer);

        for (int i=0;i<_obj.transform.childCount;i++)
        {
            GameObject childObject = _obj.transform.GetChild(i).gameObject;
            List<SpriteRenderer> childSRenderers = GetSpriteRenderersFromGameObject(childObject);
            for (int j = 0; j < childSRenderers.Count; j++)
                sRenderers.Add(childSRenderers[j]);
        }

        return sRenderers;
    }

}