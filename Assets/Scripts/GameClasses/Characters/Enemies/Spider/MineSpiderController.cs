using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, управляющий пауком шахты - агрессивной версией обычного паука
/// </summary>
public class MineSpiderController : SpiderController
{

    protected override void Initialize()
    {
        base.Initialize();
        neutral = false;
        sight.enabled = true;
        sight.RotateLocal((spiderOrientation.y>=0 && Mathf.Abs(spiderOrientation.x)-Mathf.Abs(spiderOrientation.y)<0) ? 0f: 90f);
    }
}
