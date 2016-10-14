using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuScript : MonoBehaviour
{

    public void Awake()
    {
        SpecialFunctions.PlayGame();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("cave_lvl1");
    }
}
