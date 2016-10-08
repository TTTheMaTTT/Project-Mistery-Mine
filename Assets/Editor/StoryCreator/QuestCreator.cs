using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Окошко создания квестов
/// </summary>
public class QuestCreator : EditorWindow
{
    public string questName = "Quest";

    public string questPath = "Assets/Database/Quests/";

    void OnGUI()
    {
        questName = EditorGUILayout.TextField(questName);
        questPath = EditorGUILayout.TextField(questPath);

        if (GUILayout.Button("Create New"))
        {
            CreateNewQuest();
        }
    }

    //Создаём новый квест
    private void CreateNewQuest()
    {
        Quest asset = ScriptableObject.CreateInstance<Quest>();
        asset.questName = questName;
        AssetDatabase.CreateAsset(asset, questPath + questName + ".asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
