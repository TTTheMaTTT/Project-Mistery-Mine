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

    #endregion //parametres

    public void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim)
        {
            anim.Play(activated ? "Appear" : "Disappear");
        }
    }

    /// <summary>
    /// Активировать механизм
    /// </summary>
    public void ActivateMechanism()
    {
        activated = !activated;
        if (anim)
        {
            anim.Play(activated ? "Appear" : "Disappear");
        }
    }
}
