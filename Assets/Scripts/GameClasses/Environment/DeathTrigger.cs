using UnityEngine;
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
            SpecialFunctions.player.GetComponent<HeroController>().TakeDamage(1000f);
        }
    }
}
