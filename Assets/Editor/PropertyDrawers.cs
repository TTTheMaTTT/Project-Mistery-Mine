using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(BezierSimpleCurve))]
public class SimpleCurveDrawer : PropertyDrawer 
{

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty p0 = property.FindPropertyRelative("p0");
        SerializedProperty p1 = property.FindPropertyRelative("p1");
        SerializedProperty p2 = property.FindPropertyRelative("p2");
        SerializedProperty p3 = property.FindPropertyRelative("p3");

        SerializedProperty draw = property.FindPropertyRelative("draw");

        p0.vector2Value = EditorGUILayout.Vector2Field("p0", p0.vector2Value);
        p1.vector2Value = EditorGUILayout.Vector2Field("p1", p1.vector2Value);
        p2.vector2Value = EditorGUILayout.Vector2Field("p2", p2.vector2Value);
        p3.vector2Value = EditorGUILayout.Vector2Field("p3", p3.vector2Value);

        if (GUILayout.Button("Draw"))
            draw.boolValue = true;
        if (GUILayout.Button("StopDraw"))
            draw.boolValue = false;

    }

}

[CustomPropertyDrawer(typeof(Speech))]
public class SpeechDrawer : PropertyDrawer
{

    #region consts

    const float width = 300f;
    const float height = 75f;

    #endregion //consts

    GameObject camObject;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty
        speechName = property.FindPropertyRelative("speechName"),
        edit = property.FindPropertyRelative("edit"),
        speechMod = property.FindPropertyRelative("speechMod"),
        fadeSpeed = property.FindPropertyRelative("fadeSpeed"),
        waitTime = property.FindPropertyRelative("waitTime"),
        answer1 = property.FindPropertyRelative("answer1"),
        answer2 = property.FindPropertyRelative("answer2"),

        hasText = property.FindPropertyRelative("hasText"),
        text = property.FindPropertyRelative("text"),
        portrait = property.FindPropertyRelative("portrait"),

        hasPositionChange = property.FindPropertyRelative("hasPositionChange"),
        changePositionData = property.FindPropertyRelative("changePositionData"),

        hasOrientationChange = property.FindPropertyRelative("hasOrientationChange"),
        changeOrientationData = property.FindPropertyRelative("changeOrientationData"),

        hasAnimation = property.FindPropertyRelative("hasAnimation"),
        animationData = property.FindPropertyRelative("animationData"),

        camMod = property.FindPropertyRelative("camMod"),
        camPosition = property.FindPropertyRelative("camPosition"),
        camObjectID = property.FindPropertyRelative("camObjectID");


        speechName.stringValue = EditorGUILayout.TextField("Speech Name", speechName.stringValue, GUILayout.Width(width));

        edit.boolValue = EditorGUILayout.Foldout(edit.boolValue, "edit");
        if (!edit.boolValue)
            return;

        EditorGUILayout.PropertyField(speechMod, GUILayout.Width(width));
        SpeechModEnum _speechMod = (SpeechModEnum)speechMod.enumValueIndex;
        if (_speechMod == SpeechModEnum.wait || _speechMod == SpeechModEnum.waitFadeIn || _speechMod == SpeechModEnum.waitFadeInOut || _speechMod == SpeechModEnum.waitFadeOut)
        {
            if (_speechMod != SpeechModEnum.wait)
                fadeSpeed.floatValue = EditorGUILayout.FloatField("Fade Speed", fadeSpeed.floatValue, GUILayout.Width(width));
            waitTime.floatValue = EditorGUILayout.FloatField("Wait Time", waitTime.floatValue, GUILayout.Width(width));
        }
        else if (_speechMod == SpeechModEnum.answer)
        {
            EditorGUILayout.PropertyField(answer1, true);
            EditorGUILayout.PropertyField(answer2,true);
        }

        EditorGUILayout.PropertyField(camMod, GUILayout.Width(width));
        CameraModEnum _camMod = (CameraModEnum)camMod.enumValueIndex;
        if (_camMod == CameraModEnum.move || _camMod == CameraModEnum.position)
        {
            camObjectID.intValue = -1;
            camPosition.vector3Value = EditorGUILayout.Vector3Field("Camera Position", camPosition.vector3Value, GUILayout.Width(width));
        }
        else if (_camMod == CameraModEnum.obj || _camMod == CameraModEnum.objMove)
        {
            EditorGUILayout.BeginHorizontal();
            camObject = (GameObject)EditorGUILayout.ObjectField(camObject, typeof(GameObject), true, GUILayout.Width(.7f * width));
            if (camObject != null)
            {
                DialogObject dObj = camObject.GetComponent<DialogObject>();
                if (!dObj)
                {
                    dObj = camObject.AddComponent<DialogObject>();
                    dObj.Initialize();
                }
                camObjectID.intValue = dObj.ID;
            }
            EditorGUILayout.LabelField("id " + (camObjectID.intValue != -1 ? "= " + camObjectID.intValue.ToString() : "is not setted"), GUILayout.Width(.3f * width));
            EditorGUILayout.EndHorizontal();
            camPosition.vector3Value = Vector3.zero;
        }

        bool _hasText = hasText.boolValue;
        hasText.boolValue = EditorGUILayout.Toggle("Has Text", hasText.boolValue, GUILayout.Width(width));
        if (_hasText && !hasText.boolValue)
        {
            portrait.objectReferenceValue = null;
            text.stringValue = "";
        }
        if (_hasText)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(text, GUILayout.Width(.75f * width));
            portrait.objectReferenceValue=(Sprite)EditorGUILayout.ObjectField(portrait.objectReferenceValue, typeof(Sprite), GUILayout.Width(.25f * width),GUILayout.Height(height));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        /*bool _hasPositionChange = hasPositionChange.boolValue;
        hasPositionChange.boolValue = EditorGUILayout.Toggle("Has Position Change", hasPositionChange.boolValue, GUILayout.Width(width));
        if (_hasPositionChange && !hasPositionChange.boolValue)
        {
            changePositionData.ClearArray();
            _hasPositionChange = false;
        }
        if (_hasPositionChange)
        {
            for (int i=0;i<changePositionData.arraySize;i++)
            {
                EditorGUILayout.PropertyField(changePositionData.GetArrayElementAtIndex(i));
                if (GUILayout.Button("Delete", GUILayout.Width(width)))
                    changePositionData.DeleteArrayElementAtIndex(i);
                EditorGUILayout.Space();
            }
            if (GUILayout.Button("Add new Position Change", GUILayout.Width(width)))
                changePositionData.InsertArrayElementAtIndex(changePositionData.arraySize);
        }

        EditorGUILayout.Space();
        */

        DrawArrays(hasPositionChange, changePositionData, "Position Change");
        DrawArrays(hasOrientationChange, changeOrientationData, "Orientation Change");
        DrawArrays(hasAnimation, animationData, "Animation");


    }

    /// <summary>
    /// Специальная функция, что будет отрисовывать массивы специальных структур
    /// </summary>
    void DrawArrays(SerializedProperty drawFlag, SerializedProperty array, string arrayName)
    {
        bool _drawFlag = drawFlag.boolValue;
        drawFlag.boolValue = EditorGUILayout.Toggle("Has "+arrayName, drawFlag.boolValue, GUILayout.Width(width));
        if (_drawFlag && !drawFlag.boolValue)
        {
            array.ClearArray();
            _drawFlag = false;
        }
        if (_drawFlag)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty arrayElement = array.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(arrayElement,true);
                if (GUILayout.Button("Delete", GUILayout.Width(width)))
                    array.DeleteArrayElementAtIndex(i);
                EditorGUILayout.Space();
            }
            if (GUILayout.Button("Add new " + arrayName, GUILayout.Width(width)))
            {
                array.InsertArrayElementAtIndex(array.arraySize);
                SerializedProperty arrayElement = array.GetArrayElementAtIndex(array.arraySize-1);
                arrayElement.FindPropertyRelative("dialogID").intValue = -1;
            }
        }

        EditorGUILayout.Space();
    }

}

[CustomPropertyDrawer(typeof(SpeechAnswerClass))]
public class SpeechAnswerDrawer : PropertyDrawer
{

    #region consts

    const float width = 300f;

    #endregion //consts

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty
        answerText = property.FindPropertyRelative("answerText"),
        nextDialog = property.FindPropertyRelative("nextDialog");

        EditorGUILayout.BeginHorizontal();
        answerText.stringValue = EditorGUILayout.TextField("Answer Text", answerText.stringValue, GUILayout.Width(width * .7f));
        nextDialog.objectReferenceValue=(Dialog)EditorGUILayout.ObjectField(nextDialog.objectReferenceValue,typeof(Dialog),GUILayout.Width(width*.3f));
        EditorGUILayout.EndHorizontal();

    }
}

[CustomPropertyDrawer(typeof(SpeechChangePositionClass))]
public class SpeechPositionChangeDrawer : PropertyDrawer
{

    #region consts

    const float width = 300f;

    #endregion //consts

    Color col = new Color(.4f,1f,.4f);
    Rect rect = new Rect(0f, 0f, 330f, 80f);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty
        dialogID = property.FindPropertyRelative("dialogID"),
        _position = property.FindPropertyRelative("position");


        EditorGUI.DrawRect(new Rect(rect.x + position.x, rect.y + position.y, rect.width, rect.height), col);
        EditorGUILayout.BeginHorizontal();
        GameObject obj=null;
        obj = (GameObject)EditorGUILayout.ObjectField(obj, typeof(GameObject),true, GUILayout.Width(.7f*width));
        if (obj != null)
        {
            DialogObject dObj = obj.GetComponent<DialogObject>();
            if (!dObj)
            {
                dObj = obj.AddComponent<DialogObject>();
                dObj.Initialize();
            }
            dialogID.intValue = dObj.ID;
        }
        EditorGUILayout.LabelField("id " + (dialogID.intValue != -1 ? "= " + dialogID.intValue.ToString() : "is not setted"),GUILayout.Width(.3f* width));
        EditorGUILayout.EndHorizontal();

        _position.vector3Value = EditorGUILayout.Vector3Field("Position", _position.vector3Value, GUILayout.Width(width));

    }
}

[CustomPropertyDrawer(typeof(SpeechChangeOrientationClass))]
public class SpeechOrientationChangeDrawer : PropertyDrawer
{

    #region consts

    const float width = 300f;

    #endregion //consts

    Color col = new Color(.4f, 1f, 1f);
    Rect rect = new Rect(0f, 0f, 330f, 80f);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty
        dialogID = property.FindPropertyRelative("dialogID"),
        orientation = property.FindPropertyRelative("orientation");

        EditorGUI.DrawRect(new Rect(rect.x+position.x,rect.y+position.y,rect.width,rect.height), col);
        EditorGUILayout.BeginHorizontal();
        GameObject obj=null;
        obj = (GameObject)EditorGUILayout.ObjectField(obj, typeof(GameObject), true, GUILayout.Width(.7f * width));
        if (obj != null)
        {
            DialogObject dObj = obj.GetComponent<DialogObject>();
            if (!dObj)
            {
                dObj = obj.AddComponent<DialogObject>();
                dObj.Initialize();
            }
            dialogID.intValue = dObj.ID;
        }
        EditorGUILayout.LabelField("id " + (dialogID.intValue != -1 ? "= " + dialogID.intValue.ToString() : "is not setted"), GUILayout.Width(.3f * width));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(orientation, GUILayout.Width(width));

    }
}

[CustomPropertyDrawer(typeof(SpeechAnimationClass))]
public class SpeechAnimationDrawer : PropertyDrawer
{

    #region consts

    const float width = 300f;

    #endregion //consts

    Color col = new Color(1f, 1f, .4f);
    Rect rect = new Rect(0f, 0f, 330f, 120f);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty
        dialogID = property.FindPropertyRelative("dialogID"),
        animationName = property.FindPropertyRelative("animationName"),
        id = property.FindPropertyRelative("id"),
        argument = property.FindPropertyRelative("argument");

        EditorGUI.DrawRect(new Rect(rect.x + position.x, rect.y + position.y, rect.width, rect.height), col);
        EditorGUILayout.BeginHorizontal();
        GameObject obj=null;
        obj = (GameObject)EditorGUILayout.ObjectField(obj, typeof(GameObject), true, GUILayout.Width(.7f * width));
        if (obj != null)
        {
            DialogObject dObj = obj.GetComponent<DialogObject>();
            if (!dObj)
            {
                dObj = obj.AddComponent<DialogObject>();
                dObj.Initialize();
            }
            dialogID.intValue = dObj.ID;
        }
        EditorGUILayout.LabelField("id " + (dialogID.intValue != -1 ? "= " + dialogID.intValue.ToString() : "is not setted"), GUILayout.Width(.3f * width));
        EditorGUILayout.EndHorizontal();

        animationName.stringValue = EditorGUILayout.TextField("Animation Name", animationName.stringValue, GUILayout.Width(width));
        EditorGUILayout.BeginHorizontal();
        id.stringValue = EditorGUILayout.TextField("ID", id.stringValue, GUILayout.Width(width*.5f));
        argument.intValue = EditorGUILayout.IntField("Argument", argument.intValue, GUILayout.Width(width*.5f));
        EditorGUILayout.EndHorizontal();

    }
}