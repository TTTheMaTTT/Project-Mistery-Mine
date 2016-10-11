using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Скрипт, управляющий объектом, что завершает уровень
/// </summary>
public class LevelEndController : MonoBehaviour
{

    #region parametres

    public string nextLevelName;

    #endregion //parametres

    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<HeroController>() != null)
        {
            SceneManager.LoadScene(nextLevelName);
        }
    }
	

}
