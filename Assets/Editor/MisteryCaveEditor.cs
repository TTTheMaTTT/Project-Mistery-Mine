using UnityEngine;
using UnityEditor;
using System.Collections;

public class MisteryCaveEditor : Editor{

    [MenuItem("Mistery Cave/AI/Create Speech")]
    public static void CreateSpeech()
    {
        EditorWindow.GetWindow(typeof(SpeechCreateWindow));
    }
}
