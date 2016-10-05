using UnityEngine;
using UnityEditor;
using System.Collections;

public class SpeechCreateWindow : EditorWindow
{
    public string speechName = "New Speech";

    public string dialogPath = "Assets/Database/Speeches/";

    void OnGUI()
    {
        speechName = EditorGUILayout.TextField(speechName);
        dialogPath = EditorGUILayout.TextField(dialogPath);
        if (GUILayout.Button("Create New"))
        {
            CreateNewSpeech();
        }
    }

    //Создаём новую модель поведения для ИИ
    private void CreateNewSpeech()
    {
        Speech asset = ScriptableObject.CreateInstance<Speech>();
        asset.speechName = speechName;
        AssetDatabase.CreateAsset(asset, dialogPath + speechName + ".asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}

