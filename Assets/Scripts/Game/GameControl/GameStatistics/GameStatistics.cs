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
public class GameStatistics : MonoBehaviour
{

    #region eventHandlers

    public EventHandler<StoryEventArgs> StartGameEvent;
    public EventHandler<StoryEventArgs> StatisticCountEvent;

    #endregion //eventHandlers

    #region fields

    [HideInInspector]
    public List<Statistics> statistics=new List<Statistics>();//Здесь задаётся, подсчёт каких игровых параметров нас интересует

    public DatabaseClass database;//База данных в игре

    #endregion //fields

    #region parametres

    [HideInInspector]
    public string statisticsPath = "Assets/Database/Stories/Statistics/";

    #endregion //parametres
    
    void Start()
    {
        SpecialFunctions.history.Initialize();
        SpecialFunctions.StartStoryEvent(this, StartGameEvent, new StoryEventArgs());
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