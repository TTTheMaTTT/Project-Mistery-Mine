using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;

/// <summary>
/// Класс, содержащий информацию о всех значимых событиях всей игры
/// </summary>
[XmlType("Game Progress Data")]
[XmlInclude(typeof(StoryProgressData))]
public class GameProgressData
{
    [XmlArray("Stories Progress")]
    [XmlArrayItem("Story Progress")]
    public List<StoryProgressData> storiesProgress = new List<StoryProgressData>();

    public GameProgressData()
    { }

    public GameProgressData(GameHistoryProgressClass gp)
    {
        storiesProgress = new List<StoryProgressData>();
        List<string> storyNames = gp.GetStories();
        foreach (string storyName in storyNames)
            storiesProgress.Add(new StoryProgressData(storyName, gp.GetStoryProgress(storyName)));
    }

}

/// <summary>
/// Класс, в котором хранится информация о ходе игрового прогресса в определённой истории, которая длится на протяжении всей игры
/// </summary>
[XmlType("Story Progress Data")]
public class StoryProgressData
{
    [XmlElement("Story Name")]
    public string storyName = "";

    [XmlElement("Story Progress")]
    public string storyProgress = "";

    public StoryProgressData()
    {
    }

    public StoryProgressData(string _storyName, string _storyProgress)
    {
        storyName = _storyName;
        storyProgress = _storyProgress;
    }

}
