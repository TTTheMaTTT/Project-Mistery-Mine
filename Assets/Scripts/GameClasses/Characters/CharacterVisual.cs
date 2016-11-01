using UnityEngine;
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

    #endregion //fields

    #region parametres

    protected int employment = maxEmployment;

    #endregion //parametres

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        anim = GetComponent<Animator>();
        effectSystem = GetComponent<CharacterEffectSystem>();
        employment = maxEmployment;
        FormDictionaries();
    }

    /// <summary>
    /// Сформировать словари анимационных функций
    /// </summary>
    protected virtual void FormDictionaries()
    {
        visualFunctions = new Dictionary<string, AnimatorDelegate>();
        visualFunctions.Add("hitted", Hitted);
        visualFunctions.Add("death", Death);
    }

    /// <summary>
    /// Анимировать получение урона
    /// </summary>
    protected virtual void Hitted(string id, int argument)
    {
        StopAllCoroutines();
        employment = maxEmployment;
        anim.Play("Hitted");
        StartCoroutine(VisualRoutine(5, hittedTime));
    }

    /// <summary>
    /// Визуализировать смерть
    /// </summary>
    protected virtual void Death(string id, int argument)
    {
        if (effectSystem != null)
            effectSystem.SpawnEffect("death dust");
    }

    protected virtual IEnumerator VisualRoutine(int _employment, float _time)
    {
        employment = Mathf.Clamp(employment - _employment, 0, maxEmployment);
        yield return new WaitForSeconds(_time);
        employment = Mathf.Clamp(employment + _employment, 0, maxEmployment);
    }

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

    #endregion //eventHandlers

}
