using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс стрелкового оружия
/// </summary>
public class BowClass : WeaponClass
{

    #region consts

    protected const float shootDelta = .03f;

    #endregion //consts

    #region fields

    public GameObject arrow;//Чем выстреливаем
    protected LineRenderer line;//Если выстрел сопровождается линией выстрела, то нам необходим этот компонент
    [SerializeField]protected Material arrowMaterial;//Материал, используемый для отрисовки линии

    #endregion //fields

    #region parametres

    public float shootDistance;//Дальность выстрела
    public float shootRate;//Скорострельность, а точнее время, между выстрелами
    public bool canShoot = true;//Можно ли стрелять в данный момент?

    #endregion //parametres

    /// <summary>
    /// Функция выстрела из оружия
    /// </summary>
    public virtual void Shoot(HitBox hitBox, Vector3 position, int orientation, LayerMask whatIsAim, List<string> enemies)
    {
        hitBox.StartCoroutine(DontShootProcess());
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
                        target.TakeDamage(damage);
                        break;
                    }
                }
            }
            if (hitIndex == -1 && hits[0])
            {
                hitIndex = 1;
                GameObject _bullet = GameObject.Instantiate(arrow, new Vector3(hits[0].point.x, hits[0].point.y, position.z), Quaternion.identity) as GameObject;
                Vector3 vect = _bullet.transform.localScale;
                _bullet.transform.localScale = new Vector3(orientation * vect.x, vect.y, vect.z);
            }
            if (hitIndex != -1)
                endPoint = hits[hitIndex].point;
        }
        line = hitBox.gameObject.AddComponent<LineRenderer>();
        line.material = arrowMaterial;
        line.SetWidth(.01f, .01f);
        line.SetVertexCount(2);
        line.SetPosition(0, position);
        line.SetPosition(1, new Vector3(endPoint.x, endPoint.y, position.z));
        line.SetColors(new Color(1f, 1f, 1f, .5f), new Color(1f, 1f, 1f, .5f));
        Destroy(line, .1f);
    }

    /// <summary>
    /// ПроцессЮ в течении которого нельзя стрелять
    /// </summary>
    protected virtual IEnumerator DontShootProcess()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootRate);
        canShoot = true;
    }

    /// <summary>
    /// "Перезапустить" оружие
    /// </summary>
    public virtual void ReloadWeapon()
    {
        canShoot = true;
    }

}