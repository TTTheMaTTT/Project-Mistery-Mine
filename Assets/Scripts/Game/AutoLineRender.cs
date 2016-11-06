using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, который автоматически подгоняет линию под нужные размеры
/// </summary>
public class AutoLineRender : MonoBehaviour
{

    #region parametres

    [SerializeField][HideInInspector]protected float ratio = 0.1f;
    [SerializeField][HideInInspector]protected Vector2 point1, point2;

    #endregion //parametres

    [ExecuteInEditMode]
    void Start()
    {
        AutoTile();
    }

    /// <summary>
    /// Установить параметры настройщика линий
    /// </summary>
    public void SetPoints(float _ratio, Vector2 _point1, Vector2 _point2)
    {
        ratio = _ratio;
        point1 = _point1;
        point2 = _point2;
    }

    /// <summary>
    /// Подогнать линию под нужные размеры
    /// </summary>
    public void AutoTile()
    {

        LineRenderer lRenderer = GetComponent<LineRenderer>();
        
        if (lRenderer != null)
        {
            float length=Vector2.Distance(point1,point2);
            lRenderer.sharedMaterial.SetFloat("_Tiling", length * ratio);
        }
    }
}