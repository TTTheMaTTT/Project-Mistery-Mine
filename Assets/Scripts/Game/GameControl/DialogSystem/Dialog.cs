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
    public List<int> dialogParticipants = new List<int>();//ID участников диалога

    public bool stopGameProcess=true;//Если true, персонажи перестают передвигаться, героем нельзя управлять, а враги уходят домой
    public bool pause;//Ставится ли игра на паузу при воспроизведении диалога
    public bool sentPatrolHome=false;//Если true, то патрулирующие монстры при начале диалога будут сразу идти домой

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
    float width=300f;

    int size;
    Speech speechBuffer = null;
    SerializedObject serDialog;
    SerializedProperty speeches;

    void OnEnable()
    {
        dialog = (Dialog)target;
        speechBuffer = null;
        serDialog = new SerializedObject(dialog);
        speeches = serDialog.FindProperty("speeches");
    }

    /// <summary>
    /// Удалить реплику
    /// </summary>
    void DeleteSpeech(object _obj)
    {
        if (!(_obj is Speech))
            return;
        Speech _speech = (Speech)_obj;
        if (dialog != null)
        {
            dialog.speeches.Remove(_speech);
            serDialog.ApplyModifiedProperties();
            serDialog = new SerializedObject(dialog);
            speeches = serDialog.FindProperty("speeches");
            //speeches.DeleteArrayElementAtIndex(dialog.speeches.IndexOf(_speech));
            //speeches.arraySize--;
        }
    }

    /// <summary>
    /// Скопировать реплику
    /// </summary>
    void CopySpeech(object _obj)
    {
        if (!(_obj is Speech))
            return;
        Speech _speech = (Speech)_obj;
        if (dialog != null)
            speechBuffer = _speech;
    }

    /// <summary>
    /// Вставить реплику сразу после указанной реплики
    /// </summary>
    void InsertSpeech(object _obj)
    {
        if (!(_obj is Speech))
            return;
        Speech _speech = (Speech)_obj;
        if (speechBuffer != null)
            dialog.speeches.Insert(dialog.speeches.IndexOf(_speech)+1, new Speech(speechBuffer));        
    }

    /// <summary>
    /// Добавить новую реплику
    /// </summary>
    void AddNewSpeech()
    {
        speeches.InsertArrayElementAtIndex(speeches.arraySize);
        SerializedProperty _speech = speeches.GetArrayElementAtIndex(speeches.arraySize - 1);
        _speech.FindPropertyRelative("speechName").stringValue = "";
        _speech.FindPropertyRelative("edit").boolValue = false;
        _speech.FindPropertyRelative("speechMod").enumValueIndex = 0;
        _speech.FindPropertyRelative("hasText").boolValue = false;
        _speech.FindPropertyRelative("hasPositionChange").boolValue = false;
        _speech.FindPropertyRelative("hasAnimation").boolValue = false;
        _speech.FindPropertyRelative("hasOrientationChange").boolValue = false;
        _speech.FindPropertyRelative("fadeSpeed").floatValue = 0f;
        _speech.FindPropertyRelative("waitTime").floatValue = 0f;
        _speech.FindPropertyRelative("text").stringValue = "";
        _speech.FindPropertyRelative("portrait").objectReferenceValue = null;
        _speech.FindPropertyRelative("camMod").enumValueIndex = 3;
        _speech.FindPropertyRelative("camPosition").vector3Value = Vector3.zero;
        _speech.FindPropertyRelative("camObjectID").intValue = -1;

        _speech.FindPropertyRelative("changePositionData").ClearArray();
        _speech.FindPropertyRelative("changeOrientationData").ClearArray();
        _speech.FindPropertyRelative("animationData").ClearArray();
    }

    public override void OnInspectorGUI()
    {
        Event currentEvent = Event.current;
        Rect contextRect = new Rect(0, 0, 300, 80);

        dialog = (Dialog)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dialog Participants");

        dialog.dialogName = EditorGUILayout.TextField("dialog name", dialog.dialogName, GUILayout.Width(width), GUILayout.MinWidth(width), GUILayout.MaxWidth(width));

        List<int> dParticipants = dialog.dialogParticipants;
        for (int i=0;i< dParticipants.Count;i++)
        {
            EditorGUILayout.BeginHorizontal();
            int dialogID = dParticipants[i];
            GameObject _dialogObject=null;
            _dialogObject = (GameObject)EditorGUILayout.ObjectField(_dialogObject, typeof(GameObject), true, GUILayout.Width(.7f * width));
            if (_dialogObject != null)
            {
                DialogObject dObj = _dialogObject.GetComponent<DialogObject>();
                if (!dObj)
                {
                    dObj = _dialogObject.AddComponent<DialogObject>();
                    dObj.Initialize();
                }
                dParticipants[i] = dObj.ID;
                dialogID = dObj.ID;
            }
            EditorGUILayout.LabelField("id " + (dialogID != -1 ? "= " + dialogID.ToString() : "is not setted"), GUILayout.Width(.3f * width));
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add new Dialog Participant"))
            dParticipants.Add(-1);
        if (dParticipants.Count > 0)
            if (GUILayout.Button("Delete Dialog Participant"))
                dParticipants.RemoveAt(dParticipants.Count - 1);
        EditorGUILayout.Space();

        if (dialog.speeches == null)
        {
            dialog.speeches = new List<Speech>();
        }

        size = dialog.speeches.Count;
        EditorGUILayout.IntField("dialog size", size, GUILayout.Width(width));
        /*if (size != dialog.speeches.Count)
        {
            int m = dialog.speeches.Count;
            for (int i = m; i < size; i++)
                dialog.speeches.Add(new Speech());
            for (int i = m - 1; i >= size; i--)
                dialog.speeches.RemoveAt(i);
        }*/

        for (int i=0; i<dialog.speeches.Count;i++)
        {
            Speech speech = dialog.speeches[i];
            EditorGUILayout.Space();
            GUIStyle style = new GUIStyle();
            style.fixedHeight = 10f;
            GUI.BeginGroup(GUILayoutUtility.GetRect(new GUIContent(),style));

            EditorGUI.DrawRect(contextRect, Color.green);
            if (currentEvent.type == EventType.ContextClick)
            {
                Vector2 mousePos = currentEvent.mousePosition;
                if (contextRect.Contains(mousePos))
                {
                    // Now create the menu, add items and show it
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Copy"), false, CopySpeech, speech);
                    if (speechBuffer != null)
                        menu.AddItem(new GUIContent("Insert"), false, InsertSpeech, speech);
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Delete"), false, DeleteSpeech, speech);
                    menu.ShowAsContext();
                    currentEvent.Use();
                }
            }
            GUI.EndGroup();

            EditorGUILayout.PropertyField(speeches.GetArrayElementAtIndex(i),true);
            //speech.speechName = EditorGUILayout.TextField("speech name", speech.speechName, GUILayout.Width(width));
            //speech.text = EditorGUILayout.TextArea(speech.text, GUILayout.Height(60f), GUILayout.Width(width));
            //speech.portrait = (Sprite)EditorGUILayout.ObjectField("portrait", speech.portrait, typeof(Sprite), GUILayout.Width(width));

            //speech.camMod = (CameraModEnum)EditorGUILayout.EnumPopup("Camera Mod", speech.camMod, GUILayout.Width(width));
            //speech.camPosition = EditorGUILayout.Vector3Field("Camera Position", speech.camPosition, GUILayout.Width(width));
        }

        if (GUILayout.Button("AddSpeech"))
        {
            //dialog.speeches.Add(new Speech());
            //speeches = serDialog.FindProperty("speeches");
            AddNewSpeech();
        }

        dialog.stopGameProcess = EditorGUILayout.Toggle("Stop Game Process", dialog.stopGameProcess, GUILayout.Width(width));
        dialog.pause = EditorGUILayout.Toggle("pause", dialog.pause, GUILayout.Width(width));
        dialog.sentPatrolHome = EditorGUILayout.Toggle("Sent Patrol Enemies To Home", dialog.sentPatrolHome, GUILayout.Width(width));

        serDialog.ApplyModifiedProperties();
        EditorUtility.SetDirty(dialog);

        /*GUIStyle style = new GUIStyle();

        dialog.SetDirty();
        style.fixedHeight = 100.0f;

        GUI.BeginGroup(GUILayoutUtility.GetRect(new GUIContent(), style));

        EditorGUI.DrawRect(contextRect, Color.green);
        if (currentEvent.type == EventType.ContextClick)
        {
            Vector2 mousePos = currentEvent.mousePosition;
            if (contextRect.Contains(mousePos))
            {
                // Now create the menu, add items and show it
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("MenuItem1"), false, Callback, "item 1");
                menu.AddItem(new GUIContent("MenuItem2"), false, Callback, "item 2");
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("SubMenu/MenuItem3"), false, Callback, "item 3");
                menu.ShowAsContext();
                currentEvent.Use();
            }
        }
        //if (texture != null)
        //GUI.DrawTextureWithTexCoords(rPosition, texture, rPosition);

        GUI.EndGroup();*/

    }
}
#endif
