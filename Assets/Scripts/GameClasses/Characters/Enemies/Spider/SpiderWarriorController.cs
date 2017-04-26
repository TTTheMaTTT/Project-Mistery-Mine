using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, описывающий пауков-воинов, что служат паучьей королеве
/// </summary>
public class SpiderWarriorController : SpiderController
{

    #region consts

    protected const string takeDamageMessage = "Не стоит испытывать благосклонность Королевы на прочность";

    #endregion //consts

    #region parametres

    protected int takeDamageTimes = 0;//Сколько раз уже паук получал урон

    public override LoyaltyEnum Loyalty
    {
        get
        {
            return base.Loyalty;
        }

        set
        {
            base.Loyalty = value;
            if (value == LoyaltyEnum.neutral)
            {
                takeDamageTimes = 0;
                Health = maxHealth;
            }
        }
    }

    /// <summary>
    /// Получить информацию об атакующем
    /// </summary>
    /// <param name="attackerInfo"></param>
    public override void TakeAttackerInformation(AttackerClass attackerInfo)
    {
        if (loyalty == LoyaltyEnum.neutral)
        {
            takeDamageTimes ++;
            if (takeDamageTimes >= 2)
                base.TakeAttackerInformation(attackerInfo);
            else
                SpecialFunctions.SetText(takeDamageMessage, 2.5f);
        }
        else
            base.TakeAttackerInformation(attackerInfo); 
    }

    #endregion //parametres

}
