using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, ответсвенный за визуальные эффекты персонажа
/// </summary>
public class CharacterEffectSystem : MonoBehaviour
{
    #region fields

    [SerializeField]
    protected List<CharacterEffect> effects = new List<CharacterEffect>();//эффекты, что может воспроизводить персонаж

    #endregion //fields

    #region parametres

    protected Vector3 pos, scal;

    #endregion //parametres

    public void Initialize()
    {
        pos = transform.localPosition;
        scal = transform.localScale;
    }

    /// <summary>
    /// Создать игровой объект, представляющий собой реализацию того или иного эффекта
    /// </summary>
    /// <param name="_effectName">название эффекта (или имя реализующего эффект объекта)</param>
    public void SpawnEffect(string _effectName)
    {
        CharacterEffect _effect = effects.Find(x => (x.effectName == _effectName));
        if (_effect != null)
        {
            GameObject particle = Instantiate(_effect.particle) as GameObject;
            //particle.transform.parent = transform;
            particle.transform.position = new Vector3(_effect.effectPosition.x * Mathf.Sign(transform.lossyScale.x),_effect.effectPosition.y,0f)+transform.position;
            if (_effect.child)
                particle.transform.parent = transform;
            if (_effect.lifeTime>0f)
                Destroy(particle,_effect.lifeTime);
        }
    }

    /// <summary>
    /// Убрать эффект, находящийся внутри персонажа
    /// </summary>
    /// <param name="_effectName">название эффекта (или имя реализующего эффект объекта)</param>
    public void RemoveEffect(string _effectName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject _effect = transform.GetChild(i).gameObject;
            if (_effect.name.Contains(_effectName))
            {
                Destroy(_effect);
                break;
            }
        }
    }

    /// <summary>
    /// Эффект падения
    /// </summary>
    public virtual void FallEffect()
    {
        StartCoroutine("FallProcess");
    }

    /// <summary>
    /// Процесс падения
    /// </summary>
    protected virtual IEnumerator FallProcess()
    {
        transform.localPosition = new Vector2(pos.x, pos.y - .015f);
        transform.localScale = new Vector2(scal.x, scal.y * .8f);
        yield return new WaitForSeconds(.1f);
        transform.localPosition = pos;
        transform.localScale = scal;
    }

    /// <summary>
    /// Сброс по всем эффектам
    /// </summary>
    public virtual void ResetEffects()
    {
        StopCoroutine("FallProcess");
        transform.localPosition = pos;
        transform.localScale = scal;
    }

}

/// <summary>
/// Эффект, основанный на ParticleSystem
/// </summary>
[System.Serializable]
public class CharacterEffect
{
    public string effectName;//название эффекта
    public GameObject particle;//Испускаемая частица при воспроизведении эффекта 
    public float lifeTime;//Как долго длится эффект
    public Vector2 effectPosition;//Где располагается объект относительно персонажа
    public bool child = false;//Является ли эффект дочерним объектом по отношению к создаваемому его игровому объекту1
}