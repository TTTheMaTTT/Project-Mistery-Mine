using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт, который может призывать игровые объекты (правильным образом)
/// </summary>
public class Summoner : MonoBehaviour, IHaveStory
{

    #region delegates

    public delegate void storyActionDelegate(StoryAction _action);

    #endregion //delegates

    #region dictionaries

    private Dictionary<string, storyActionDelegate> storyActionBase = new Dictionary<string, storyActionDelegate>(); //Словарь сюжетных действий
    public Dictionary<string, storyActionDelegate> StoryActionBase { get { return storyActionBase; } }

    #endregion //dictionaries

    #region fields

    public List<SummonClass> summons = new List<SummonClass>();
    public List<SummonClass> destroys = new List<SummonClass>();
    public List<SummonClass> activeObjects = new List<SummonClass>();

    #endregion //fields

    void Awake()
    {
        storyActionBase.Add("summon", InstantiateObject);
        storyActionBase.Add("destroy", DestroyObject);
    }

    /// <summary>
    /// Создать объект
    /// </summary>
    public void InstantiateObject(StoryAction _action)
    {
        SummonClass _summon = summons.Find(x => x.summonName == _action.id1);
        if (_summon != null)
            GameController.InstantiateWithId(_summon.summon, _summon.position, Quaternion.identity);
    }

    /// <summary>
    /// Уничтожить объект
    /// </summary>
    /// <param name="_action"></param>
    public void DestroyObject(StoryAction _action)
    {
        SummonClass _destroy = destroys.Find(x => x.summonName == _action.id1);
        if (_destroy != null? _destroy.summon!= null :false)
            Destroy(_destroy.summon);
    }

    /// <summary>
    /// Переместить объект на заданную позицию
    /// </summary>
    public void MoveObject(StoryAction _action)
    {
        SummonClass _obj = activeObjects.Find(x => x.summonName == _action.id1);
        if (_obj != null ? _obj.summon != null : false)
        {
            _obj.summon.transform.position = _obj.position;
            _obj.summon.SetActive(_obj.activate);
            if (_obj.activate)
                GameController.SetIDToNewObject(_obj.summon);
        }
    }

    /// <summary>
    /// Функция, возвращающая экземпляр SummonClass с объектом, который имеет заданное имя
    /// </summary>
    public SummonClass GetSummon(string _summonName)
    {
        SummonClass _summon = null;
        foreach (SummonClass activeObject in activeObjects)
            if (activeObject.summonName == _summonName)
            {
                _summon = activeObject;
                break;
            }
        foreach (SummonClass summon in summons)
            if (summon.summonName == _summonName)
            {
                _summon = summon;
                break;
            }
        return _summon;
    }

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    public virtual List<string> actionNames()
    {
        return new List<string>() { "summon", "destroy", "move"};
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() {{ "summon", summons.ConvertAll<string>(x=>x.summonName) },
                                                       { "destroy", destroys.ConvertAll<string>(x=>x.summonName)},
                                                       { "move", activeObjects.ConvertAll<string>(x=>x.summonName)}};
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() {{ "summon", new List<string>() { } },
                                                       { "destroy", new List<string>() { } },
                                                       { "move", new List<string>() { } }};
    }

    /// <summary>
    /// Вернуть словарь id-шников, настраивающих функцию проверки
    /// </summary>
    public virtual Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>() { { "", new List<string>()}};
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

/// <summary>
/// Класс, несущий информацию об объекте, который можно создать во время игры
/// </summary>
[System.Serializable]
public class SummonClass
{
    public string summonName="";
    public GameObject summon;
    public Vector3 position;
    public bool activate;

    public SummonClass(string _summonName, GameObject _summon, Vector3 _position)
    {
        summonName = _summonName;
        summon = _summon;
        position = _position;
    }

}