using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Экран, который показывается при успешном завершении уровня. Он показывает статистику уровня.
/// </summary>
public class LevelCompleteScreenScript : UIPanel, ILanguageChangeable
{

    #region consts

    protected const float collectionWidth = 50f;
    protected const string mainMenuName = "MainMenu";

    #endregion //consts

    #region fields

    protected Canvas canvas;
    protected Text levelCompleteText;
    protected Text secretsFoundText;
    protected Text collectionNameText;
    protected Text collectionNumbText;

    [SerializeField]protected GameObject collectionItemPanel;
    [SerializeField]protected List<MultiLanguageTextInfo> languageChanges = new List<MultiLanguageTextInfo>();

    #endregion //fields

    #region parametres

    protected MultiLanguageText levelMLText = new MultiLanguageText("Уровень ", "Level ", "Рівень ", "", ""), 
                                completeMLText = new MultiLanguageText(" пройден!", " complete!", " пройдено", "",""),
                                secretPlacesFoundMLText=new MultiLanguageText("Найдено секретных мест ", "Secret places were found: ", "Знайде секретних місць: ", "","");
    protected string nextLevelName;

    #endregion //parametres

    public virtual void Update()
    {
        if (canvas.enabled ? InterfaceWindow.openedWindow == null ? activePanel != this : false : false)
            SetActive();
    }

    public override void Initialize()
    {
        canvas = GetComponent<Canvas>();
        canvas.enabled = false;
        Transform panel = transform.FindChild("Panel");
        levelCompleteText = panel.FindChild("LevelCompleteText").GetComponent<Text>();
        secretsFoundText = panel.FindChild("SecretPlacesFoundText").GetComponent<Text>();
        collectionNameText = panel.FindChild("CollectionNameText").GetComponent<Text>();
        collectionNumbText = panel.FindChild("CollectionNumbText").GetComponent<Text>();
        collectionNameText.text = "";
        collectionNumbText.text = "";
    }

    /// <summary>
    /// Включить окно, знаменующее успешное завершение уровня 
    /// </summary>
    /// <param name="_nextLevelName">Название следующего уровня</param>
    /// <param name="currentSecretsCount">Количество найденных секретных мест</param>
    /// <param name="totalSecretsCount">Общее количество секретных мест на уровне</param>
    /// <param name="collection">Коллекция, которую можно собрать в данном сеттинге</param>
    public void SetLevelCompleteScreen(string _nextLevelName, int currentSecretsCount, int totalSecretsCount, ItemCollection collection)
    {
        Transform panel = transform.FindChild("Panel");
        SpecialFunctions.PauseGame();
        Cursor.visible = true;
        nextLevelName = _nextLevelName;
        LanguageEnum _language = SettingsScript.language;
        levelCompleteText.text = levelMLText.GetText(_language) + SpecialFunctions.GetLevelName().GetText(SettingsScript.language) + completeMLText.GetText(_language);

        secretsFoundText.text = secretPlacesFoundMLText.GetText(_language) + currentSecretsCount.ToString() + "/" + totalSecretsCount.ToString();

        if (collection != null)
        {
            collectionNameText.text = collection.collectionTextName.GetText(_language);
            float xPosition = -collectionWidth / 2f * collection.collection.Count;
            int secretsFoundCount = 0;
            collectionNumbText.GetComponent<RectTransform>().localPosition = new Vector3(xPosition, -30f, 0f);

            for (int i = 0; i < collection.collection.Count; i++)
            {
                xPosition += collectionWidth;
                GameObject newObject = Instantiate(collectionItemPanel, transform.position, Quaternion.identity);
                newObject.transform.SetParent(panel);
                RectTransform rTrans = newObject.GetComponent<RectTransform>();
                rTrans.localPosition = new Vector3(xPosition, -30f, 0f);
                rTrans.localScale = new Vector3(1f, 1f, 1f);
                rTrans.sizeDelta = new Vector2(collectionWidth, collectionWidth);
                Image _img = newObject.transform.FindChild("Item").GetComponent<Image>();
                if (collection.collection[i].itemFound)
                {
                    secretsFoundCount++;
                    _img.sprite = collection.collection[i].item.itemImage;
                }
                else
                    _img.color = new Color(0f, 0f, 0f, 0f);
            }
            collectionNumbText.text = secretsFoundCount.ToString() + "/" + collection.collection.Count.ToString();
            Animator anim=panel.FindChild("Image").GetComponent<Animator>();
            anim.SetTimeUpdateMode(UnityEngine.Experimental.Director.DirectorUpdateMode.UnscaledGameTime);
            anim.Play("Idle");
        }

        canvas.enabled = true;
        SpecialFunctions.totalPaused = true;
        SetActive();
    }

    /// <summary>
    /// Функция, переводящая игру на следующий уровень
    /// </summary>
    public void GoToTheNextLevel()
    {
        LoadingScreenScript.instance.LoadScene(nextLevelName);
        //SceneManager.LoadScene(nextLevelName);
    }

    /// <summary>
    /// Функция, возвращаюшая игру в главное меню
    /// </summary>
    public void GoToTheMainMenu()
    {
        SpecialFunctions.PlayGame();
        LoadingScreenScript.instance.LoadScene(mainMenuName);
        //SceneManager.LoadScene(mainMenuName);
    }

    /// <summary>
    /// Активировать элемент интерфейса
    /// </summary>
    public override void SetActive()
    {
        activePanel = this;
        currentIndex = new UIElementIndex(-1, -1);
    }

    /// <summary>
    /// Отмена
    /// </summary>
    public override void Cancel()
    {
    }

    /// <summary>
    /// Выдвинуться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    public override void MoveHorizontal(int direction)
    {
        if (currentIndex.indexX == -1 && currentIndex.indexY == -1)
        {
            base.Activate();
        }
        else
            base.MoveHorizontal(direction);
    }

    /// <summary>
    /// Выдвинуться в горизонтальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    /// <param name="_index">Индекс, с которого происходит перемещение</param>
    public override void MoveHorizontal(int direction, UIElementIndex _index)
    {
        if (currentIndex.indexX == -1 && currentIndex.indexY == -1)
        {
            base.Activate();
        }
        else
            base.MoveHorizontal(direction, _index);
    }

    /// <summary>
    /// Двинуться в вертикальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    public override void MoveVertical(int direction)
    {
        if (currentIndex.indexX == -1 && currentIndex.indexY == -1)
        {
            base.SetActive();
        }
        else
            base.MoveVertical(direction);
    }

    /// <summary>
    /// Двинуться в вертикальном направлении
    /// </summary>
    /// <param name="direction">Знак направления</param>
    public override void MoveVertical(int direction, UIElementIndex _index)
    {
        if (currentIndex.indexX == -1 && currentIndex.indexY == -1)
        {
            base.SetActive();
        }
        else
            base.MoveVertical(direction, _index);
    }

    /// <summary>
    /// Открыть окно инвентаря для смены оружия
    /// </summary>
    public void OpenEquipmentWindow()
    {
        SpecialFunctions.equipWindow.OpenWindow();
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
