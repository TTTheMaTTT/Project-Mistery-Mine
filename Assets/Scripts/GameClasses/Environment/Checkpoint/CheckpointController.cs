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
    GameObject light;

    #endregion //fields

    #region parametres

    public int checkpointNumb = 0;//Номерной знак чекпоинта на уровне
    public bool activated = false;//Чекпоинт можно активировать лишь один раз

    [SerializeField]
    [HideInInspector]
    int id;

    #endregion //parametres

    public void Awake()
    {
        anim = GetComponent<Animator>();
        if (transform.childCount > 0)
        {
            light = transform.FindChild("Light").gameObject;
            light.SetActive(false);
        }
    }

    public void Update()
    {
        if (activated)
            DestroyCheckpoint();
    }

    /// <summary>
    /// Провзаимодействовать с чекпоинтом
    /// </summary>
    public void Interact()
    {
        if (!activated)
        {
            activated = true;
            SpecialFunctions.gameController.SaveGame(checkpointNumb, false, SceneManager.GetActiveScene().name);
            DestroyCheckpoint();
        }
    }

    /// <summary>
    /// После активации чекпоинт больше не может быть активирован
    /// </summary>
    public void DestroyCheckpoint()
    {
        if (anim != null)
        {
            anim.Play("Active");
        }
        if (light != null)
        {
            light.SetActive(true);
        }
        DestroyImmediate(this);
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
        MechData chData = new MechData(id, activated,transform.position);
        return chData;
    }

}
