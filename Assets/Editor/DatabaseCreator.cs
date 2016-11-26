using UnityEngine;
using UnityEditor;
using System.Collections;


/// <summary>
/// Окошко создания базы данных
/// </summary>
public class DatabaseCreator : EditorWindow
{

    #region parametres

    public string databaseName = "Database";

    public string databasePath = "Assets/Database/Databases/";

    public string databaseType = "quest";

    #endregion //parametres

    void OnGUI()
    {
        databaseName = EditorGUILayout.TextField(databaseName);
        databasePath = EditorGUILayout.TextField(databasePath);
        databaseType = EditorGUILayout.TextField(databaseType);

        if (GUILayout.Button("Create New"))
        {
            CreateDatabase();
        }
    }

    //Создаём новый предмет
    private void CreateDatabase()
    {
        if (databaseType == "quest")
        {
            DatabaseClass asset = ScriptableObject.CreateInstance<DatabaseClass>();
            asset.databaseName = databaseName;
            AssetDatabase.CreateAsset(asset, databasePath + databaseName + ".asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
        else if (databaseType == "item")
        {
            ItemBaseClass asset = ScriptableObject.CreateInstance<ItemBaseClass>();
            asset.databaseName = databaseName;
            AssetDatabase.CreateAsset(asset, databasePath + databaseName + ".asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }
}