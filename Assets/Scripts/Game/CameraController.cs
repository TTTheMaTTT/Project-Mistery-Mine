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

    #endregion //consts

    #region fields

    protected Transform player;//Персонаж, за которым следует камера

    #endregion //fields

    protected void Awake ()
    {
        Initialize();
	}
	
	protected void  FixedUpdate ()
    {
        transform.position = player.position + new Vector3(offsetX,offsetY,offsetZ);    
	}

    protected void Initialize()
    {
        player = GameObject.FindGameObjectWithTag("player").transform;
    }
}
