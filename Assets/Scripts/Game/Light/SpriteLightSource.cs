using UnityEngine;
using System.Collections;

/// <summary>
/// Источник света
/// </summary>
public class SpriteLightSource : MonoBehaviour
{
    //Внести в список источников света
    public void OnEnable()
    {
        if (SpriteLightKit.lightSources.Contains(this))
            SpriteLightKit.lightSources.Add(this);
    }

    //Внести в список источников света
    public void Awake()
    {
        if (SpriteLightKit.lightSources.Contains(this))
            SpriteLightKit.lightSources.Add(this);
    }

    //Перестать учитывать этот источник света
    public void OnDestroy()
    {
        SpriteLightKit.lightSources.Remove(this);
    }

    public Material material { get { return GetComponent<SpriteRenderer>().material; } }

}

