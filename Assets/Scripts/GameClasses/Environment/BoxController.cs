using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Коробка с содержимым
/// </summary>
public class BoxController : MonoBehaviour, IDamageable
{

    #region consts

    protected const float dropForceX = 50f, dropForceY = 80f;
    protected const float deathTime = .05f;

    #endregion //consts

    #region fields

    [SerializeField]
    protected List<GameObject> content = new List<GameObject>();//Содержимое коробки, что вываливается из неё

    protected Animator anim;

    #endregion //fields

    #region parametres

    [SerializeField] protected float maxHealth = 10f;
    [SerializeField] protected float health = 10f;

    #endregion //parametres

    protected virtual void Awake()
    {
        Initialize();
    }

    protected void Initialize()
    {
        health = maxHealth;
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public void TakeDamage(float damage)
    {
        health -= damage;
        StopAllCoroutines();
        StartCoroutine(HitProcess());
        if (health <= 0f)
            Destroy();
    }

    /// <summary>
    /// Функция получения урона
    /// </summary>
    public void TakeDamage(float damage, bool ignoreInvul)
    {
        health -= damage;
        StopAllCoroutines();
        StartCoroutine(HitProcess());
        if (health <= 0f)
            Destroy();
    }

    /// <summary>
    /// Что произойдёт, когда коробка будет уничтожена
    /// </summary>
    public void Destroy()
    {
        foreach (GameObject drop in content)
        {
            GameObject drop1 = Instantiate(drop, transform.position, transform.rotation) as GameObject;
            Rigidbody2D rigid = drop1.GetComponent<Rigidbody2D>();
            if (rigid != null)
                rigid.AddForce(new Vector2(Random.Range(-dropForceX, dropForceX), dropForceY));
            Destroy(gameObject, deathTime);
        }
    }

    /// <summary>
    /// Узнать текущее здоровье
    /// </summary>
    public float GetHealth()
    {
        return health;
    }

    protected virtual IEnumerator HitProcess()
    {
        if (anim != null)
        {
            anim.Play("TakeDamage");
            yield return new WaitForSeconds(.1f);
            anim.Play("Idle");
        }
    }

}
