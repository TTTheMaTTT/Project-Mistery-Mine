using UnityEngine;
using System.Collections;

/// <summary>
/// Интерфейс, реализующий возможность получить урон
/// </summary>
public interface IDamageable
{

    void TakeDamage(float damage);

    void TakeDamage(float damage, bool ignoreInvul);

    float GetHealth();
}

/// <summary>
/// Интерфейс, реализующий возможность внешнего взаимодействия
/// </summary>
public interface IInteractive
{
    void Interact();
}

/// <summary>
/// Интерфейс игрового механизма (двери, движущиеся платформы)
/// </summary>
public interface IMechanism
{
    void ActivateMechanism();
}
