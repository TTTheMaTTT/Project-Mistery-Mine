using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, реализующий поведение и перемещение камеры
/// </summary>
public class CameraController : MonoBehaviour
{

    #region consts

    protected const float offsetX = 0f;
    protected const float offsetY = 0.1f;
    protected const float offsetZ = -10f;

    protected const int shakeTimes = 5;
    protected const float shakeDistance = .02f;
    protected const float shakeTime = .02f;

    protected const float lightFadeSpeed = 3f;//Скорость изменения освещения уровня
    protected const float lightTransitionTime=1.3f;//Сколько времени будет происходить изменение уровня освещённости
    protected const float sizeTransitionSpeed= 1.5f;//Скорость изменения размера камеры

    #endregion //consts

    #region fields

    protected Transform player;//Трансформ героя
    protected Transform currentObject = null;//Текущий объект, за которым следит камера
    protected AreaTriggerIndicator aTriggerIndicator;//Индикатор, который закрепляется за камерой и следит за оптимизацией монстров
    protected SpriteLightKitImageEffect lightManager;//Компонент, ответственный за освещение игры
    protected Camera cam;

    #endregion //fields

    #region parametres

    protected Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);

    protected ETarget currentTarget;//Какую позицию стремится снять камера?
    [SerializeField]protected float camSpeed;
    protected bool instantMotion = true;//Камера мгновенно перемещается к текущей цели
    protected bool freeMode = false;//Движется ли камера в свободном режиме (т.е. при помощи стрелок)
    protected bool FreeMode
    {
        set
        {
            freeMode = value;
            BattleField bField = SpecialFunctions.battleField;
            if (bField != null)
            {
                Transform battlefieldTrans = bField.transform;
                if (value)
                    battlefieldTrans.parent = transform;
                else
                    battlefieldTrans.parent = SpecialFunctions.Player.transform.FindChild("Indicators");
                battlefieldTrans.localPosition = Vector3.zero;
            }
        }
    }

    protected float targetIntensity;//Целевой уровень окружающего освещения
    protected float targetHDRRatio;//Целевой уровень освещения от источников
    protected bool lightTransition;//Происходит ли постепенное изменения уровня освещённости в данный момент?

    protected float targetSize=1f;//Целевой размер камеры
    protected bool sizeTransition = false;//Происходит ли постепенное изменение размера камеры?

    #endregion //parametres

    protected void Awake()
    {
        Initialize();
    }

    protected void FixedUpdate()
    {
        if (freeMode)
        {
            if (Input.GetButton("CamHorizontal"))
                transform.position += Vector3.right * Input.GetAxis("CamHorizontal") * camSpeed/2f * Time.fixedDeltaTime;
            if (Input.GetButton("CamVertical"))
                transform.position += Vector3.up * Input.GetAxis("CamVertical") * camSpeed/2f * Time.fixedDeltaTime;
        }
        else
        {
            //if (Time.timeScale>0f) { 
            if (instantMotion)
                transform.position = currentTarget + offset;
            else
                transform.position = Vector3.Lerp(transform.position, currentTarget + offset, Time.fixedDeltaTime * camSpeed);
        }

        if (lightTransition)
        {
            //Постепенное изменение уровня освещённости
            lightManager.intensity = Mathf.Lerp(lightManager.intensity, targetIntensity, Time.fixedDeltaTime * lightFadeSpeed);
            lightManager.HDRRatio = Mathf.Lerp(lightManager.HDRRatio, targetHDRRatio, Time.fixedDeltaTime * lightFadeSpeed);
        }

        if (sizeTransition)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.fixedDeltaTime * sizeTransitionSpeed);
            if (Mathf.Abs(cam.orthographicSize - targetSize)<.01f)
            {
                cam.orthographicSize = targetSize;
                sizeTransition = false;
            }
        }

    }

    /*protected void Update()
    {
        if (Time.timeScale==0f)
        {
            if (camMod == CameraModEnum.player)
                transform.position = player.position + offset;
            else if (camMod==CameraModEnum.playerMove)
                transform.position = Vector3.Lerp(transform.position,player.position + offset,Time.deltaTime*camSpeed);
            else
                transform.position = camMod == CameraModEnum.move ? Vector3.Lerp(transform.position, currentPosition + offset, Time.deltaTime * camSpeed) : currentPosition + offset;
        }
    }*/

    protected void Initialize()
    {
        player = GameObject.FindGameObjectWithTag("player").transform;
        ChangeCameraMod(CameraModEnum.player);
        lightManager = GetComponent<SpriteLightKitImageEffect>();
        //aTriggerIndicator = GetComponentInChildren<AreaTriggerIndicator>();
        //aTriggerIndicator.Activate(false);
        cam = GetComponent<Camera>();
    }

    /// <summary>
    /// Переключить камеру на камеру в свободном режиме
    /// </summary>
    public void ChangeFreeMode()
    {
        FreeMode = !freeMode;
    }

    /// <summary>
    /// Изменить режим работы камеры
    /// </summary>
    public void ChangeCameraMod(CameraModEnum _camMod)
    {
        FreeMode = false;
        instantMotion = !(_camMod == CameraModEnum.move || _camMod == CameraModEnum.objMove || _camMod == CameraModEnum.playerMove);
        if (_camMod == CameraModEnum.player || _camMod == CameraModEnum.playerMove)
        {
            currentTarget = new ETarget(player);
            //aTriggerIndicator.Activate(!instantMotion);
        }
        else if (currentObject != null ? _camMod == CameraModEnum.obj || _camMod == CameraModEnum.objMove : false)
        {
            currentTarget = new ETarget(currentObject);
            //aTriggerIndicator.Activate(true);
        }
    }

    /// <summary>
    /// Изменить точку, за которой следит камера (В данном случае это объект и именно за ним будет следить камера)
    /// </summary>
    public void ChangeCameraTarget(GameObject newTarget, bool _instantMotion)
    {
        currentObject = newTarget.transform;
        instantMotion = _instantMotion;
        if (currentObject != null)
        {
            currentTarget = new ETarget(currentObject);
            //aTriggerIndicator.Activate(true);
        }
        else
        {
            currentTarget = new ETarget(player);
            //aTriggerIndicator.Activate(!instantMotion);
        }
    }

    /// <summary>
    /// Изменить точку, за которой следит камера (В данном случае это на эту точку указывает вектор)
    /// </summary>
    public void ChangeCameraTarget(Vector3 _position, bool _instantMotion)
    {
        currentTarget = new ETarget(_position);
        instantMotion = _instantMotion;
        //aTriggerIndicator.Activate(true);
    }

    /// <summary>
    /// Толкнуть камеру
    /// </summary>
    public void PushCamera(Vector3 addOffset)
    {
        offset = new Vector3(offsetX, offsetY, offsetZ);
        StopCoroutine("PushCameraProcess");
        StartCoroutine("PushCameraProcess", addOffset);
    }

    /// <summary>
    /// Процесс временного толчка камеры
    /// </summary>
    /// <param name="addOffset">Куда отталкивается камера</param>
    IEnumerator PushCameraProcess(Vector3 addOffset)
    {
        offset = new Vector3(offsetX, offsetY, offsetZ) + addOffset;
        yield return new WaitForSeconds(shakeTime * 3);
        offset = new Vector3(offsetX, offsetY, offsetZ);
    }

    /// <summary>
    /// Трясти камеру
    /// </summary>
    public void ShakeCamera()
    {
        offset = new Vector3(offsetX, offsetY, offsetZ);
        StopCoroutine("ShakeCameraProcess");
        StartCoroutine("ShakeCameraProcess");
    }

    public void ShakeCamera(float _shakeTime)
    {
        offset = new Vector3(offsetX, offsetY, offsetZ);
        StopCoroutine("ShakeCameraForTimeProcess");
        StartCoroutine("ShakeCameraForTimeProcess", _shakeTime);
    }

    /// <summary>
    /// Процесс тряски камеры
    /// </summary>
    IEnumerator ShakeCameraProcess()
    {
        float shakeReduce = shakeDistance / shakeTimes;
        float _shakeDistance = shakeDistance;
        for (int i = 0; i < shakeTimes; i++)
        {
            offset =  new Vector3(offsetX+Random.Range(-shakeDistance, _shakeDistance), offsetY+Random.Range(-shakeDistance, _shakeDistance),offsetZ);
            yield return new WaitForSeconds(shakeTime);
            _shakeDistance -= shakeReduce;
        }
        offset = new Vector3(offsetX, offsetY, offsetZ);
    }

    //Процесс тряски камеры в течение определённого времени
    IEnumerator ShakeCameraForTimeProcess(float _time)
    {
        float _shakeDistance = shakeDistance;
        while (_time>0f)
        {
            offset = new Vector3(offsetX + Random.Range(-shakeDistance, _shakeDistance), offsetY + Random.Range(-shakeDistance, _shakeDistance), offsetZ);
            yield return new WaitForSeconds(shakeTime);
            _time -= shakeTime;
        }
        offset = new Vector3(offsetX, offsetY, offsetZ);
    }

    /// <summary>
    /// Начать переход к новому уровню освещённости
    /// </summary>
    /// <param name="_intensity">Какой должна стать интенсивность</param>
    /// <param name="_HDRRatio">Какой должна стать интенсивность источников освещения</param>
    public void StartLightTransition(float _intensity, float _HDRRatio)
    {
        StopCoroutine("LightTransitionProcess");
        targetIntensity = _intensity;
        targetHDRRatio = _HDRRatio;
        StartCoroutine("LightTransitionProcess");
    }

    /// <summary>
    /// Процесс изменения уровня освещённости
    /// </summary>
    /// <returns></returns>
    IEnumerator LightTransitionProcess()
    {
        lightTransition = true;
        yield return new WaitForSeconds(lightTransitionTime);
        lightTransition = false;
    }

    /// <summary>
    /// Начать изменение размера камеры
    /// </summary>
    public void StartSizeTransition(float _targetSize=1f)
    {
        targetSize = _targetSize;
        sizeTransition = true;
    }

    public void SetPlayer(Transform _player)
    {
        player = _player;
        ChangeCameraMod(CameraModEnum.player);
    }

}
