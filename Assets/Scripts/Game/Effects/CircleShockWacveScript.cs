using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер, управляющей сферической (т.е. круговой, мы же в 2D)) волной
/// </summary>
public class CircleShockWacveScript : MonoBehaviour
{

    protected const float shockWaveSpeed = 6f;//Скорость распространения
    protected const float lifeTime = 3f;//Время жизни

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        transform.localScale += Vector3.one * shockWaveSpeed * Time.deltaTime;
    }

}
