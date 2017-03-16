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

    #endregion //consts

    #region fields

    protected Transform player;//Трансформ героя
    protected Transform currentObject = null;//Текущий объект, за которым следит камера
    protected AreaTriggerIndicator aTriggerIndicator;//Индикатор, который закрепляется за камерой и следит за оптимизацией монстров

    #endregion //fields

    #region parametres

    protected Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);

    protected ETarget currentTarget;//Какую позицию стремится снять камера?
    [SerializeField]protected float camSpeed;
    protected bool instantMotion = true;//Камера мгновенно перемещается к текущей цели

    #endregion //parametres

    protected void Awake()
    {
        Initialize();
    }

    protected void FixedUpdate()
    {
        //if (Time.timeScale>0f) { 
        if (instantMotion)
            transform.position = currentTarget + offset;
        else
            transform.position = Vector3.Lerp(transform.position, currentTarget + offset, Time.fixedDeltaTime * camSpeed);
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
        //aTriggerIndicator = GetComponentInChildren<AreaTriggerIndicator>();
        //aTriggerIndicator.Activate(false);
    }

    /// <summary>
    /// Изменить режим работы камеры
    /// </summary>
    public void ChangeCameraMod(CameraModEnum _camMod)
    {
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

    public void SetPlayer(Transform _player)
    {
        player = _player;
        ChangeCameraMod(CameraModEnum.player);
    }

}
