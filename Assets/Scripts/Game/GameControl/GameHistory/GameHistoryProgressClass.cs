using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, содержащий в себе информацию о прогрессе игры
/// </summary>
public class GameHistoryProgressClass
{

    private const string defaultStoryProgressName = "notStarted";//Что возвращается, когда запрашиваемая история не имеет своего прогресса

    public Dictionary<string, string> gameHistoryDictionary = new Dictionary<string, string>();//Словарь, в котором хранится информация о каждой значимой в прогрессе игры статье. Считаем, что ключи этого словаря - это
                                                                                               //истории, а значения - состояния историй

    public static string DefaultProgress { get { return defaultStoryProgressName; } }

    public GameHistoryProgressClass()
    {
        gameHistoryDictionary = new Dictionary<string, string>();
    }

    public void ClearDictionary()
    {
        gameHistoryDictionary = new Dictionary<string, string>();
    }

    /// <summary>
    /// Функция, возвращающая список историй, способных повлиять на события игры
    /// </summary>
    /// <returns></returns>
    public List<string> GetStories()
    {
        List<string> _stories = new List<string>();
        foreach (string story in gameHistoryDictionary.Keys)
            _stories.Add(story);
        return _stories;
    }

    /// <summary>
    /// Изменить одну из игровых историй
    /// </summary>
    public void ChangeStoryProgress(string storyName, string storyProgress)
    {
        if (gameHistoryDictionary.ContainsKey(storyName))
            gameHistoryDictionary[storyName] = storyProgress;
        else
            gameHistoryDictionary.Add(storyName, storyProgress);
    }

    /// <summary>
    /// Инициализировать данные прогресса игры, используя сохранённые данные
    /// </summary>
    public void SetStoryProgressData(GameProgressData gData)
    {
        gameHistoryDictionary = new Dictionary<string, string>();
        if (gData != null)
            foreach (StoryProgressData sData in gData.storiesProgress)
                gameHistoryDictionary.Add(sData.storyName, sData.storyProgress);
    }

    /// <summary>
    /// Узнать прогресс выбранной истории
    /// </summary>
    /// <param name="storyName">Название истории</param>
    public string GetStoryProgress(string storyName)
    {
        if (!gameHistoryDictionary.ContainsKey(storyName))
            return DefaultProgress;
        else
            return gameHistoryDictionary[storyName];
    }

}
