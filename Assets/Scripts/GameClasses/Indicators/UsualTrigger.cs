using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Триггер, который хранит информацию о том, находится ли в нём игрок или нет
/// </summary>
public class UsualTrigger : MonoBehaviour
{

    public bool playerInside = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == SpecialFunctions.Player)
            playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == SpecialFunctions.Player)
            playerInside = false;
    }

}
