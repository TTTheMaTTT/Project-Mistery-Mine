using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif 

/// <summary>
/// Класс, представляюший собой диалог
/// </summary>
public class Dialog : ScriptableObject
{

    public string dialogName;

    public List<Speech> speeches;//Из каких реплик состоит диалог

    public bool pause;

    [HideInInspector]
    public int stage = 0;

    public Dialog()
    {
    }

    public Dialog(Dialog _dialog)
    {
        dialogName = _dialog.dialogName;
        speeches = _dialog.speeches;
        pause = _dialog.pause;
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(Dialog))]
public class CustomDialogEditor : Editor
{
    Dialog dialog;

    int size;

    public override void OnInspectorGUI()
    {
        dialog = (Dialog)target;

        dialog.dialogName = EditorGUILayout.TextField("dialog name", dialog.dialogName);

        if (dialog.speeches == null)
        {
            dialog.speeches = new List<Speech>();
        }

        size = dialog.speeches.Count;
        size = EditorGUILayout.IntField("dialog size", size);
        if (size != dialog.speeches.Count)
        {
            int m = dialog.speeches.Count;
            for (int i = m; i < size; i++)
                dialog.speeches.Add(new Speech());
            for (int i = m - 1; i >= size; i--)
                dialog.speeches.RemoveAt(i);
        }

        foreach (Speech speech in dialog.speeches)
        {
            EditorGUILayout.Space();
            speech.speechName = EditorGUILayout.TextField("speech name", speech.speechName);
            speech.text = EditorGUILayout.TextArea(speech.text, GUILayout.Height(60f));
            speech.portrait = (Sprite)EditorGUILayout.ObjectField("portrait", speech.portrait, typeof(Sprite));
        }

        dialog.pause = EditorGUILayout.Toggle("pause", dialog.pause);

        dialog.SetDirty();

    }
}
#endif
