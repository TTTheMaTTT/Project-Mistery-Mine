using UnityEngine;
using UnityEngine.UI;
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
    bool parentMod = false;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        parentMod=EditorGUILayout.Toggle("Parent mod", parentMod);

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
        if (parentMod)
        {
            foreach (GameObject obj in creator.editorObjects)
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    GameObject editorObj = obj.transform.GetChild(i).gameObject;
                    if (editorObj.GetComponent<EditorCollider>() == null)
                    {
                        editorObj.AddComponent<EditorCollider>();
                    }
                }
        }
        else
        {
            foreach (GameObject editorObj in creator.editorObjects)
                if (editorObj.GetComponent<EditorCollider>() == null)
                {
                    editorObj.AddComponent<EditorCollider>();
                }
        }
    }

    /// <summary>
    /// Убратьь компонент EditorCollider из указанных объектов 
    /// </summary>
    void RemoveColliders()
    {
        EditorColliderCreator creator = (EditorColliderCreator)target;
        EditorCollider col=null;
        if (parentMod)
        {
            foreach (GameObject obj in creator.editorObjects)
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    GameObject editorObj = obj.transform.GetChild(i).gameObject;
                    if ((col = editorObj.GetComponent<EditorCollider>()) != null)
                        DestroyImmediate(col);
                }
        }
        else
        {
            foreach (GameObject editorObj in creator.editorObjects)
                if ((col = editorObj.GetComponent<EditorCollider>()) != null)
                    DestroyImmediate(col);
        }
        Button button;

    }

}

#endif //UNITY_EDITOR