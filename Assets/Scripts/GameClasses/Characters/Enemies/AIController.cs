using UnityEngine;
using System.Collections;


/// <summary>
/// Базовый класс для персонажей, управляемых ИИ
/// </summary>
public class AIController : CharacterController
{

    #region fields

    protected GameObject target;//Что является целью ИИ

    #endregion //fields

    #region parametres

    protected bool agressive = false;

    [SerializeField]protected float acceleration = 1f;

    [SerializeField] protected float damage = 1f;
    [SerializeField] protected float hitForce = 0f;
    [SerializeField] protected Vector2 attackSize = new Vector2(.07f, .07f);
    [SerializeField] protected Vector2 attackPosition = new Vector2(0f, 0f);

    #endregion //parametres

    protected override void Initialize()
    {
        base.Initialize();
        agressive = false;
    }

    /// <summary>
    /// Разозлиться
    /// </summary>
    protected virtual void BecomeAgressive()
    {
        agressive = true;
        target = SpecialFunctions.player;
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected virtual void BecomeCalm()
    {
        agressive = false;
        target = null;
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
    }

    /// <summary>
    /// Функция смерти
    /// </summary>
    protected override void Death()
    {
        base.Death();
        Destroy(gameObject);
    }

}
