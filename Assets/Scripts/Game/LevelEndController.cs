using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, управляющий объектом, что завершает уровень
/// </summary>
public class LevelEndController : MonoBehaviour
{

    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<HeroController>() != null)
        {
            GameController.GoToTheNextLevel();
        }
    }
	

}
