using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Интерфейс для объектов, которые должны быть идентифицированы
/// </summary>
public interface IHaveID
{
    int GetID();

    void SetID(int _id);

    void SetData(InterObjData _intObjData);

    InterObjData GetData();
}

/// <summary>
/// Интерфейс, реализующий возможность получить урон
/// </summary>
public interface IDamageable: IHaveID
{

    void TakeDamage(float damage);

    void TakeDamage(float damage, bool ignoreInvul);

    float GetHealth();

    bool InInvul();

}

/// <summary>
/// Интерфейс, реализующий возможность внешнего взаимодействия
/// </summary>
public interface IInteractive: IHaveID
{
    void Interact();

}

/// <summary>
/// Интерфейс игрового механизма (двери, движущиеся платформы)
/// </summary>
public interface IMechanism: IHaveID
{
    void ActivateMechanism();

}

/// <summary>
/// Интерфейс, используемый для удобно настраиваемого редактора игровой истории
/// </summary>
public interface IHaveStory
{
    List<string> actionNames();//Вернуть имена сюжетно важных функций, выполняемых данным объектом
    Dictionary<string, List<string>> actionIDs1();//Вернуть id-шники,
    Dictionary<string, List<string>> actionIDs2();//настраивающие данные функции
    Dictionary<string, List<string>> conditionIDs();//id-шники, настраивающие функцию проверки
}
