using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Компонент, управляющий интро
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class IntroScript : MonoBehaviour
{

    #region fields

    public MovieTexture movie;
    private AudioSource aSource;

    #endregion //fields

    void Start()
    {
        GetComponent<RawImage>().texture = movie as MovieTexture;
        aSource = GetComponent<AudioSource>();
        aSource.clip = movie.audioClip;
        StartCoroutine(BeginIntroProcess());
    }

    void Update()
    {
        if (InputCollection.instance.GetButtonDown("Jump"))
            SceneManager.LoadScene("MainMenu");
    }

    IEnumerator BeginIntroProcess()
    {
        yield return new WaitForSecondsRealtime(1f);
        movie.Play();
        aSource.Play();
        StartCoroutine(NextLevelProcess());
    }

    IEnumerator NextLevelProcess()
    {
        yield return new WaitForSecondsRealtime(movie.duration);
        SceneManager.LoadScene("MainMenu");
    }

}
