using UnityEngine;
using UnityEditor;
using System.Collections;

public class MisteryCaveEditor : Editor{

    [MenuItem("Mistery Cave/Create Speech")]
    public static void CreateDialog()
    {
        EditorWindow.GetWindow(typeof(DialogCreateWindow));
    }

    [MenuItem("Mistery Cave/Create Item")]
    public static void CreateItem()
    {
        EditorWindow.GetWindow(typeof(ItemCreator));
    }

    [MenuItem("Mistery Cave/Create Quest")]
    public static void CreateQuest()
    {
        EditorWindow.GetWindow(typeof(QuestCreator));
    }

    [MenuItem("Mistery Cave/Create Story")]
    public static void CreateStory()
    {
        EditorWindow.GetWindow(typeof(StoryCreator));
    }

    [MenuItem("Mistery Cave/Create Database")]
    public static void CreateDatabase()
    {
        EditorWindow.GetWindow(typeof(DatabaseCreator));
    }

    [MenuItem("Mistery Cave/LevelEditor/LevelEditor")]
    public static void LevelEditor()
    {
        EditorWindow.GetWindow(typeof(LevelEditor));
    }

}
    