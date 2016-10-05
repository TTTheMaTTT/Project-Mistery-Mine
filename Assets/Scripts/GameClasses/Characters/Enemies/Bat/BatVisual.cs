using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, отвечающий за визуальной отображение летучей мыши
/// </summary>
public class BatVisual : CharacterVisual
{
    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("idle", Idle);
        visualFunctions.Add("fly", Fly);
    }

    /// <summary>
    /// Отобразить стояние
    /// </summary>
    protected virtual void Idle(string id, int argument)
    {
        if (employment <= 6)
        {
            return;
        }
        anim.Play("Idle");
    }

    /// <summary>
    /// Отобразить полёт
    /// </summary>
    protected virtual void Fly(string id, int argument)
    {
        if (employment <= 6)
        {
            return;
        }
        anim.Play("Fly");
    }

}
