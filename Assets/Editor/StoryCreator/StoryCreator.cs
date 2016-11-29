using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections;

/// <summary>
/// Окно создания игровой истории
/// </summary>
public class StoryCreator: EditorWindow
{
    public string storyName = "Item";

    public string storyPath = "Assets/Database/Stories/";

    public bool currentSceneStory = true;

    void OnGUI()
    {
        storyName = EditorGUILayout.TextField(storyName);
        storyPath = EditorGUILayout.TextField(storyPath);

        currentSceneStory = EditorGUILayout.Toggle("current scene story", currentSceneStory);

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
        if (currentSceneStory)
            asset.sceneName = SceneManager.GetActiveScene().name;
        AssetDatabase.CreateAsset(asset, storyPath + storyName + ".asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}