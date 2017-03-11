using UnityEngine;
using System.Collections;

/// <summary>
/// Триггер, при входе в который выдаётся сообщение, что было найдено секретное место
/// </summary>
public class SecretPlaceTrigger : MonoBehaviour, IHaveID
{

    #region parametres

    [SerializeField][HideInInspector]public int id;

    #endregion //parametres

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("player"))
        {
            SpecialFunctions.FindSecretPlace(1.5f);
            Destroy(gameObject);
        }
    }

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
    /// Загрузить данные о секретном месте
    /// </summary>
    public virtual void SetData(InterObjData _intObjData)
    {
    }

    /// <summary>
    /// Запомнить секретное место с таким id
    /// </summary>
    public virtual InterObjData GetData()
    {
        return new InterObjData(id);
    }

    #endregion //IHaveID

}
