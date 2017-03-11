﻿using UnityEngine;
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
    public List<string> Enemies {set { enemies = value; } }

    [SerializeField]
    protected HitParametres hitData;
    public HitParametres HitData { get { return hitData; } set { hitData = value; } }
    
    #endregion //fields

    void Start()
    {
        hitBox = GetComponent<HitBox>();
        hitBox.Immobile = true;
        hitBox.SetEnemies(enemies);
        hitBox.SetHitBox(new HitParametres(hitData.damage,-1f,hitData.hitSize,transform.position,0f, hitData.damageType, hitData.effectChance,hitData.attackPower,0f,0f));
    }

}
