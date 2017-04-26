using UnityEngine;
using System.Collections;

public class HeartDropClass : DropClass
{

    #region consts

    protected const float heartPickTime = 1f;//Время автоподбора сердца

    #endregion //consts

    public override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag != "player")
            return;
        HeroController hero = SpecialFunctions.Player.GetComponent<HeroController>();
        if (hero.Health < hero.MaxHealth)
        {
            if (dropped)
                base.OnTriggerEnter2D(other);
            else
                StartCoroutine("HeartPickProcess");
        }
    }

    public virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag=="player")
            StopCoroutine("HeartPickProcess");
    }

    /// <summary>
    /// Провзаимодействовать с сердцем
    /// </summary>
    public override void Interact()
    {
        base.Interact();
        StopCoroutine("HeartPickProcess");
    }

    /// <summary>
    /// Процесс подбора сердечка
    /// </summary>
    IEnumerator HeartPickProcess()
    {
        yield return new WaitForSeconds(heartPickTime);
        HeroController hero = SpecialFunctions.Player.GetComponent<HeroController>();
        if (hero.Health < hero.MaxHealth)
            Interact();
    }

}
