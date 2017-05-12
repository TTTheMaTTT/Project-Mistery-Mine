﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, отвечающий за анимирование персонажа
/// </summary>
public class CharacterVisual : MonoBehaviour
{

    #region consts

    protected const int maxEmployment = 10;
    protected const float hittedTime = .1f;
    protected const float underwaterCoof = .8f;
    protected const float invulBlinkTime = .2f;

    #endregion //consts

    #region delegates

    protected delegate void AnimatorDelegate(string id, int argument);

    #endregion //delegates

    #region dictionaries

    protected Dictionary<string, AnimatorDelegate> visualFunctions = new Dictionary<string, AnimatorDelegate>();

    #endregion //dictionaries

    #region fields

    protected Animator anim;
    protected CharacterEffectSystem effectSystem;//Система событий, воспроизводящая эффекты при проигрывании анимаций
    protected SpriteRenderer sRenderer;

    [SerializeField] protected List<AudioData> audioInfo=new List<AudioData>();

    #endregion //fields

    #region parametres

    protected int employment = maxEmployment;

    protected Color color1 = Color.white;//Цвета, используемые для смешения в результатирующий
    protected Color color2 = Color.white;//цвет, который потом используется для отображения эффектов на персонаже
    protected float colorCoof1 = .5f, colorCoof2 = .5f;//Коэффициенты смешения
    protected Color silhouetteColor = new Color(0.047f, 0.592f, 0.815f, 1f);//Основной цвет, используемый для отображения персонажа под водой

    protected AudioSource soundSource;//Источник звуков анимированных анимаций
    protected AudioSource soundSource2;

    #region effectColors

    protected virtual Color burningColor { get { return new Color(1f, .47f, 0f); } }
    protected virtual float burningCoof { get { return .6f; } }
    protected virtual Color coldColor { get { return new Color(.41f, 1f, 1f); } }
    protected virtual float coldCoof { get { return .5f; } }
    protected virtual Color poisonedColor { get { return new Color(.192f, .682f, 0f); } }
    protected virtual float poisonedCoof { get { return .5f; } }
    protected virtual Color wetColor { get { return new Color(.494f, .494f, .494f); } }
    protected virtual float wetCoof { get { return .5f; } }
    protected virtual Color frozenColor { get { return new Color(.34f, .86f, 1f); } }
    protected virtual float frozenCoof { get { return 1f; } }

    #endregion //effectColors

    #endregion //parametres

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        anim = GetComponent<Animator>();
        effectSystem = GetComponent<CharacterEffectSystem>();
        if (effectSystem != null)
            effectSystem.Initialize();
        employment = maxEmployment;
        FormDictionaries();
        sRenderer = GetComponent<SpriteRenderer>();
        SetDefaultColor();
        AudioSource[] aSources = GetComponents<AudioSource>();
        if (aSources.Length < 2)
        {
            for (int i = aSources.Length; i < 2; i++)
                gameObject.AddComponent<AudioSource>();
            aSources = GetComponents<AudioSource>();
        }
        for (int i = 0; i < 2; i++)
        {
            aSources[i].volume = PlayerPrefs.GetFloat("SoundVolume");
            aSources[i].spatialBlend = 1f;
        }
        soundSource = aSources[0];
        soundSource2 = aSources[1];

    }

    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected virtual void FormDictionaries()
    {
        visualFunctions = new Dictionary<string, AnimatorDelegate>();
        visualFunctions.Add("hitted", Hitted);
        visualFunctions.Add("stop", StopVisualRoutine);
        visualFunctions.Add("spawnEffect", SpawnEffect);
        visualFunctions.Add("setDefaultColor", SetDefaultColor);
        visualFunctions.Add("startBurning", StartBurning);
        visualFunctions.Add("startStun", StartStun);
        visualFunctions.Add("startPoison", StartPoison);
        visualFunctions.Add("startCold", StartCold);
        visualFunctions.Add("startFrozen", StartFrozen);
        visualFunctions.Add("startWet", StartWet);
        visualFunctions.Add("startAlly", StartAlly);
        visualFunctions.Add("stopBurning", StopBurning);
        visualFunctions.Add("stopStun", StopStun);
        visualFunctions.Add("stopPoison", StopPoison);
        visualFunctions.Add("stopCold", StopCold);
        visualFunctions.Add("stopFrozen", StopFrozen);
        visualFunctions.Add("stopWet", StopWet);
        visualFunctions.Add("stopAlly", StopAlly);
        visualFunctions.Add("death", Death);
        visualFunctions.Add("playSound", PlaySoundWithName);
    }

    /// <summary>
    /// Проиграть звук из коллекции
    /// </summary>
    public virtual void PlaySoundWithName(string id, int argument)
    {
        if (argument == 0)
            PlaySound(id);
        else if (argument == 1)
            PlayAdditionalSound(id);
    }

    /// <summary>
    /// Проиграть звук из коллекции
    /// </summary>
    public virtual void PlayAdditionalSound(string _audioName)
    {
        AudioData _aData = audioInfo.Find(x => x.audioName == _audioName);
        if (_aData == null)
            return;
        AudioClip _clip = _aData.audios[UnityEngine.Random.Range(0, _aData.audios.Count)];
        soundSource2.clip = _clip;
        soundSource2.PlayOneShot(_clip, _aData.volume);
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
        soundSource.PlayOneShot(_clip,_aData.volume);
    }

    /*
    void StartVolumeProcess(float _time)
    {
        StopCoroutine("VolumeProcess");
        StartCoroutine("VolumeProcess", _time);
    }

    /// <summary>
    /// Процесс, после которого возвращается прежний уровень громкости
    /// </summary>
    protected IEnumerator VolumeProcess(float _time)
    {
        yield return new WaitForSeconds(_time);
        soundSource.volume = SpecialFunctions.soundVolume;
    }
    */

    /// <summary>
    /// Аудио начинаются играть зацикленно
    /// </summary>
    public virtual void MakeSoundCycle(bool _cycle)
    {
        if (soundSource!=null)
        soundSource.loop = _cycle;
    }

    /// <summary>
    /// Анимировать получение урона
    /// </summary>
    protected virtual void Hitted(string id, int argument)
    {
        if (argument == 0)
        {
            StopCoroutine("VisualRoutine");
            if (employment > 0)//Если же персонаж полностью занят, значит он, скорее всего заморожен, поэтому нельзя прерывать этот процесс вызыванием других анимаций.
            {
                employment = maxEmployment;
                StartVisualRoutine(5, hittedTime);
            }
        }
        Blink();
        PlayAdditionalSound("Damage");
    }

    /// <summary>
    /// Визуализировать смерть
    /// </summary>
    protected virtual void Death(string id, int argument)
    {
        if (id == "animation")
            anim.Play("Death");
        else if (effectSystem != null)
        {
            switch (id)
            {
                case "fire":
                    {
                        effectSystem.SpawnEffect("AshDrop");
                        break;
                    }
                case "ice":
                    {
                        effectSystem.SpawnEffect("IceDrop");
                        break;
                    }
                default:
                    {
                        effectSystem.SpawnEffect("deathDust");
                        break;
                    }
            }
        }
        soundSource.Stop();
        soundSource2.Stop();
        PlayAdditionalSound("Death");
    }

    /// <summary>
    /// Процесс, который позволяет некоторым анимациям не прерываться другими
    /// </summary>
    /// <param name="_employment">Насколько увеличивается занятость</param>
    /// <param name="_time">длительность визуалього процесса</param>
    protected virtual IEnumerator VisualRoutine(int _employment, float _time)
    {
        employment = Mathf.Clamp(employment - _employment, 0, maxEmployment);
        yield return new WaitForSeconds(_time);
        employment = Mathf.Clamp(employment + _employment, 0, maxEmployment);
    }

    /// <summary>
    /// Функция, вызывающая VisualRoutine и контролирующая его единственность среди всех Routine, запущенных для игрового объекта
    /// </summary>
    /// <param name="_employment">Насколько занятым становится персонаж при отображении анимации</param>
    /// <param name="_time">Как долго длится процесс</param>
    protected virtual void StartVisualRoutine(int _employment, float _time)
    {
        StopCoroutine("VisualRoutine");
        employment = maxEmployment;
        IEnumerator vRoutine = VisualRoutine(_employment, _time);
        StartCoroutine(vRoutine);
    }

    protected virtual void StopVisualRoutine(string id, int argument)
    {
        StopCoroutine("VisualRoutine");
        employment = maxEmployment;
    }

    #region battleEffects

    /// <summary>
    /// Визуализировать оглушение
    /// </summary>
    protected virtual void StartStun(string id, int argument)
    {
        effectSystem.SpawnEffect("StunStars");
    }

    /// <summary>
    /// Завершить стан
    /// </summary>
    protected virtual void StopStun(string id, int argument)
    {
        effectSystem.RemoveEffect("StunStars");
    }

    /// <summary>
    /// Визуализировать горение
    /// </summary>
    protected virtual void StartBurning(string id, int argument)
    {
        AddColor(burningColor, burningCoof);
        effectSystem.SpawnEffect("BurnFire");
    }

    /// <summary>
    /// Прекратить горение
    /// </summary>
    protected virtual void StopBurning(string id, int argument)
    {
        RemoveColor(burningColor);
        effectSystem.RemoveEffect("BurnFire");
    }

    /// <summary>
    /// Создать эффект с данным названием
    /// </summary>
    /// <param name="id">название эффекта</param>
    protected virtual void SpawnEffect(string id, int argument)
    {
        effectSystem.SpawnEffect(id);
    }

    /// <summary>
    /// Визуализировать замерзание
    /// </summary>
    protected virtual void StartCold(string id, int argument)
    {
        AddColor(coldColor, coldCoof);
    }

    /// <summary>
    /// Прекратить замерзание
    /// </summary>
    protected virtual void StopCold(string id, int argument)
    {
        RemoveColor(coldColor);
    }

    /// <summary>
    /// Визуализировать отравление
    /// </summary>
    protected virtual void StartPoison(string id, int argument)
    {
        AddColor(poisonedColor, poisonedCoof);
        effectSystem.SpawnEffect("Poison");
    }

    /// <summary>
    /// Прекратить отравление
    /// </summary>
    protected virtual void StopPoison(string id, int argument)
    {
        RemoveColor(poisonedColor);
        effectSystem.RemoveEffect("Poison");
    }

    /// <summary>
    /// Визуализировать промокшесть
    /// </summary>
    protected virtual void StartWet(string id, int argument)
    {
        AddColor(wetColor, wetCoof);
        effectSystem.SpawnEffect("WaterDrops");
    }

    /// <summary>
    /// Прекратить промокшесть
    /// </summary>
    protected virtual void StopWet(string id, int argument)
    {
        RemoveColor(wetColor);
        effectSystem.RemoveEffect("WaterDrops");
    }

    /// <summary>
    /// Начать обледенение
    /// </summary>
    protected virtual void StartFrozen(string id, int argument)
    {
        AddColor(frozenColor, frozenCoof);
        StopCoroutine("VisualRoutine");
        employment = 0;
        anim.speed = 0f;
    }

    /// <summary>
    /// Прекратить обледенение
    /// </summary>
    protected virtual void StopFrozen(string id, int argument)
    {
        RemoveColor(frozenColor);
        StopCoroutine("VisualRoutine");
        employment = maxEmployment;
        anim.speed = 1f;
    }

    #endregion //battleEffects

    /// <summary>
    /// Визуально отобразить, что персонаж стал союзником
    /// </summary>
    public virtual void StartAlly(string id, int argument)
    {
        //effectSystem.SpawnEffect("AllyAura");
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        sRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_Outline", 1f);
        mpb.SetColor("_OutlineColor", Color.green);
        sRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>
    /// Визуально отобразить, что персона перестал быть союзником
    /// </summary>
    public virtual void StopAlly(string id, int argument)
    {
        //effectSystem.RemoveEffect("AllyAura");
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        sRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_Outline", 0f);
        sRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>
    /// Установить новый цвет персонажу
    /// </summary>
    /// <param name="_col">Новый цвет, который будет использоваться для смешивания</param>
    /// <param name="_colCoof">Коэффициент смешения</param>
    public virtual void AddColor(Color _col, float _colCoof)
    {
        Color oldColor = color1;
        color1 = color2;
        colorCoof1 = colorCoof2;
        color2 = _col;
        colorCoof2 = _colCoof;
        if (oldColor==Color.white)
        {
            color1 = _col;
            colorCoof1 = _colCoof;
        }
        SetColor(color1 * colorCoof1 + color2 * colorCoof2);
    }

    /// <summary>
    /// Установить дефолтный белый цвет персонажу
    /// </summary>
    public virtual void SetDefaultColor()
    {
        color1 = Color.white;
        colorCoof1 = .5f;
        color2 = Color.white;
        colorCoof2 = .5f;
        SetColor(color1 * colorCoof1 + color2 * colorCoof2);
    }

    protected virtual void SetDefaultColor(string id, int argument)
    {
        SetDefaultColor();
    }

    /// <summary>
    /// Убрать один из цветов смешивания
    /// </summary>
    /// <param name="_col">Убираемый цвет</param>
    public virtual void RemoveColor(Color _col)
    {
        if (color1 == color2 && color1 == _col)
            SetDefaultColor();
        else
        {
            if (color1 == _col)
            {
                color1 = color2;
                colorCoof1 = colorCoof2;
            }
            else if (color2 == _col)
            {
                color2 = color1;
                colorCoof2 = colorCoof1;
            }
            SetColor(color1 * colorCoof1 + color2 * colorCoof2);
        }
    }

    /// <summary>
    /// Функция мигания (от получения урона)
    /// </summary>
    public virtual void Blink(bool blinkProcess=true)
    {
        SetColor(color1 * colorCoof1 + color2 * colorCoof2);
        StopCoroutine("BlinkProcess");
        StartCoroutine("BlinkProcess");
    }

    /// <summary>
    /// Процесс мигания
    /// </summary>
    protected virtual IEnumerator BlinkProcess()
    {
        Color oldColor = color1 * colorCoof1 + color2 * colorCoof2;
        SetColor(Color.white * 2f);
        yield return new WaitForSeconds(invulBlinkTime);
        SetColor(oldColor);
    }

    /// <summary>
    /// Установить нужный цвет персонажу
    /// </summary>
    /// <param name="_col">новый цвет</param>
    protected virtual void SetColor(Color _col)
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        sRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_MixedColor", _col);
        mpb.SetColor("_SilhouetteMixedColor", (1f - underwaterCoof) * _col + underwaterCoof * silhouetteColor);
        sRenderer.SetPropertyBlock(mpb);
    }

    #region sounds

    /// <summary>
    /// Проиграть выбранный звук
    /// </summary>
    public virtual void MakeSound(AudioClip sound)
    {
        soundSource.clip = sound;
        soundSource.Play();
    }

    /// <summary>
    /// Обработать событие "Поменялась громкость звука"
    /// </summary>
    protected virtual void HandleSoundLevelChange(object sender, SoundChangesEventArgs e)
    {
        soundSource.volume = e.SoundVolume;
        soundSource2.volume = e.SoundVolume;
    }

    #endregion //sounds

    #region eventHandlers

    /// <summary>
    /// Обработчик запроса на анимирование
    /// </summary>
    public void AnimateIt(object sender, AnimationEventArgs e)
    {
        if (visualFunctions.ContainsKey(e.AnimationType))
        {
            visualFunctions[e.AnimationType].Invoke(e.ID, e.Argument);
        }
    }

    /// <summary>
    /// Обработчик паузы
    /// </summary>
    protected virtual void HandlePause(object sender, PauseEventArgs e)
    {
        if (soundSource == null)
            return;
        if (e.Paused)
        {
            soundSource.Pause();
            soundSource2.Pause();
        }
        else
        {
            soundSource.UnPause();
            soundSource2.UnPause();
        }
    }

    #endregion //eventHandlers

}

[System.Serializable]
public class AudioData
{
    public string audioName;
    public List<AudioClip> audios = new List<AudioClip>();
    [Range(0f,1f)]
    public float volume = 1f;//Громкость данных звуков

}