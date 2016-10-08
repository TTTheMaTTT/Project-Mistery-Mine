using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, представляющий собой реплику, что говорит персонаж
/// </summary>
public class Speech : ScriptableObject
{

    public string speechName;

    [TextArea(3, 10)]
    public string text;
    public Sprite portrait;

    public Speech nextSpeech;

    public bool pause;//Остановится ли ход игры во время этой реплики

}