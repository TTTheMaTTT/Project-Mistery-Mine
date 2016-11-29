using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Специальный класс, добавляющий объектам из указанного списка специальный компонент EditorCollider
/// </summary>
public class EditorColliderCreator : MonoBehaviour
{
    public List<GameObject> editorObjects = new List<GameObject>();
}

#if UNITY_EDITOR
[CustomEditor(typeof(EditorColliderCreator))]
public class EditorColliderCreator_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Create Editor Colliders"))
            CreateColliders();
        if (GUILayout.Button("Remove Editor Colliders"))
            RemoveColliders();

          
    }

    /// <summary>
    /// Добавить компонент EditorCollider в указанные объекты
    /// </summary>
    void CreateColliders()
    {
        EditorColliderCreator creator = (EditorColliderCreator)target;
        foreach (GameObject editorObj in creator.editorObjects)
            if (editorObj.GetComponent<EditorCollider>() == null)
            {
                editorObj.AddComponent<EditorCollider>();
            }
    }

    /// <summary>
    /// Убратьь компонент EditorCollider из указанных объектов 
    /// </summary>
    void RemoveColliders()
    {
        EditorColliderCreator creator = (EditorColliderCreator)target;
        EditorCollider col=null;
        foreach (GameObject editorObj in creator.editorObjects)
            if ((col = editorObj.GetComponent<EditorCollider>()) != null)
                DestroyImmediate(col);
    }

}

#endif //UNITY_EDITOR