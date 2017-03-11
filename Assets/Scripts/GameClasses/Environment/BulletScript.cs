using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, управляющий снарядами
/// </summary>
public class BulletScript : MonoBehaviour
{

    #region consts

    protected const string groundName = "ground";

    #endregion //consts

    #region fields

    protected HitBoxController hitBox;

    #endregion //fields

    public virtual void Awake()
    {
        hitBox = GetComponentInChildren<HitBoxController>();
        hitBox.AttackEventHandler += HandleAttackProcess;
    }

    /// <summary>
    /// Проверка на столкновение с землёй
    /// </summary>
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (LayerMask.LayerToName(other.gameObject.layer) == groundName)
            Destroy(gameObject);
    }


    #region events

    /// <summary>
    ///  Обработка события "произошла атака" (проверка на столкновение с целью)
    /// </summary>
    protected void HandleAttackProcess(object sender, HitEventArgs e)
    {
        Destroy(gameObject);
    }

    #endregion //events

}
