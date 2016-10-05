using UnityEngine;
using UnityEditor;
using System.Collections;

public class MisteryCaveEditor : Editor{

    [MenuItem("Mistery Cave/AI/Create Speech")]
    public static void CreateSpeech()
    {
        EditorWindow.GetWindow(typeof(SpeechCreateWindow));
    }

    [MenuItem("Mistery Cave/Create Item")]
    public static void CreateItem()
    {
        EditorWindow.GetWindow(typeof(ItemCreator));
    }

}
