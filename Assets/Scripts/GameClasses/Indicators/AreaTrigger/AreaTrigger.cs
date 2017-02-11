using UnityEngine;
using System.Collections;

/// <summary>
/// Триггер, активирующийся при его вхождении в область AreaTriggerSearcher. При этом он вызывает функции, в которые в него внёс родительский объект 
/// (Эти функции связаны с переключением между активным и оптимизированным состоянием родительского объекта). Используется для оптимизации.
/// </summary>
public class AreaTrigger : MonoBehaviour
{

    #region consts

    protected const float distanceToHero= 3f;

    #endregion //consts

    #region delegates

    public delegate void TriggerFunctionDelegate();

    #endregion //delegates

    #region fields

    public TriggerFunctionDelegate triggerFunctionIn, triggerFunctionOut;

    protected GameObject triggerHolder;//Какой объект использует этот триггер
    public GameObject TriggerHolder { get { return triggerHolder; } set { triggerHolder = value; } }

    #endregion //fields

    /// <summary>
    /// Инициализировать этот триггер
    /// </summary>
    public virtual void InitializeAreaTrigger()
    {
        if (Vector2.SqrMagnitude(transform.position - SpecialFunctions.Player.transform.position) <= distanceToHero*distanceToHero)
            triggerFunctionIn.Invoke();
        else
            triggerFunctionOut.Invoke();
    }

    /// <summary>
    /// Перевести объекты, привязанные к этому триггеру, в активное полнофункциональное состояние
    /// </summary>
    public virtual void TriggerIn()
    {
        triggerFunctionIn.Invoke();
    }

    /// <summary>
    /// Перевести объекты, привязанные к этому триггеру, в оптимизированное состояние
    /// </summary>
    public virtual void TriggerOut()
    {
        triggerFunctionOut.Invoke();
    }

}
