using UnityEngine;
using UnityEngine.UI;
using System.IO;
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

    private const string groundBrushesPath = "Assets/Editor/LevelEditor/GroundBrushes/";//в этой папке находятся все нужные кисти

    bool parentMod = false;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        parentMod=EditorGUILayout.Toggle("Parent mod", parentMod);

        if (GUILayout.Button("Create Editor Colliders"))
            CreateEditorColliders();
        if (GUILayout.Button("Remove Editor Colliders"))
            RemoveEditorColliders();
        if (GUILayout.Button("Remove Colliders"))
            RemoveColliders();
        if (GUILayout.Button("Add Colliders"))
            AddColliders();
    }

    /// <summary>
    /// Добавить компонент EditorCollider в указанные объекты
    /// </summary>
    void CreateEditorColliders()
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
    void RemoveEditorColliders()
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

    }

    /// <summary>
    /// Функция, что удаляет компонент Collider2D из указанных объектов
    /// </summary>
    void RemoveColliders()
    {
        EditorColliderCreator creator = (EditorColliderCreator)target;
        EditorCollider edCol = null;
        Collider2D col = null;
        if (parentMod)
        {
            foreach (GameObject obj in creator.editorObjects)
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    GameObject editorObj = obj.transform.GetChild(i).gameObject;
                    if ((edCol = editorObj.GetComponent<EditorCollider>()) != null)
                        DestroyImmediate(edCol);
                    if ((col = editorObj.GetComponent<Collider2D>()) != null)
                        DestroyImmediate(col);
                }
        }
        else
            foreach (GameObject editorObj in creator.editorObjects)
            {
                if ((edCol = editorObj.GetComponent<EditorCollider>()) != null)
                    DestroyImmediate(col);
                if ((col = editorObj.GetComponent<Collider2D>()) != null)
                    DestroyImmediate(col);
            }
    }

    /// <summary>
    /// Добавить соответствующие коллайдеры в указанные объекты
    /// </summary>
    void AddColliders()
    {
        EditorColliderCreator creator = (EditorColliderCreator)target;
        List<GroundBrush> groundBrushes = new List<GroundBrush>();

        if (!Directory.Exists(groundBrushesPath))
        {
            AssetDatabase.CreateFolder("Assets/Editor/LevelEditor/", "GroundBrushes");
            AssetDatabase.Refresh();
            Debug.Log("Created Ground Brush Directory");
        }
        string[] brushNames = Directory.GetFiles(groundBrushesPath, "*.asset");
        groundBrushes = new List<GroundBrush>();
        foreach (string brushName in brushNames)
        {
            groundBrushes.Add(AssetDatabase.LoadAssetAtPath<GroundBrush>(brushName));
        }

        GroundBrush currentBrush=null;
        foreach (GameObject obj in creator.editorObjects)
        {
            if (parentMod)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    GameObject editorObj = obj.transform.GetChild(i).gameObject;
                    AddCollider(editorObj, groundBrushes, ref currentBrush);
                }
            }
            else
            {
                AddCollider(obj, groundBrushes, ref currentBrush);
            }
        }
    }

    /// <summary>
    /// Добавить нужный коллайдер в объект
    /// </summary>
    void AddCollider(GameObject obj, List<GroundBrush> _groundBrushes, ref GroundBrush currentBrush)
    {
        SpriteRenderer sRenderer = obj.GetComponent<SpriteRenderer>();
        if (!sRenderer)
            return;
        if (obj.GetComponent<Collider2D>() != null)
            return;
        Sprite sprite = sRenderer.sprite;
        if (currentBrush!=null?!currentBrush.ContainsSprite(sprite):true)
        {
            currentBrush = null;
            foreach (GroundBrush _groundBrush in _groundBrushes)
                if (_groundBrush.ContainsSprite(sprite))
                {
                    currentBrush = _groundBrush;
                    break;
                }
        }
        if (currentBrush == null)
            return;
        bool isAngle = currentBrush.angleGround == sprite;
        if (isAngle)
        {
            Vector2 texSize = sprite.textureRect.size;
            PolygonCollider2D col = obj.AddComponent<PolygonCollider2D>();
            col.points = new Vector2[3];
            col.points = new Vector2[]{new Vector2(texSize.x, texSize.y) / 2f / sprite.pixelsPerUnit,
                                        new Vector2(-texSize.x, -texSize.y) / 2f / sprite.pixelsPerUnit,
                                        new Vector2(texSize.x, -texSize.y) / 2f / sprite.pixelsPerUnit};
            col.isTrigger =false;
        }
        else
        {
            obj.AddComponent<BoxCollider2D>();
            obj.GetComponent<BoxCollider2D>().isTrigger = false;
        }
    }

}

#endif //UNITY_EDITOR