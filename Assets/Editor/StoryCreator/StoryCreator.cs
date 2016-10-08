using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Окно создания игровой истории
/// </summary>
public class StoryCreator: EditorWindow
{
    public string storyName = "Item";

    public string storyPath = "Assets/Database/Stories/";

    void OnGUI()
    {
        storyName = EditorGUILayout.TextField(storyName);
        storyPath = EditorGUILayout.TextField(storyPath);

        if (GUILayout.Button("Create New"))
        {
            CreateNewStory();
        }
    }

    //Создаём новый квест
    private void CreateNewStory()
    {
        Story asset = ScriptableObject.CreateInstance<Story>();
        asset.storyName = storyName;
        AssetDatabase.CreateAsset(asset, storyPath + storyName + ".asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}