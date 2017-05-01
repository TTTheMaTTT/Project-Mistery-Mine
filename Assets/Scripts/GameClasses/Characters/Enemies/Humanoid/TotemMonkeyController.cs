using UnityEngine;
using System.Collections;

/// <summary>
/// Контроллер тотемного животного - прзрачной обезьянки. Это дружественный персонаж, призываемый героем при эффекте "Тотемное животное"
/// </summary>
public class TotemMonkeyController : HumanoidController
{

    #region consts

    protected const float totemAnimalTime = 20f;//Время жизни обезьянки
    protected const float maxAllyDistance = 2.2f;//Максимальное расстояние до героя, при котором персонаж необязан ещё следовать за героем (то есть может заниматься другими делами)

    #endregion //consts

    #region parametres

    protected override float allyTime{get{return 2f;}}

    #endregion //parametres

    protected override void Initialize()
    {
        base.Initialize();
        AddBuff(new BuffClass("TotemAnimal", Time.fixedTime, totemAnimalTime));
        StartCoroutine(TotemMonkeyDeathProcess());
    }

    /// <summary>
    /// Провести анализ окружающей обстановки
    /// </summary>
    protected override void Analyse()
    {
        base.Analyse();
        Vector2 pos = transform.position;
        if (loyalty != LoyaltyEnum.ally)
            return;
        switch (behavior)
        {
            case BehaviorEnum.agressive:
                {
                    if (Vector2.SqrMagnitude(beginPosition - pos) > maxAllyDistance * maxAllyDistance)
                    {
                        MainTarget = ETarget.zero;
                        GoHome();
                    }
                    break;
                }
            case BehaviorEnum.patrol:
                {
                    if (mainTarget.exists)
                        if (Vector2.SqrMagnitude(beginPosition - pos) > maxAllyDistance * maxAllyDistance)
                        {
                            MainTarget = ETarget.zero;
                            GoHome();
                        }                    
                    
                    break;
                }

            default:
                {
                    break;
                }
        }
    }

    /// <summary>
    /// Анализ окружающей персонажа обстановки, когда он находится в оптимизированном состоянии
    /// </summary>
    protected override void AnalyseOpt()
    {
        Vector2 pos = transform.position;
        base.AnalyseOpt();
        if (behavior == BehaviorEnum.patrol)
        {
            float sqDistance = Vector2.SqrMagnitude(beginPosition - pos);
            if (followAlly)
            {
                if (prevTargetPosition.exists? Vector2.SqrMagnitude(beginPosition - (Vector2)prevTargetPosition) > minCellSqrMagnitude: true)
                {
                    prevTargetPosition = new EVector3(pos);//Динамическое преследование героя-союзника
                    GoHome();
                    StopFollowOptPath();
                    StartCoroutine("ConsiderAllyPathProcess");
                }
            }
        }
    }

    /// <summary>
    /// Смерть персонажа в связи с его выходом из триггера поля боя не происходит
    /// </summary>
    protected override void AreaTriggerExitDeath()
    {
    }

    /// <summary>
    /// Процесс жизни тотемного животного
    /// </summary>
    protected virtual IEnumerator TotemMonkeyDeathProcess()
    {
        yield return new WaitForSeconds(totemAnimalTime);
        Death();
    }

}
