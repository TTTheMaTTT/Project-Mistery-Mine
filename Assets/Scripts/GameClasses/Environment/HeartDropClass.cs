using UnityEngine;
using System.Collections;

public class HeartDropClass : DropClass
{
    public override void OnTriggerEnter2D(Collider2D other)
    {
        HeroController hero = SpecialFunctions.player.GetComponent<HeroController>();
        if (hero.Health<hero.MaxHealth)
            base.OnTriggerEnter2D(other);
    }
}
