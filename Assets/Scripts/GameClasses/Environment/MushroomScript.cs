using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, управляющий грибами-батутами
/// </summary>
public class MushroomScript : MonoBehaviour
{

    #region consts

    protected const float minSpeed = -.1f;//Как минимум такая скорость должна быть у персонажа, чтобы его подросил гриб-батут. (Персонаж должен надавить на него)

    protected const float pushTime = .3f;

    #endregion //consts

    #region fields

    protected List<Rigidbody2D> rigids=new List<Rigidbody2D>();//Какие объекты находятся под действием гриба-попрыгуна
    public Vector2 up1;

    protected Animator anim;

    #endregion //fields

    #region parametres

    [SerializeField]
    protected float addForce = 2000f;

    #endregion //parametres

    protected void Awake ()
    {
        Initialize();
	}
	
	protected void Update ()
    {
        up1 = transform.up;
	}

    protected void Initialize()
    {
        rigids = new List<Rigidbody2D>();

        anim = GetComponent<Animator>();

    }

    protected void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rigid;
        Vector2 up = transform.up;
        if ((rigid=other.GetComponent<Rigidbody2D>())!=null)
        {
            if (!rigids.Contains(rigid))
            {
                rigids.Add(rigid);
                if ((rigid.velocity.x*up.x+rigid.velocity.y*up.y)/up.magnitude< minSpeed)
                {
                    rigid.velocity = new Vector2(rigid.velocity.x, 0f);
                    rigid.AddForce(up * addForce);
                    StartCoroutine(PushProcess());
                }
            }
        }
    }

    protected void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rigid;
        if ((rigid = other.GetComponent<Rigidbody2D>()) != null)
        {
            if (rigids.Contains(rigid))
            {
                rigids.Remove(rigid);
            }
        }
    }

    protected IEnumerator PushProcess()
    {
        if (anim != null)
        {
            anim.Play("Push");
            yield return new WaitForSeconds(pushTime);
            anim.Play("Idle");
        }
        else
        {
            yield return new WaitForSeconds(0f);
        }
    }

}
