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

    public void SpawnEffect(string _effectName)
    {
        CharacterEffect _effect = effects.Find(x => (x.effectName == _effectName));
        if (_effect != null)
        {
            GameObject particle = Instantiate(_effect.particle) as GameObject;
            //particle.transform.parent = transform;
            particle.transform.position = new Vector3(_effect.effectPosition.x * Mathf.Sign(transform.lossyScale.x),_effect.effectPosition.y,0f)+transform.position;
            Destroy(particle,_effect.lifeTime);
        }
    }

    /// <summary>
    /// Эффект падения
    /// </summary>
    public virtual void FallEffect()
    {
        StartCoroutine(FallProcess());
    }

    /// <summary>
    /// Процесс падения
    /// </summary>
    protected virtual IEnumerator FallProcess()
    {
        Vector3 pos = transform.localPosition, scal = transform.localScale;
        transform.localPosition = new Vector2(pos.x, pos.y - .015f);
        transform.localScale = new Vector2(scal.x, scal.y * .8f);
        yield return new WaitForSeconds(.1f);
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
}