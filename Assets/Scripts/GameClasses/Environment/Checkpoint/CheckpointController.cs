using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Скрипт, управляющий чекпоинтами
/// </summary>
public class CheckpointController : MonoBehaviour, IInteractive
{

    #region fields

    Animator anim;
    SpriteRenderer sRenderer;
    GameObject checkpointLight;

    #endregion //fields

    #region parametres

    public int checkpointNumb = 0;//Номерной знак чекпоинта на уровне
    public bool activated = false;//Чекпоинт можно активировать лишь один раз

    [SerializeField]
    int id;

    protected Color outlineColor = Color.yellow;//Цвет контура
    protected bool changed = false;

    #endregion //parametres

    public void Awake()
    {
        anim = GetComponent<Animator>();
        sRenderer = GetComponent<SpriteRenderer>();
        if (transform.childCount > 0)
        {
            checkpointLight = transform.FindChild("Light").gameObject;
            checkpointLight.SetActive(false);
        }
    }

    public void Update()
    {
        if (activated && !changed)
            ChangeCheckpoint();
    }

    /// <summary>
    /// После активации чекпоинт больше не может быть активирован
    /// </summary>
    public void ChangeCheckpoint()
    {
        SetOutline(false);
        changed = true;
        if (anim != null)
        {
            anim.Play("Active");
        }
        if (checkpointLight != null)
        {
            checkpointLight.SetActive(true);
        }
        SetID(-1);//Чекпоинт не учтётся при сохранении, следовательно, чекпоинт будет считаться неактивным при следующей загрузке
    }

    #region IInteractive

    /// <summary>
    /// Провзаимодействовать с чекпоинтом
    /// </summary>
    public void Interact()
    {
        if (!activated)
        {
            activated = true;
            SpecialFunctions.gameController.SaveGame(checkpointNumb, false, SceneManager.GetActiveScene().name);
            ChangeCheckpoint();
        }
        else
        {
            SpecialFunctions.equipWindow.OpenWindow();
        }
    }

    /// <summary>
    /// Отрисовать контур, если происзодит взаимодействие (или убрать этот контур)
    /// </summary>
    public virtual void SetOutline(bool _outline)
    {
        if (sRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            sRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_Outline", _outline ? 1f : 0);
            mpb.SetColor("_OutlineColor", outlineColor);
            sRenderer.SetPropertyBlock(mpb);
        }
    }

    /// <summary>
    /// Можно ли провзаимодействовать с объектом в данный момент?
    /// </summary>
    public virtual bool IsInteractive()
    {
        return true;
    }

    #endregion //IInteractive

    #region IHaveID

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
    /// Загрузить данные о чекпоинте 
    /// </summary>
    public void SetData(InterObjData _intObjData)
    {
        MechData chData = (MechData)_intObjData;
        if (chData != null)
        {
            activated = chData.activated;
        }
    }

    /// <summary>
    /// Сохранить данные о чекпоинте
    /// </summary>
    public InterObjData GetData()
    {
        MechData chData = new MechData(id, activated,transform.position, gameObject.name);
        return chData;
    }

    #endregion //IHaveID

}
