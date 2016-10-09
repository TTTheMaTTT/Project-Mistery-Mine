using UnityEngine;
using UnityEditor;
using System.Collections;


/// <summary>
/// Окошко создания базы данных
/// </summary>
public class DatabaseCreator : EditorWindow
{
    public string databaseName = "Database";

    public string databasePath = "Assets/Database/Databases/";

    void OnGUI()
    {
        databaseName = EditorGUILayout.TextField(databaseName);
        databasePath = EditorGUILayout.TextField(databasePath);

        if (GUILayout.Button("Create New"))
        {
            CreateNewItem();
        }
    }

    //Создаём новый предмет
    private void CreateNewItem()
    {
        DatabaseClass asset = ScriptableObject.CreateInstance<DatabaseClass>();
        asset.databaseName = databaseName;
        AssetDatabase.CreateAsset(asset, databasePath + databaseName + ".asset");
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
