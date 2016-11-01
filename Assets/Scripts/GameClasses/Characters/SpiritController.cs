using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер, урправляющий духом - спутником игрок
/// </summary>
public class SpiritController : CharacterController
{

    #region fields

    protected HeroController hero;//Персонаж, за которым следует дух

    #endregion //fields

    protected override void Initialize()
    {
        base.Initialize();
        hero = SpecialFunctions.player.GetComponent<HeroController>();
        transform.SetParent(hero.transform);
        transform.localPosition = Vector3.zero;

    }
}
