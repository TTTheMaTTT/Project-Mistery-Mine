using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Компонент, который применяется для озвучивания любого объекта во время его анимации
/// </summary>
public class AnimatedSoundManager : MonoBehaviour
{

    #region fields

    private AudioSource soundSource;
    [SerializeField]private List<AudioData> audioInfo=new List<AudioData>();

    #endregion //fields

    private void Awake()
    {
        soundSource = GetComponent<AudioSource>();
        if (soundSource == null)
            soundSource = gameObject.AddComponent<AudioSource>();
        soundSource.volume = PlayerPrefs.GetFloat("SoundVolume");
        SettingsScript.soundEventHandler += HandleSoundVolumeChange;
        SettingsScript.pauseEventHandler += HandlePause;
        soundSource.spatialBlend = 1f;
    }

    /// <summary>
    /// Проиграть звук из коллекции
    /// </summary>
    public virtual void PlaySound(string _audioName)
    {
        AudioData _aData = audioInfo.Find(x => x.audioName == _audioName);
        if (_aData == null)
            return;
        AudioClip _clip = _aData.audios[UnityEngine.Random.Range(0, _aData.audios.Count)];
        soundSource.clip = _clip;
        soundSource.loop = false;
        soundSource.PlayOneShot(_clip, _aData.volume);
    }

    public virtual void PlaySoundLoop(string _audioName)
    {
        AudioData _aData = audioInfo.Find(x => x.audioName == _audioName);
        if (_aData == null)
            return;
        AudioClip _clip = _aData.audios[UnityEngine.Random.Range(0, _aData.audios.Count)];
        soundSource.clip = _clip;
        soundSource.loop = true;
        soundSource.clip = _clip;
        soundSource.Play();
        //soundSource.PlayOneShot(_clip, _aData.volume);
    }

    public virtual void StopPlaying()
    {
        soundSource.Stop();
    }

    /// <summary>
    /// Проиграть выбранный звук
    /// </summary>
    public virtual void MakeSound(AudioClip sound)
    {
        soundSource.clip = sound;
        soundSource.Play();
    }

    /// <summary>
    /// Обработка события - "Громкость звуков изменилась"
    /// </summary>
    private void HandleSoundVolumeChange(object sender, SoundChangesEventArgs e)
    {
        soundSource.volume = e.SoundVolume;
    }

    /// <summary>
    /// Обработчик паузы
    /// </summary>
    protected virtual void HandlePause(object sender, PauseEventArgs e)
    {
        if (soundSource == null)
            return;
        if (e.Paused)
            soundSource.Pause();
        else
            soundSource.UnPause();
    }

}
