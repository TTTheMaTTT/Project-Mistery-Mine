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

    void TakeDamage(HitParametres hitData);

    void TakeDamage(HitParametres hitData, bool ignoreInvul);

    float GetHealth();

    void TakeDamageEffect(DamageType _dType);

    bool InInvul();

}

/// <summary>
/// Интерфейс, реализующий возможность внешнего взаимодействия
/// </summary>
public interface IInteractive: IHaveID
{
    void Interact();

    void SetOutline(bool _outline);//Отрисовать контур обхекта, если с ним возможно произвести взаимодействие

    bool IsInteractive();//Можно ли взаимодействовать с объектом в данный момент?

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
    StoryAction.StoryActionDelegate GetStoryAction(string actionName);//Вернуть ссылку на функцию, соответствующую данному имени
}

/// <summary>
/// Интерфес объектов, которые каким-то образом меняются при смене языка
/// </summary>
public interface ILanguageChangeable
{
    void MakeLanguageChanges(LanguageEnum _language);//Произвести изменения, связанные с переменой языка игры
}