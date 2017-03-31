﻿using UnityEngine;
using System.Collections;

/// <summary>
/// Триггер смерти
/// </summary>
public class DeathTrigger : MonoBehaviour
{
    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("player"))
        {
            SpecialFunctions.Player.GetComponent<HeroController>().TakeDamage(new HitParametres(1000f, DamageType.Physical, 200), true);
        }
    }
}
