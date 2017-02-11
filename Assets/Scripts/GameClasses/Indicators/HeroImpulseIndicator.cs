using UnityEngine;
using System.Collections;

/// <summary>
/// Индикатор, определяющий, находится ли герой рядом, и если находится, то отталкивает его
/// </summary>
public class HeroImpulseIndicator : MonoBehaviour
{

    protected float sqDistance = .01f;
    protected float force = 50f;

    protected Transform hero;
    protected Transform trans;

    protected void Awake()
    {
        trans = transform;
        hero = SpecialFunctions.Player.transform;
    }

    protected void Update()
    {
        Vector2 direction = hero.position - trans.position;
        if (Vector2.SqrMagnitude(direction) < sqDistance)
        {
            hero.GetComponent<Rigidbody2D>().AddForce(force * direction.normalized);
        }

    }


}
