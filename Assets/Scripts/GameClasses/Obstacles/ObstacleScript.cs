using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, управляющий дамажащими препятствиями
/// </summary>
public class ObstacleScript : MonoBehaviour
{

    #region fields

    protected HitBox hitBox;
    [SerializeField] protected List<string> enemies = new List<string>();

    [SerializeField]
    protected HitClass hitData;

    #endregion //fields

    void Start()
    {
        hitBox = GetComponent<HitBox>();
        hitBox.SetEnemies(enemies);
        hitBox.SetHitBox(new HitClass(hitData.damage,-1f,hitData.hitSize,transform.position,0f));
    }

}
