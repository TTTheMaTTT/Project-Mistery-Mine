using UnityEngine;
using System.Collections;

public class AngleCalculator : MonoBehaviour
{

    private Transform target;

    public float angle;

	void Start ()
    {
        target = SpecialFunctions.player.transform;
	}
	
	void Update ()
    {
        Vector2 beginDirection = (target.position.x >= transform.position.x ? 1 : -1) * Vector2.right;
        angle = Vector2.Angle(beginDirection, ((Vector2)target.position - (Vector2)transform.position).normalized);
	}
}
