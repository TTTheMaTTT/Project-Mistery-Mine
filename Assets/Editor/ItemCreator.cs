using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Окошко создания предметов
/// </summary>
public class ItemCreator : EditorWindow
{
    public string itemName = "Item";

    public string itemPath = "Assets/Database/Items/";

    public string itemType = "sword";

    void OnGUI()
    {
        itemName = EditorGUILayout.TextField(itemName);
        itemPath = EditorGUILayout.TextField(itemPath);
        itemType = EditorGUILayout.TextField(itemType);

        if (GUILayout.Button("Create New"))
        {
            CreateNewItem();
        }
    }

    //Создаём новый предмет
    private void CreateNewItem()
    {
        if (itemType == "sword")
        {
            SwordClass asset = ScriptableObject.CreateInstance<SwordClass>();
            asset.itemName = itemName;
            AssetDatabase.CreateAsset(asset, itemPath + itemName + ".asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
        else if (itemType == "bow")
        {
            BowClass asset = ScriptableObject.CreateInstance<BowClass>();
            asset.itemName = itemName;
            AssetDatabase.CreateAsset(asset, itemPath + itemName + ".asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
        else if (itemType == "heart")
        {
            HeartClass asset = ScriptableObject.CreateInstance<HeartClass>();
            asset.itemName = itemName;
            AssetDatabase.CreateAsset(asset, itemPath + itemName + ".asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }
}
