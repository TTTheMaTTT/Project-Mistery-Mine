using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Редактор уровней
/// </summary>
public class LevelEditor : EditorWindow
{

    #region fields

    #endregion //fields

    #region parametres

    private static bool isGrid;//Включить отображение сетки
    private static Vector2 gridSize = new Vector2(0.16f, 0.16f);//Размер сетки

    #endregion //parametres

    public void OnInspectorUpdate()
    {
        Repaint();
    }


    void OnEnable()
    {
        Editor.CreateInstance(typeof(SceneViewEventHandler));
    }



    /// <summary>
    /// Класс, что задаёт правила ввода в редакторе
    /// </summary>
    public class SceneViewEventHandler : Editor
    {
        static SceneViewEventHandler()
        {
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView sView)
        {

        }
    }

}
