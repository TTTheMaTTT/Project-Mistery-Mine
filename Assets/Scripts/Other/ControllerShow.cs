using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerShow : MonoBehaviour
{

    public float s;
	
	void Update ()
    {
        s = InputCollection.instance.GetAxis("Horizontal");
	}
}
