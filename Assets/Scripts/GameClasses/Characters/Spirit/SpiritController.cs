using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер, урправляющий духом - спутником игрок
/// </summary>
public class SpiritController : CharacterController
{

    #region fields

    protected Transform hero;//Персонаж, за которым следует дух
    public Transform Hero { get { return hero; } set { hero = value; } }

    #endregion //fields

    #region parametres

    [SerializeField]
    protected float xOffset = 1f, yOffset = 0f;//Смещение

    #endregion //parametres

    protected override void Initialize()
    {
        base.Initialize();
        hero = SpecialFunctions.Player.transform;
    }

    protected virtual void FixedUpdate()
    {
        if (hero == null)
            return;
        Vector2 pivot = hero.position + new Vector3(xOffset * Mathf.Sign(hero.lossyScale.x), yOffset);
        transform.position = Vector2.Lerp(transform.position, pivot, Time.fixedDeltaTime * speed);
    }

    /// <summary>
    /// Воспроизвести вспышку света
    /// </summary>
    /// <param name="flashType"></param>
    protected void MakeFlash(string flashType)
    {
        Animate(new AnimationEventArgs("flash", flashType, 0));
    }


}
