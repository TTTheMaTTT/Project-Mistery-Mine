using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Визуальное оформление духа
/// </summary>
public class SpiritVisual: CharacterVisual
{

    #region consts

    protected const float smallFlashTime = 1f, bigFlashTime=2f;

    #endregion //consts

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

    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("flash", Flash);
    }

    /// <summary>
    /// Воспроизвести вспышку света
    /// </summary>
    /// <param name="id">Вид вспышки</param>
    protected virtual void Flash(string id, int argument)
    {
        anim.Play(id + "Flash");
        StartCoroutine("FlashProcess", id == "Small" ? smallFlashTime : bigFlashTime);
    }

    protected virtual IEnumerator FlashProcess(float _flashTime)
    {
        yield return new WaitForSeconds(_flashTime);
        anim.Play("Idle");
    }

}
