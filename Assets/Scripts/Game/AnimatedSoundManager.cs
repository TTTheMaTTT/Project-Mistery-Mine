using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Компонент, который применяется для озвучивания любого объекта во время его анимации
/// </summary>
public class AnimatedSoundManager : MonoBehaviour
{

    AudioSource soundSource;

    private void Awake()
    {
        soundSource = GetComponent<AudioSource>();
        if (soundSource == null)
            soundSource = gameObject.AddComponent<AudioSource>();
        soundSource.volume = PlayerPrefs.GetFloat("SoundVolume");
        SpecialFunctions.Settings.soundEventHandler += HandleSoundVolumeChange;
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

}
