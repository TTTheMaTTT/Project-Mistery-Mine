using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Класс, являющийся родительским для всех боссов
/// </summary>
public class BossController : AIController
{
    #region eventHandlers

    public EventHandler<BossHealthEventArgs> bossHealthEvent;

    #endregion //eventHandlers

    #region parametres

    public override float Health { get { return health; } set { health = value; OnBossHealthChanged(new BossHealthEventArgs(health, maxHealth, gameObject.name)); } }

    #endregion //parametres

    /// <summary>
    /// Функция присоединения хп босса к игровому UI
    /// </summary>
    protected virtual void ConnectToUI()
    {
        bossHealthEvent += SpecialFunctions.gameUI.HandleBossHealthChanges;
        OnBossHealthChanged(new BossHealthEventArgs(health, maxHealth, gameObject.name));
    }

    /// <summary>
    /// Отсоединиться от игрового интерфейса
    /// </summary>
    protected virtual void DisconnectFromUI()
    {
        bossHealthEvent -= SpecialFunctions.gameUI.HandleBossHealthChanges;
        SpecialFunctions.gameUI.SetInactiveBossPanel();
    }

    protected override void BecomeAgressive()
    {
        base.BecomeAgressive();
        ConnectToUI();
    }

    protected override void BecomeCalm()
    {
        base.BecomeCalm();
        DisconnectFromUI();
    }

    protected override void Death()
    {
        DisconnectFromUI();
        base.Death();
    }

    #region events

    /// <summary>
    /// Вызвать событие "Здоровье босса изменилось"
    /// </summary>
    protected virtual void OnBossHealthChanged(BossHealthEventArgs e)
    {
        if (bossHealthEvent != null)
        {
            bossHealthEvent(this, e);
        }
    }

    #endregion //events

}
