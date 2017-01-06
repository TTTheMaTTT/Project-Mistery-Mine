using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif 


/// <summary>
/// Компонент игрового контроллера, ответственный за подсчёт игровых параметров, а также за хранение информации
/// </summary>
public class GameStatistics : MonoBehaviour, IHaveStory
{

    #region consts

    protected const string mapsPath = "Assets/Database/Maps/";//В этой папке находятся навигационные карты

    #endregion //consts

    #region dictionaries

    //Эти словари нужны для удобства
    Dictionary<string, WeaponClass> weaponDict = new Dictionary<string, WeaponClass>();//Словарь оружий
    public Dictionary<string, WeaponClass> WeaponDict { get { return weaponDict; } }

    Dictionary<string, ItemClass> itemDict = new Dictionary<string, ItemClass>();//Словарь предметов
    public Dictionary<string, ItemClass> ItemDict { get { return itemDict; } }

    Dictionary<string, GameObject> dropDict = new Dictionary<string, GameObject>();//Словарь дропа
    public Dictionary<string, GameObject> DropDict { get { return dropDict; } }

    List<ItemCollection> itemCollections = new List<ItemCollection>();//Игровые коллекции, учёт которых ведётся на данном уровне
    public List<ItemCollection> ItemCollections { get { return itemCollections; } }

    [HideInInspector]public NavigationSystem navSystem;//Карты уровня, используемые на данной сцене

    #endregion //dictionaries

    #region eventHandlers

    public EventHandler<StoryEventArgs> StatisticCountEvent;

    #endregion //eventHandlers

    #region fields

    [HideInInspector]
    public List<Statistics> statistics=new List<Statistics>();//Здесь задаётся, подсчёт каких игровых параметров нас интересует

    public DatabaseClass database;//База данных в игре
    public ItemBaseClass itemBase;//База предметов в игре

    #endregion //fields

    #region parametres

    [HideInInspector]
    public string statisticsPath = "Assets/Database/Stories/Statistics/";

    #endregion //parametres

    void Awake()
    {
        weaponDict = new Dictionary<string, WeaponClass>();
        foreach (WeaponClass weapon in itemBase.weapons)
        {
            if (!weaponDict.ContainsKey(weapon.itemName))
                weaponDict.Add(weapon.itemName, weapon);
        }

        itemDict = new Dictionary<string, ItemClass>();
        foreach (ItemClass item in itemBase.items)
        {
            if (!itemDict.ContainsKey(item.itemName))
                itemDict.Add(item.itemName, item);
        }

        dropDict = new Dictionary<string, GameObject>();
        foreach (GameObject dropObj in itemBase.drops)
        {
            DropClass drop = dropObj.GetComponent<DropClass>();
            if (drop != null ? drop.item != null ? !dropDict.ContainsKey(drop.item.itemName) : false : false)
                dropDict.Add(drop.item.itemName, dropObj);
        }

        itemCollections = new List<ItemCollection>();
        foreach (ItemCollection _collection in itemBase.collections)
            itemCollections.Add(new ItemCollection(_collection));

    }


    [ExecuteInEditMode]
    void Start()
    {
        //foreach (Statistics stat in statistics)
        //{
        //stat.value = 0;//Стоит пока обнулять значения статистик в самом начале игры.
        //}

#if UNITY_EDITOR

        if (navSystem == null)
        {
            if (!File.Exists(mapsPath + SceneManager.GetActiveScene().name + "NavSystem.asset"))
            {
                NavigationSystem _navSystem = new NavigationSystem(SceneManager.GetActiveScene().name);
                AssetDatabase.CreateAsset(_navSystem, mapsPath + SceneManager.GetActiveScene().name + "NavSystem.asset");
                AssetDatabase.SaveAssets();
                navSystem = _navSystem;
            }
            else
            {
                navSystem = AssetDatabase.LoadAssetAtPath<NavigationSystem>(mapsPath + SceneManager.GetActiveScene().name + "NavSystem.asset");
            }
        }

#endif //UNITY_EDITOR

    }

    public void ResetStatistics()
    {
        foreach (Statistics stat in statistics)
        {
            stat.value = 0;//Стоит пока обнулять значения статистик в самом начале игры.
        }
    }

    /// <summary>
    /// Возвращает квест по названию
    /// </summary>
    public Quest GetQuest(string _questName)
    {
        if (database != null)
            return database.quests.Find(x => x.questName == _questName);
        else
            return null;
    }

    /// <summary>
    /// При получении коллекционного предмета узнать, в какую коллекци он входит и вывести соответствующий экран
    /// </summary>
    /// <param name="_item">Рассматриваемый предмет</param>
    public void ConsiderCollectionItem(ItemClass _item)
    {
        List<ItemCollection> considerList = new List<ItemCollection>();
        foreach (ItemCollection _collection in itemCollections)
        {
            CollectorsItem cItem = _collection.collection.Find(x => x.item == _item);
            if (cItem != null)
            {
                cItem.itemFound = true;
                considerList.Add(_collection);
            }
        }

        SpecialFunctions.gameUI.ConsiderCollections(_item, considerList);//отоборазить изменения на экране
    }

    /// <summary>
    /// Произвести расчёт по нужным статистическим данным
    /// </summary>
    public void ConsiderStatistics(UnityEngine.Object obj)
    {
        List<Statistics> currentList = statistics.FindAll(x => (x.GetObjType.IsAssignableFrom(obj.GetType())));

        foreach (Statistics currentStatistics in currentList)
        {
            currentStatistics.Compare(obj);
            SpecialFunctions.StartStoryEvent(this, StatisticCountEvent, new StoryEventArgs(currentStatistics.statisticName,currentStatistics.value));
        }
    }

    /// <summary>
    /// Загрузить данные о игровой статистике уровня
    /// </summary>
    /// <param name="lStats"></param>
    public void LoadStaticstics(LevelStatsData lStats)
    {
        foreach (LevelStatisticsInfo lStatsInfo in lStats.statistics)
        {
            Statistics statsElement = statistics.Find(x => x.statisticName == lStatsInfo.statisticsName);
            if (statsElement != null)
                statsElement.value = lStatsInfo.statisticsValue;
        }
    }

    /// <summary>
    /// Обновить данные по всем статистикам (нужно для корректной загрузки списка квестов)
    /// </summary>
    public void InitializeAllStatistics()
    {
        foreach (Statistics stat in statistics)
        {
            SpecialFunctions.StartStoryEvent(this, StatisticCountEvent, new StoryEventArgs(stat.statisticName, stat.value));
        }
    }

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public virtual List<string> actionNames()
    {
        return new List<string>() { };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Вернуть словарь id-шников, связанных с конкретной функцией проверки условия сюжетного события
    /// </summary>
    public virtual Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>> { { "", new List<string>() },
                                                      { "compare", new List<string>() },
                                                      { "compareStatistics", statistics.ConvertAll<string>(x=>x.statisticName)} };
    }

    #endregion //IHaveStory

}

#if UNITY_EDITOR
[CustomEditor(typeof(GameStatistics))]
public class CustomGameStatistics_Editor : Editor
{

    Dictionary<string, Type> m_statisticsList=new Dictionary<string, Type>();
    List<string> m_statisticsNames = new List<string>();
    List<bool> foldouts = new List<bool>();

    GameStatistics gStats;

    public string statisticsName;

    void OnEnable()
    {
        gStats = (GameStatistics)target;
        var runtimeAssembly = Assembly.GetAssembly(typeof(Statistics));

        m_statisticsList=new Dictionary<string, Type>();
        m_statisticsNames = new List<string>();

        if (foldouts.Count != gStats.statistics.Count)
        {
            int min = Math.Min(foldouts.Count, gStats.statistics.Count);
            for (int i = min; i < gStats.statistics.Count; i++)
                foldouts.Add(false);
            for (int i = foldouts.Count - 1; i >= gStats.statistics.Count; i--)
                foldouts.RemoveAt(i);
        }

        foreach (var checkedType in runtimeAssembly.GetTypes())
        {
            if (typeof(Statistics).IsAssignableFrom(checkedType) && checkedType != typeof(Statistics))
            {
                m_statisticsList.Add(checkedType.FullName, checkedType);
                m_statisticsNames.Add(checkedType.FullName);
            }
        }
    }

    public override void OnInspectorGUI()
    {

        base.OnInspectorGUI();  //вызов функции отрисовки базового класса для показа публичных полей компонента   

        EditorGUILayout.BeginHorizontal();
        {

            statisticsName = EditorGUILayout.TextField("statisticsName", statisticsName);

            var selectIndex = EditorGUILayout.Popup(-1, m_statisticsNames.ToArray());

            for (int i = gStats.statistics.Count - 1; i >= 0; i--)
            {
                if (gStats.statistics[i] == null)
                    gStats.statistics.RemoveAt(i);
            }
                
            bool k = (gStats.statistics.Find(x => (x.statisticName == statisticsName)) == null);

            if (selectIndex >= 0 && k)
            {
                var statisticsClass = m_statisticsNames[selectIndex];

                var statistics = CreateInstance(m_statisticsList[statisticsClass]);
                DirectoryInfo dInfo = new DirectoryInfo(gStats.statisticsPath);
                dInfo.CreateSubdirectory(SceneManager.GetActiveScene().name + "/");
                AssetDatabase.CreateAsset(statistics, gStats.statisticsPath + SceneManager.GetActiveScene().name + "/" + statisticsName + ".asset");
                AssetDatabase.SaveAssets();

                Statistics s = (Statistics)statistics;
                s.statisticName = statisticsName;
                s.datapath = gStats.statisticsPath + SceneManager.GetActiveScene().name + "/" + statisticsName + ".asset";

                gStats.statistics.Add(s);
                foldouts.Add(false);

            }
        }
        EditorGUILayout.EndHorizontal();

        for (int i=0;i< gStats.statistics.Count;i++)
        {
            Statistics _stat = gStats.statistics[i];
            
            foldouts[i]=EditorGUILayout.Foldout(foldouts[i], _stat.statisticName);
            if (foldouts[i])    
            {
                var fields = _stat.GetType().GetFields();

                EditorGUILayout.Space();

                foreach (FieldInfo fieldInfo in fields)
                {
                    if (fieldInfo.FieldType == typeof(string))
                    {
                        if (fieldInfo.Name != "datapath" && fieldInfo.Name != "statisticName")
                        {
                            var value = (string)fieldInfo.GetValue(_stat);

                            value = EditorGUILayout.TextField(fieldInfo.Name, value);

                            fieldInfo.SetValue(_stat, value);
                        }
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Delete"))
                {
                    foldouts.RemoveAt(i);
                    gStats.statistics.Remove(_stat);
                    AssetDatabase.DeleteAsset(_stat.datapath);   
                }

            }

            _stat.SetDirty();
            EditorGUILayout.Space();

        }
        EditorUtility.SetDirty(gStats);

    }

}
#endif