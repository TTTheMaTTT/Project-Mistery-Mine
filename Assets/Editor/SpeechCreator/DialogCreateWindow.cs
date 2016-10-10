using UnityEngine;
using UnityEditor;
using System.Collections;

public class DialogCreateWindow : EditorWindow
{
    public string dialogName = "New Dialog";

    public string dialogPath = "Assets/Database/Speeches/";

    void OnGUI()
    {
        dialogName = EditorGUILayout.TextField(dialogName);
        dialogPath = EditorGUILayout.TextField(dialogPath);
        if (GUILayout.Button("Create New"))
        {
            CreateNewSpeech();
        }
    }

    //Создаём новую модель поведения для ИИ
    private void CreateNewSpeech()
    {
        Dialog asset = ScriptableObject.CreateInstance<Dialog>();
        asset.dialogName = dialogName;
        AssetDatabase.CreateAsset(asset, dialogPath + dialogName + ".asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}


