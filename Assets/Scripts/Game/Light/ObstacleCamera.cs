using UnityEngine;
using System.Collections;

/// <summary>
/// Камера, что учитывает препятствия света и создаёт текстуру препятствий
/// </summary>
[ExecuteInEditMode]
public class ObstacleCamera : MonoBehaviour
{

    #region fields

    public Camera mainCamera;//Главная камера
    Material m1;
    public LayerMask obstacleLayer;//Слои, соответствующие препятствиям

    public Shader shader;//Шейдер, преобразующий изображения препятствий в текстуру препятствий
    public Shader disableShader;//Шейдер, используемый при выключении камеры, т.е при прекращении учёта препятствий
    public RenderTexture spriteObstacleRT;//Текстура, в которую заносятся все препятствия
    public RenderTexture finalSpriteObstacleRT;//преобразованная текстура

    [SerializeField]
    [HideInInspector]
    protected Camera spriteObstacleCamera;

    protected ObstacleTextureCreator OTCreator;//Функция, что создаёт финальную текстуру

    SpriteLightKit lKit;

    #endregion //fields

    #region parametres

    int lastScreenWidth = -1;
    int lastScreenHeight = -1;
    float previousCameraOrthoSize;

    Material _material;
    /// <summary>
    /// Создать материал, что будет преобразовывать текстуры
    /// </summary>
    protected Material material
    {
        get
        {
            if (_material == null)
            {
                _material = new Material(shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }

            return _material;
        }
    }

    #endregion //parametres

    /// <summary>
    /// При включении камеры
    /// </summary>
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (OTCreator == null)
            OTCreator = GetComponentInChildren<ObstacleTextureCreator>();

        if (lKit == null)
            lKit=transform.parent.GetComponentInChildren<SpriteLightKit>();

        PrepareCamera();
        lastScreenHeight = Screen.height;
        lastScreenWidth = Screen.width;
        OTCreator.PrepareCamera(lastScreenWidth, lastScreenHeight, previousCameraOrthoSize);
        UpdateTexture();
        transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// При включении камеры
    /// </summary>
    void OnEnable()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (OTCreator == null)
            OTCreator = GetComponentInChildren<ObstacleTextureCreator>();

        if (lKit == null)
            lKit = transform.parent.GetComponentInChildren<SpriteLightKit>();

        PrepareCamera();
        lastScreenHeight = Screen.height;
        lastScreenWidth = Screen.width;
        OTCreator.PrepareCamera(lastScreenWidth, lastScreenHeight, previousCameraOrthoSize);
        UpdateTexture();
        transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// При выключении камеры
    /// </summary>
    public void OnDisable()
    {

        if (spriteObstacleCamera != null)
            spriteObstacleCamera.targetTexture = null;

        if ((OTCreator != null)&& (spriteObstacleRT != null))
        {
            finalSpriteObstacleRT = OTCreator.Capture(spriteObstacleRT, new Material(disableShader));
            lKit.SetObstacleTexture(finalSpriteObstacleRT);
            spriteObstacleRT.Release();
            DestroyImmediate(spriteObstacleRT);
        }

        if (finalSpriteObstacleRT != null)
        {
            OTCreator.ResetTargetTexture();
            finalSpriteObstacleRT.Release();
            DestroyImmediate(finalSpriteObstacleRT);
        }

        if (_material)
        {
            DestroyImmediate(_material);
            _material = null;
        }

    }

    /// <summary>
    /// Функция, вызываемая перед рендером
    /// </summary>
    void OnPreRender()
    {
        // Если размер главной камеры изменился, то нужно изменить и размер этой камеры
        if (mainCamera.orthographicSize != previousCameraOrthoSize || lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
        {
            spriteObstacleCamera.orthographicSize = mainCamera.orthographicSize;
            previousCameraOrthoSize = mainCamera.orthographicSize;
            OTCreator.PrepareCamera(Screen.width, Screen.height, mainCamera.orthographicSize);
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            UpdateTexture();
        }
        CreateObstacleTexture();
    }

    /// <summary>
    /// Подготовить камеру к использованию
    /// </summary>
    void PrepareCamera()
    {
        if (spriteObstacleCamera != null)
        {
            previousCameraOrthoSize = mainCamera.orthographicSize;
            return;
        }

        spriteObstacleCamera = GetComponent<Camera>();
        if (spriteObstacleCamera == null)
        {
            spriteObstacleCamera = gameObject.AddComponent<Camera>();
            spriteObstacleCamera.backgroundColor = new Color(0f, 0f, 0f,0f);
        }

        spriteObstacleCamera.CopyFrom(mainCamera);

        spriteObstacleCamera.cullingMask = obstacleLayer;
        spriteObstacleCamera.clearFlags = CameraClearFlags.Color;
        spriteObstacleCamera.useOcclusionCulling = false;
        spriteObstacleCamera.targetTexture = null;

        // we need to render before the main camera
        spriteObstacleCamera.depth = mainCamera.depth - 11;
    }

    /// <summary>
    /// Обновить текстуру препятствий
    /// </summary>
    void UpdateTexture(bool forceRefresh = true)
    {
        if (spriteObstacleCamera == null)
            return;

        if (forceRefresh || spriteObstacleRT == null)
        {
            if (spriteObstacleRT != null)
            {
                spriteObstacleCamera.targetTexture = null;
                spriteObstacleRT.Release();
                DestroyImmediate(spriteObstacleRT);
            }

            // Ширина и высота рисуемой текстуры препятствий
            var rtWidth = Mathf.RoundToInt(spriteObstacleCamera.pixelWidth);
            var rtHeight = Mathf.RoundToInt(spriteObstacleCamera.pixelHeight);

            // Поменять формат создаваемой текстуры препятствий
            var format = RenderTextureFormat.Default;
            //if (!SystemInfo.SupportsRenderTextureFormat(format))
            //{
            //   Debug.LogWarning("Invalid texture format for this system. Defaulting to RenderTextureFormat.Default");
            //  format = RenderTextureFormat.Default;
            //}

            spriteObstacleRT = new RenderTexture(rtWidth, rtHeight, 0, format);
            finalSpriteObstacleRT = new RenderTexture(rtWidth, rtHeight, 0, format);
            spriteObstacleRT.name = "Sprite Obstacle RT";
            spriteObstacleRT.Create();
            //Graphics.Blit(spriteObstacleRT, finalSpriteObstacleRT, material);
            //finalSpriteObstacleRT = (RenderTexture)m1.mainTexture;
            spriteObstacleCamera.targetTexture = spriteObstacleRT;
            CreateObstacleTexture();
        }
    }


    /// <summary>
    /// Создать текстуру препятствий света
    /// </summary>
    void CreateObstacleTexture()
    {

        if (finalSpriteObstacleRT != null)
        {
            OTCreator.ResetTargetTexture();
            finalSpriteObstacleRT.Release();
            DestroyImmediate(finalSpriteObstacleRT);
        }

        finalSpriteObstacleRT = OTCreator.Capture(spriteObstacleRT, material);
        lKit.SetObstacleTexture(finalSpriteObstacleRT);

    }

}
