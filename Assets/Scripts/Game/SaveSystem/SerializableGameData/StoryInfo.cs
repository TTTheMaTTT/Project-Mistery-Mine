using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Класс, в котором хранится информация о ходе игровой истории
/// </summary>
[XmlType("Story Info")]
public class StoryInfo
{
    [XmlArray("Stories")]
    [XmlArrayItem("Story")]
    public List<string> stories = new List<string>();

    [XmlArray("Completed Stories")]
    [XmlArrayItem("Completed Story")]
    public List<string> completedStories = new List<string>();

    public StoryInfo()
    {
    }

    public StoryInfo(History history)
    {
        stories = history.storyList.ConvertAll<string>(x => x.storyName);

        completedStories = new List<string>();
        foreach (StoryInitializer init in history.initList)
        {
            if (init.completed)
                completedStories.Add(init.story.storyName);
        }
    }

}
