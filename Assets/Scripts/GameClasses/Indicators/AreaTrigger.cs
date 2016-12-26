using UnityEngine;
using System.Collections;

/// <summary>
/// Триггер, активирующийся при вхождении игрока в него. При этом он вызывает функции, в которые в него внёс родительский объект. Используется для оптимизации.
/// </summary>
public class AreaTrigger : MonoBehaviour
{

    #region delegates

    public delegate void TriggerFunctionDelegate();

    #endregion //delegates

    #region fields

    public TriggerFunctionDelegate triggerFunctionIn, triggerFunctionOut;

    #endregion //fields

    protected virtual void Start()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (!col)
            return;
        if (Vector2.SqrMagnitude(transform.position - SpecialFunctions.player.transform.position) <= col.radius * col.radius)
            triggerFunctionIn.Invoke();
        else
            triggerFunctionOut.Invoke();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("player"))
        {
            triggerFunctionIn.Invoke();
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("player"))
        {
            triggerFunctionOut.Invoke();
        }
    }

}
