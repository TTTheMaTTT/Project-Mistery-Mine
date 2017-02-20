using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Экран, который показывается при успешном завершении уровня. Он показывает статистику уровня.
/// </summary>
public class LevelCompleteScreenScript : MonoBehaviour
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

    #endregion //fields

    #region parametres

    protected string nextLevelName;

    #endregion //parametres

    public void Awake()
    {
        Initialize();
    }

    public void Initialize()
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
        levelCompleteText.text = "Уровень " + SceneManager.GetActiveScene().name + " пройден!";

        secretsFoundText.text = "Найдено секретных мест " + currentSecretsCount.ToString() + "/" + totalSecretsCount.ToString();

        if (collection != null)
        {
            collectionNameText.text = collection.collectionName;
            float xPosition = -collectionWidth / 2f * collection.collection.Count;
            int secretsFoundCount = 0;
            collectionNumbText.GetComponent<RectTransform>().localPosition = new Vector3(xPosition, -30f, 0f);
            for (int i = 0; i < collection.collection.Count; i++)
            {
                xPosition += collectionWidth;
                GameObject newObject = new GameObject("ItemImage" + i.ToString());
                newObject.transform.SetParent(panel);
                RectTransform rTrans = newObject.AddComponent<RectTransform>();
                rTrans.localPosition = new Vector3(xPosition, -30f, 0f);
                rTrans.localScale = new Vector3(1f, 1f, 1f);
                rTrans.sizeDelta = new Vector2(collectionWidth, collectionWidth);
                Image _img = newObject.AddComponent<Image>();
                if (collection.collection[i].itemFound)
                {
                    secretsFoundCount++;
                    _img.sprite = collection.collection[i].item.itemImage;
                }
            }
            collectionNumbText.text = secretsFoundCount.ToString() + "/" + collection.collection.Count.ToString();
            Animator anim=panel.FindChild("Image").GetComponent<Animator>();
            anim.SetTimeUpdateMode(UnityEngine.Experimental.Director.DirectorUpdateMode.UnscaledGameTime);
            anim.Play("Idle");
        }

        canvas.enabled = true;
        SpecialFunctions.totalPaused = true;
    }

    /// <summary>
    /// Функция, переводящая игру на следующий уровень
    /// </summary>
    public void GoToTheNextLevel()
    {
        SceneManager.LoadScene(nextLevelName);
    }

    /// <summary>
    /// Функция, возвращаюшая игру в главное меню
    /// </summary>
    public void GoToTheMainMenu()
    {
        SceneManager.LoadScene(mainMenuName);
    }

    /// <summary>
    /// Открыть окно инвентаря для смены оружия
    /// </summary>
    public void OpenEquipmentWindow()
    {
        SpecialFunctions.equipWindow.OpenWindow();
    }

}
