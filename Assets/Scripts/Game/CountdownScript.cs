using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Компонент, ответственный за обратный отсчёт. Если отсчёт кончится - игра заканчивается
/// </summary>
public class CountdownScript : MonoBehaviour, IHaveID, IHaveStory
{

    #region delegates

    public delegate void storyActionDelegate(StoryAction _action);

    #endregion //delegates

    #region dictionaries

    protected Dictionary<string, storyActionDelegate> storyActionBase = new Dictionary<string, storyActionDelegate>(); //Словарь сюжетных действий

    #endregion //dictionaries

    #region consts

    private const float endLevelTime = 5f;

    #endregion //consts

    #region parametres

    [SerializeField]
    private float countdownTime = 120f;

    [SerializeField]
    private string endGameText = "";

    [SerializeField]
    private int id;

    #endregion //parametres

    void Awake()
    {
        storyActionBase = new Dictionary<string, storyActionDelegate>();
        storyActionBase.Add("startCountdown", StoryStartCountdown);
        storyActionBase.Add("stopCountdown", StoryStopCountdown);
    }

    /// <summary>
    /// Процесс обратного отсчёта
    /// </summary>
    IEnumerator CountdownProcess()
    {
        yield return new WaitForSeconds(countdownTime);
        StartCoroutine("EndLevelProcess");
    }

    /// <summary>
    /// Процесс завершения игры
    /// </summary>
    IEnumerator EndLevelProcess()
    {
        SpecialFunctions.SetSecretText(6f,endGameText);
        SpecialFunctions.SetFade(true);
        SpecialFunctions.Player.GetComponent<HeroController>().SetImmobile(true);
        yield return new WaitForSeconds(endLevelTime);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Начать обратный отсчёт
    /// </summary>
    void StartCountdown()
    {
        StartCoroutine("CountdownProcess");
        SpecialFunctions.gameUI.StartCountdown(countdownTime);
    }

    #region IHaveID

    /// <summary>
    /// Вернуть id персонажа
    /// </summary>
    public virtual int GetID()
    {
        return id;
    }

    /// <summary>
    /// Установить заданное id
    /// </summary>
    public virtual void SetID(int _id)
    {
        id = _id;
        StartCountdown();
    }

    /// <summary>
    /// Настроить персонажа в соответствии с сохранёнными данными
    /// </summary>
    public virtual void SetData(InterObjData _intObjData)
    {
        StartCountdown();
    }

    /// <summary>
    /// Вернуть сохраняемые данные персонажа
    /// </summary>
    public virtual InterObjData GetData()
    {
        return new InterObjData(id, gameObject.name, transform.position);
    }

    #endregion //IHaveID

    #region storyActions

    /// <summary>
    /// Начать обратный отсчёт
    /// </summary>
    public void StoryStartCountdown(StoryAction _action)
    {
        StartCountdown();
    }

    /// <summary>
    /// Прекратить обратный отсчёт
    /// </summary>
    public void StoryStopCountdown(StoryAction _action)
    {
        StopCoroutine("CountdownProcess");
        SpecialFunctions.gameUI.StopCountdown();
        gameObject.SetActive(false);
    }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить скрипт
    /// </summary>
    /// <returns></returns>
    public List<string> actionNames()
    {
        return new List<string>() { "startCountdown", "stopCountdown" };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() { { "startCountdown", new List<string>() { } }, { "stopCountdown", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() { { "startCountdown", new List<string>() { } }, { "stopCountdown", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь id-шников, связанных с конкретной функцией проверки условия сюжетного события
    /// </summary>
    public Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Возвращает ссылку на сюжетное действие, соответствующее данному имени
    /// </summary>
    public StoryAction.StoryActionDelegate GetStoryAction(string s)
    {
        if (storyActionBase.ContainsKey(s))
            return storyActionBase[s].Invoke;
        else
            return null;
    }

    #endregion //IHaveStory

}
