using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif 

/// <summary>
/// Класс, представляющий собой реплику, что говорит персонаж
/// </summary>
[System.Serializable]
public class Speech 
{
    public string speechName;

    [TextArea(3, 10)]
    public string text;

    public Sprite portrait;

    public bool moveCam;//Если true, то когда реплика появится на экране, камера передвинется к нужному месту
    public Vector3 camPosition;//Позиция камеры при реплике

}