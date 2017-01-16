using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс особого оружия - бумеранга
/// </summary>
public class BoomerangClass : BowClass
{

    #region fields

    public GameObject boomerang;//Чем стреляем

    #endregion //fields

    #region parametres

    public float speed = 2f;//С какой скоростью бросается бумеранг
    public float acceleration = 20f;//Ускорение, с которым движется бумеранг

    public Vector2 hitSize;
    public float hitForce = 50f;

    #endregion //parametres

    /// <summary>
    /// Бросок бумеранга
    /// </summary>
    public override void Shoot(HitBoxController hitBox, Vector3 position, int orientation, LayerMask whatIsAim, List<string> enemies)
    {
        canShoot = false;
        RaycastHit2D[] hits = new RaycastHit2D[] { Physics2D.Raycast(position, orientation * Vector3.right, shootDistance, whatIsAim),
                                                   Physics2D.Raycast(position + shootDelta* Vector3.up, orientation * Vector3.right, shootDistance, whatIsAim),
                                                   Physics2D.Raycast(position + Vector3.up*shootDelta/2f, orientation * Vector3.right, shootDistance, whatIsAim),
                                                   Physics2D.Raycast(position + Vector3.up*(-shootDelta/2f),orientation * Vector3.right, shootDistance, whatIsAim),
                                                   Physics2D.Raycast(position + Vector3.up*(-shootDelta), orientation * Vector3.right, shootDistance, whatIsAim),
                                                   Physics2D.Raycast(position +  Vector3.up*(- 1.5f*shootDelta), orientation * Vector3.right,shootDistance, whatIsAim)};
        Vector2 endPoint = position + orientation * Vector3.right * (shootDistance + .1f);
        if (hits[0] || hits[1] || hits[2] || hits[3] || hits[4] || hits[5])
        {
            IDamageable target = null;
            int hitIndex = -1;
            for (int i = 0; i < 5; i++)
            {
                if (hits[i].collider != null ? (target = hits[i].collider.gameObject.GetComponent<IDamageable>()) != null : false)
                {
                    GameObject targetObj = hits[i].collider.gameObject;
                    if (enemies.Contains(targetObj.tag))
                    {
                        hitIndex = i;
                        break;
                    }
                }
            }
            if (hitIndex != -1)
                endPoint = hits[hitIndex].point;
        }
        GameObject _boomerang = GameObject.Instantiate(boomerang, position, Quaternion.identity) as GameObject;
        Vector3 scal = _boomerang.transform.localScale;
        _boomerang.transform.localScale = new Vector3(orientation*scal.x, scal.y, scal.z);
        BoomerangScript boomerangScript = _boomerang.GetComponent<BoomerangScript>();
        if (boomerangScript)
        {
            boomerangScript.SetTarget(endPoint);
            boomerangScript.SetBoomerang(speed, acceleration);
            boomerangScript.SetHitBox(new HitClass(damage,-1f,hitSize,Vector2.zero,hitForce), enemies);
        }
    }

}
