using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuScript : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("BattleSystem");
    }
}
