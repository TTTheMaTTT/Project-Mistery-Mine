using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, реализующий поведение сундука
/// </summary>
public class ChestController : MonoBehaviour, IInteractive
{

    #region consts

    private const float pushForceY = 30f, pushForceX = 70f;//С какой силой выбрасывается содержимое сундука при его открытии?

    #endregion //consts

    #region fields

    public List<DropClass> content = new List<DropClass>();

    #endregion //fields

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
        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.Play("Opened");
        DestroyImmediate(this);
    }

}
