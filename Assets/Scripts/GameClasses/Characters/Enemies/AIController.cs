using UnityEngine;
using System.Collections;


/// <summary>
/// Базовый класс для персонажей, управляемых ИИ
/// </summary>
public class AIController : CharacterController
{

    #region consts

    protected const float sightRadius = 5f, sightOffset = 0.1f;

    #endregion //consts

    #region fields

    protected GameObject mainTarget;//Что является целью ИИ
    protected GameObject currentTarget;//Что является текущей целью ИИ

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
        mainTarget = SpecialFunctions.player;
        currentTarget = mainTarget;
    }

    /// <summary>
    /// Успокоиться
    /// </summary>
    protected virtual void BecomeCalm()
    {
        agressive = false;
        mainTarget = null;
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
        SpecialFunctions.statistics.ConsiderStatistics(this);
        Destroy(gameObject);
    }

}
