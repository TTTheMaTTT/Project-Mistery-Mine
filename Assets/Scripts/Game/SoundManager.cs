using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{

    #region fields

    private AudioSource ambientSource, musicSource, soundSource;//Источники звуков окружающего мира и музыки, а также источник игровых звуков
    [SerializeField]
    private List<AudioClip> ambientClips = new List<AudioClip>(), musicClips = new List<AudioClip>(), soundClips = new List<AudioClip>();

    #endregion //fields

    void Start()
    {
        AudioSource[] audioSources = GameObject.FindGameObjectWithTag("MainCamera").GetComponents<AudioSource>();
        if (audioSources.Length >= 1)
        {
            musicSource = audioSources[0];
            musicSource.volume = PlayerPrefs.GetFloat("MusicVolume");
            if (musicClips.Count > 0)
            {
                musicSource.clip = musicClips[0];
                musicSource.Play();
            }
        }
        if (audioSources.Length >= 2)
        {
            ambientSource = audioSources[1];
            ambientSource.volume = PlayerPrefs.GetFloat("SoundVolume");
            SettingsScript.soundEventHandler += HandleSoundVolumeChange;
            if (ambientClips.Count > 0)
            {
                ambientSource.clip = ambientClips[0];
                ambientSource.Play();
            }
        }
        if (audioSources.Length >= 3)
        {
            soundSource = audioSources[2];
            soundSource.volume = PlayerPrefs.GetFloat("SoundVolume");
        }
    }

    #region musicAndSounds

    /// <summary>
    /// Поменять громкость музыки
    /// </summary>
    public void ChangeMusicVolume(float _volume)
    {
        if (musicSource != null)
            musicSource.volume = _volume;
    }

    /// <summary>
    /// Обработка события "Поменялась громкость звуков"
    /// </summary>
    private void HandleSoundVolumeChange(object sender, SoundChangesEventArgs e)
    {
        if (soundSource != null)
            soundSource.volume = e.SoundVolume;
        if (ambientSource != null)
            ambientSource.volume = e.SoundVolume;
    }

    /// <summary>
    /// Начать проигрывать звуки окружающего мира, которые имеют заданное название
    /// </summary>
    public void ChangeAmbientSound(string clipName)
    {
        if (ambientSource == null)
            return;
        ambientSource.Stop();
        if (clipName != "")
        {
            AudioClip _clip = ambientClips.Find(x => x.name.Contains(clipName));
            if (_clip != null)
            {
                ambientSource.clip = _clip;
                ambientSource.Play();
            }
        }
    }

    /// <summary>
    /// Остановить музыку
    /// </summary>
    public void StopMusic()
    {
        if (musicSource == null)
            return;
        musicSource.Stop();
    }

    /// <summary>
    /// Начать проигрывать музыкальную тему с заданным названием
    /// </summary>
    public void ChangeMusicTheme(string clipName)
    {
        if (musicSource == null)
            return;
        musicSource.Stop();
        if (clipName != "")
        {
            AudioClip _clip = musicClips.Find(x => x.name.Contains(clipName));
            if (_clip != null)
            {
                musicSource.clip = _clip;
                musicSource.Play();
            }
        }
    }

    /// <summary>
    /// Проиграть игровой звук с заданным названием
    /// </summary>
    /// <param name="soundName"></param>
    public void PlaySound(string soundName)
    {
        if (soundSource == null)
            return;
        if (soundName != "")
        {
            AudioClip _clip = soundClips.Find(x => x.name.Contains(soundName));
            if (_clip != null)
            {
                soundSource.clip = _clip;
                soundSource.Play();
            }
        }
    }

    #endregion //musicAndSounds

}
