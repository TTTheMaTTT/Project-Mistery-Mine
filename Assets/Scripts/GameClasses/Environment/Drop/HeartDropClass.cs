using UnityEngine;
using System.Collections;

public class HeartDropClass : DropClass
{
    public override void OnTriggerEnter2D(Collider2D other)
    {
        HeroController hero = SpecialFunctions.Player.GetComponent<HeroController>();
        if (hero.Health<hero.MaxHealth && dropped)
            base.OnTriggerEnter2D(other);
    }

}
