using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// База данных различных историй, длящихся на протяжении всей игры
/// </summary>
public class HistoryBaseClass : ScriptableObject
{
    public List<StoryProgressBaseClass> stories = new List<StoryProgressBaseClass>();

    public HistoryBaseClass()
    {
        stories = new List<StoryProgressBaseClass>();
    }

}

/// <summary>
/// База данных различных состояний, развитий и развязок ОДНОЙ истории
/// </summary>
[System.Serializable]
public class StoryProgressBaseClass
{
    public string storyName;//Название истории
    public List<string> storyProgressNames=new List<string>();//Названия ответвлений истории

    public StoryProgressBaseClass(string _storyName, List<string> _storyProgressNames)
    {
        storyName = _storyName;
        storyProgressNames = _storyProgressNames;
    }

}