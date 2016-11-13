using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, который создаёт текстуру препятствий
/// </summary>
public class ObstacleTextureCreator : MonoBehaviour
{
    #region fields

    protected MeshRenderer quad;//Квад, на котором будет создаваться наша текстура
    protected Camera cam;//Камера, рендерящая текстуру

    #endregion //fields

    #region parametres

    int lastScreenWidth = 801;
    int lastScreenHeight = 422;
    float previousCameraOrthoSize=1f;

    #endregion //parametres

    /// <summary>
    /// Подготовить камеру к рендеру
    /// </summary>
    public void PrepareCamera(int _screenWidth, int _screenHeight, float _cameraOrthoSize)
    {
        if (cam == null)
            cam = GetComponent<Camera>();
        if (quad == null)
            quad = GetComponentInChildren<MeshRenderer>();
        Vector3 scal = quad.transform.localScale;
        quad.transform.localScale = new Vector3(scal.x / lastScreenWidth * _screenWidth * lastScreenHeight / _screenHeight / previousCameraOrthoSize * _cameraOrthoSize,
                         scal.y / previousCameraOrthoSize * _cameraOrthoSize,
                         scal.z);
        cam.orthographicSize = _cameraOrthoSize;
        previousCameraOrthoSize = _cameraOrthoSize;
        lastScreenWidth = _screenWidth;
        lastScreenHeight = _screenHeight;
    }

    /// <summary>
    /// Функция, что создаёт новую текстуру из исходной, используя материал
    /// </summary>
    /// <param name="исходная текстура"></param>
    /// <param name="используемый материал для рендера"></param>
    /// <returns></returns>
    public RenderTexture Capture(RenderTexture source, Material material)
    {
        quad.sharedMaterial = material;
        quad.sharedMaterial.SetTexture("_MainTex", source);
        RenderTexture temp = RenderTexture.GetTemporary(lastScreenWidth, lastScreenHeight);
        cam.targetTexture = temp;
        cam.Render();
        return temp;
    }

    public void ResetTargetTexture()
    {
        if (cam.targetTexture != null)
            cam.targetTexture = null;
    }

}
