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

    protected Transform player;//Персонаж, за которым следует камера

    #endregion //fields

    #region parametres

    protected Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);

    protected Vector3 currentPosition;//Какую позицию стремится снять камера?
    [SerializeField]protected float camSpeed;

    [SerializeField] protected CameraModEnum camMod = CameraModEnum.player;//Режим перемещения камеры

    #endregion //parametres

    protected void Awake()
    {
        Initialize();
    }

    protected void FixedUpdate()
    {
        //if (Time.timeScale>0f) { 
        if (camMod == CameraModEnum.player)
            transform.position = player.position + offset;
        else if (camMod == CameraModEnum.playerMove)
            transform.position = Vector3.Lerp(transform.position, player.position + offset, Time.fixedDeltaTime * camSpeed);
        //transform.position = player.position + offset;
        else
            transform.position = (camMod == CameraModEnum.move) ? Vector3.Lerp(transform.position, currentPosition + offset, Time.fixedDeltaTime * camSpeed) : currentPosition + offset;
        //}
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
        currentPosition = player.position;
    }

    /// <summary>
    /// Изменить режим работы камеры
    /// </summary>
    public void ChangeCameraMod(CameraModEnum _camMod)
    {
        camMod = _camMod;
    }

    /// <summary>
    /// Изменить точку, за которой следит камера
    /// </summary>
    public void ChangeCameraTarget(Vector3 newTarget)
    {
        currentPosition = newTarget;
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

}
