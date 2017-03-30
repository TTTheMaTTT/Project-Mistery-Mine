using UnityEngine;
using System.Collections;

public class LavaGolemController : GolemController
{

    #region consts

    protected const float wetDamageCoof = .5f;//Коэффициент, на который домножается урон, когда персонаж находится в мокром состоянии

    #endregion //consts

    protected override void Start()
    {
        base.Start();
        Animate(new AnimationEventArgs("startBurning"));
    }

    #region damageEffects

    /// <summary>
    /// Лавового голема нельзя поджечь... можно только высушить
    /// </summary>
    protected override void BecomeBurning(float _time)
    {
        if (GetBuff("FrozenProcess") != null)
        {
            //Если персонажа подожгли, когда он был заморожен, то он отмараживается и не получает никакого урона от огня, так как считаем, что всё тепло ушло на разморозку
            StopFrozen();
            return;
        }
        if (GetBuff("FrozenWet") != null)
        {
            //Если персонажа подожгли, когда он был промокшим, то он высыхает
            StopWet();
            return;
        }
    }

    /// <summary>
    /// Процесс промокшести
    /// </summary>
    /// <param name="_time">Длительность процесса</param>
    /// <returns></returns>
    protected override IEnumerator WetProcess(float _time)
    {
        AddBuff(new BuffClass("WetProcess", Time.fixedTime, _time));
        attackParametres.damage *= wetDamageCoof;
        Animate(new AnimationEventArgs("spawnEffect", "SteamCloud", 0));
        Animate(new AnimationEventArgs("stopBurning"));
        Animate(new AnimationEventArgs("startWet"));
        yield return new WaitForSeconds(_time);
        attackParametres.damage /= wetDamageCoof;
        Animate(new AnimationEventArgs("stopWet"));
        Animate(new AnimationEventArgs("startBurning"));
        RemoveBuff("WetProcess");
    }

    /// <summary>
    /// Высушиться
    /// </summary>
    protected override void StopWet()
    {
        if (GetBuff("WetProcess") == null)
            return;
        StopCoroutine("WetProcess");
        attackParametres.damage /= wetDamageCoof;
        RemoveBuff("WetProcess");
        Animate(new AnimationEventArgs("stopWet"));
        Animate(new AnimationEventArgs("startBurning"));
    }

    #endregion //damageEffects

}
