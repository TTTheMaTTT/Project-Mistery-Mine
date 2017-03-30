using UnityEngine;
using System.Collections;

/// <summary>
/// Визуальное оформление духа
/// </summary>
public class SpiritVisual: CharacterVisual
{

    #region fields

    protected Transform parent;
    protected GameObject spiritLight;//Освещение, создаваемое духом

    #endregion //fields

    #region parametres

    protected Vector3 pivot;//Точка, относительно которой движется дух

    [SerializeField] protected float speed = 1f;//Скорость
    [SerializeField] protected float xOffset = -1f, yOffset = 0f;//Смещение
    [SerializeField] protected float period=5f;//Период синусоидального движения
    [SerializeField] protected float amplitude = .1f;//Амплитуда синусоидального движения

    #endregion //parametres

    /// <summary>
    /// Здесь описано перемещение духа
    /// </summary>  
    protected virtual void FixedUpdate()
    {
        pivot = amplitude*Mathf.Sin(2 * Mathf.PI * Time.fixedTime / period) * Vector3.up;

        transform.localPosition = pivot;
    }

}
