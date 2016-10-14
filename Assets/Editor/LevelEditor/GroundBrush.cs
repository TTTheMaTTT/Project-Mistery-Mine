using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Класс, который представляет из себя ландшафтную кисть для создания твёрдых поверхностей.
/// </summary>
public class GroundBrush : ScriptableObject
{
    #region fields

    public string brushName;

    public Sprite defGround, outGround, inAngleGround, outAngleGround, edgeGround, marginGround, inGround, angleGround;//Виды игровых объектов, которые представляют
                                                                                                                           //различные представления земной поверхности в зависимости от расположения

    #endregion //fields

    #region parametres

    private bool incomplete;//Способ указания того, что кисть ещё дорабатывается
    public bool Incomplete { get { return incomplete; } set { incomplete = value; } }

    #endregion //parametres

    public bool ContainsSprite(Sprite _sprite)
    {
        return (defGround == _sprite) || (outGround == _sprite) || (inAngleGround == _sprite) || (outAngleGround == _sprite) || (edgeGround == _sprite) || (marginGround == _sprite) || (inGround == _sprite) || (angleGround == _sprite);
    }

}

[CustomEditor(typeof(GroundBrush))]
public class GroundBrushEditor: Editor
{
    public override void OnInspectorGUI()
    {

        GroundBrush grBrush = (GroundBrush)target;

        EditorGUILayout.LabelField("ground brush name", grBrush.brushName);

        EditorGUILayout.ObjectField("default ground",grBrush.defGround, typeof(Sprite));
        EditorGUILayout.ObjectField("outter ground", grBrush.outGround, typeof(Sprite));
        EditorGUILayout.ObjectField("inner angle ground",grBrush.inAngleGround, typeof(Sprite));
        EditorGUILayout.ObjectField("outter angle ground",grBrush.outAngleGround, typeof(Sprite));
        
        EditorGUILayout.ObjectField("edge ground",grBrush.edgeGround, typeof(Sprite));
        EditorGUILayout.ObjectField("margin ground",grBrush.marginGround, typeof(Sprite));
        EditorGUILayout.ObjectField("inner ground", grBrush.inGround, typeof(Sprite));
        EditorGUILayout.ObjectField("45 angle ground",grBrush.angleGround, typeof(Sprite));

    }
}
