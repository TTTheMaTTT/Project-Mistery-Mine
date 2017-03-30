using UnityEngine;
using System.Collections;

/// <summary>
/// Платформа, что может появляться, либо исчезать
/// </summary>
public class GhostPlatform : MonoBehaviour, IMechanism
{

    #region fields 

    protected Animator anim;

    #endregion fields

    #region parametres

    [SerializeField]
    protected bool activated = true;

    [SerializeField]
    [HideInInspector]
    protected int id;

    #endregion //parametres

    public void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim)
        {
            StopAllCoroutines();
            StartCoroutine(AppearProcess());
            anim.Play(activated ? "Appear" : "Disappear");
        }
    }

    /// <summary>
    /// Активировать механизм
    /// </summary>
    public void ActivateMechanism()
    {
        activated = !activated;
        StopAllCoroutines();
        StartCoroutine(AppearProcess());
    }

    IEnumerator AppearProcess()
    {
        yield return new WaitForSeconds(.1f);
        if (anim)
        {
            anim.Play(activated ? "Appear" : "Disappear");
        }
    }

        /// <summary>
    /// Вернуть id
    /// </summary>
    public int GetID()
    {
        return id;
    }

    /// <summary>
    /// Выставить id объекту
    /// </summary>
    public void SetID(int _id)
    {
        id = _id;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif //UNITY_EDITOR
    }

    /// <summary>
    /// Загрузить данные о механизме
    /// </summary>
    public void SetData(InterObjData _intObjData)
    {
        MechData mData = (MechData)_intObjData;
        if (mData != null)
        {
            activated = mData.activated;
            StopAllCoroutines();
            StartCoroutine(AppearProcess());
        }
    }

    /// <summary>
    /// Сохранить данные о механизме
    /// </summary>
    public InterObjData GetData()
    {
        MechData mData = new MechData(id, activated,transform.position, gameObject.name);
        return mData;
    }

}