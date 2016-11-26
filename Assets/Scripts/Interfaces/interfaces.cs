using UnityEngine;
using System.Collections;

/// <summary>
/// Интерфейс для объектов, которые должны быть идентифицированы
/// </summary>
public interface IHasID
{
    int GetID();

    void SetID(int _id);

    void SetData(InterObjData _intObjData);

    InterObjData GetData();
}

/// <summary>
/// Интерфейс, реализующий возможность получить урон
/// </summary>
public interface IDamageable: IHasID
{

    void TakeDamage(float damage);

    void TakeDamage(float damage, bool ignoreInvul);

    float GetHealth();

    bool InInvul();

}

/// <summary>
/// Интерфейс, реализующий возможность внешнего взаимодействия
/// </summary>
public interface IInteractive: IHasID
{
    void Interact();

}

/// <summary>
/// Интерфейс игрового механизма (двери, движущиеся платформы)
/// </summary>
public interface IMechanism: IHasID
{
    void ActivateMechanism();

}
