using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, реализующий поведение сундука
/// </summary>
public class ChestController : MonoBehaviour, IInteractive
{

    #region consts

    private const float pushForceY = 50f, pushForceX = 25f;//С какой силой выбрасывается содержимое сундука при его открытии?

    #endregion //consts

    #region fields

    public List<DropClass> content = new List<DropClass>();

    #endregion //fields

    #region parametres

    [SerializeField]
    [HideInInspector]
    int id;

    #endregion //parametres

    /// <summary>
    /// Как происходит взаимодействие с сундуком
    /// </summary>
    public void Interact()
    {
        foreach (DropClass drop in content)
        {
            GameObject _drop = Instantiate(drop.gameObject,transform.position+Vector3.up*.05f,transform.rotation) as GameObject;
            if (_drop.GetComponent<Rigidbody2D>() != null)
            {
                _drop.GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.RandomRange(-pushForceX, pushForceX), pushForceY));
            }
            /*GameObject obj = new GameObject(drop.item.itemName);
            obj.transform.position = transform.position;
            DropClass.AddDrop(obj, drop);
            Rigidbody2D rigid = obj.GetComponent<Rigidbody2D>();
            rigid.AddForce(new Vector2(Random.RandomRange(-pushForceX, pushForceX), pushForceY));*/
        }
        gameObject.tag = "Untagged";
        SpecialFunctions.statistics.ConsiderStatistics(this);
        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.Play("Opened");
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
    /// Загрузить данные о сундуке
    /// </summary>
    public void SetData(InterObjData _intObjData)
    {
    }

    /// <summary>
    /// Сохранить данные о сундуке
    /// </summary>
    public InterObjData GetData()
    {
        InterObjData cData = new InterObjData(id);
        return cData;
    }

    /// <summary>
    /// Сразу открыть сундук без вываливания содержимого
    /// </summary>
    public void DestroyClosedChest()
    {
        gameObject.tag = "Untagged";
        SpecialFunctions.statistics.ConsiderStatistics(this);
        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.Play("Opened");
        DestroyImmediate(this);
    }

}
