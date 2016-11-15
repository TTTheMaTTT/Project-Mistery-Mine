using UnityEngine;
using System.Collections;

/// <summary>
/// Источник света
/// </summary>
[ExecuteInEditMode]
public class SpriteLightSource : MonoBehaviour
{

    #region fields

    protected Transform camQuad;// положение и размеры квада камеры, в котором отрисовываются тени

    #endregion //fields

    //Внести в список источников света
    public void OnEnable()
    {
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            SpriteLightKit lKit = cam.GetComponentInChildren<SpriteLightKit>();
            if (lKit != null ? !lKit.lightSources.Contains(this) : false)
            {
                lKit.lightSources.Add(this);
                camQuad=cam.transform.FindChild("ObstacleCamera").FindChild("ObstacleCamera1").FindChild("Quad");
            }
        }
        //if (!SpriteLightKit.lightSources.Contains(this))
            //SpriteLightKit.lightSources.Add(this);
    }

    //Внести в список источников света
    public void Awake()
    {
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            SpriteLightKit lKit = cam.GetComponentInChildren<SpriteLightKit>();
            if (lKit != null ? !lKit.lightSources.Contains(this) : false)
            {
                lKit.lightSources.Add(this);
            }
            camQuad = cam.transform.FindChild("ObstacleCamera").FindChild("ObstacleCamera1").FindChild("Quad");
        }
        //if (!SpriteLightKit.lightSources.Contains(this))
        //SpriteLightKit.lightSources.Add(this);
    }

    //Перестать учитывать этот источник света
    public void OnDisable()
    {
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            SpriteLightKit lKit = cam.GetComponentInChildren<SpriteLightKit>();
            if (lKit != null ? lKit.lightSources.Contains(this) : false)
                lKit.lightSources.Remove(this);
        }
    }

    //Перестать учитывать этот источник света
    public void OnDestroy()
    {
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            SpriteLightKit lKit = cam.GetComponentInChildren<SpriteLightKit>();
            if (lKit != null ? lKit.lightSources.Contains(this) : false)
                lKit.lightSources.Remove(this);
        }
    }

    void Update()
    {
        if (camQuad != null)
        {
            material.SetFloat("_LightPositionX", Mathf.Clamp((transform.position.x - camQuad.position.x + camQuad.lossyScale.x/2f) / camQuad.lossyScale.x, 0f, 1f));
            material.SetFloat("_LightPositionY", Mathf.Clamp((transform.position.y - camQuad.position.y + camQuad.lossyScale.y / 2f) / camQuad.lossyScale.y, 0f, 1f));
        }
    }

    public Material material { get { return GetComponent<SpriteRenderer>().sharedMaterial; } }

}

