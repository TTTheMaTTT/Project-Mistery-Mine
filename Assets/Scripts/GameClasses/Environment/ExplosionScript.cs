using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Скрипт, управляющий взрывом и задающий его параметры
public class ExplosionScript : MonoBehaviour
{

    #region consts

    protected const float groundRadius = .02f;

    protected const float lifeTime = .5f, setFireTime = 0.25f;
    protected const float fireOffset = -.07f;

    #endregion //consts

    #region fields

    [SerializeField]protected List<string> enemies = new List<string>();

    protected HitBox hitBox;
    protected Transform groundCheck;

    [SerializeField]protected GameObject fire;//Огонь, что оставляет за собой этот взрыв

    #endregion //fields

    #region parametres

    string lName = "ground";
    [SerializeField]protected float damage = 2f;
    [SerializeField]protected bool fireable = true;//Если true, то взрыв оставляет за собой огонь

    protected bool isSet;

    #endregion //parametres

    public void Start()
    {
        hitBox = GetComponent<HitBox>();
        hitBox.Immobile = true;
        hitBox.SetEnemies(enemies);
        hitBox.SetHitBox(new HitClass(damage, -1f, Vector2.zero, transform.position, 0f));
        groundCheck = transform.FindChild("GroundCheck");
        isSet = false;
    }

    public void FixedUpdate()
    {
        if (!isSet)
        {
            isSet = true;
            StartCoroutine(ExplosionProcess());
        }
    }

    /// <summary>
    /// Процесс взрыва
    /// </summary>
    protected IEnumerator ExplosionProcess()
    {
        yield return new WaitForSeconds(setFireTime);
        if (fireable)
            if (Physics2D.OverlapCircle(groundCheck.position, groundRadius, LayerMask.GetMask(lName)))
                Instantiate(fire, transform.position + Vector3.up * fireOffset, transform.rotation);
        yield return new WaitForSeconds(lifeTime - setFireTime);
        Destroy(gameObject);
    }

}
