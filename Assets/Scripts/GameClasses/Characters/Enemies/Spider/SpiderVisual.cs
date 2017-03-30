using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderVisual : GroundMoveVisual
{
    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        visualFunctions.Add("moveOut", MoveOut);
        visualFunctions.Add("setInGround", SetInGround);
    }

    /// <summary>
    /// Выдвинуться из земной тверди
    /// </summary>
    protected virtual void MoveOut(string id, int argument)
    {
        if (transform.localPosition.y < 0f)
            anim.Play("MoveOutVertical");
        else
            anim.Play("MoveOutHorizontal");
    }

    /// <summary>
    /// Расположить паука в земле
    /// </summary>
    /// <param name="id">В какую именно землю рассположить паука</param>
    protected virtual void SetInGround(string id, int argument)
    {
        if (id == "down")
            anim.Play("InGroundVertical");
        else if (id == "right")
            anim.Play("InGroundHorizontal");
    }

}