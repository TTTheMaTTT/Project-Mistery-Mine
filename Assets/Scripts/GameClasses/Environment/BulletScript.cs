using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт, управляющий снарядами
/// </summary>
public class BulletScript : MonoBehaviour
{

    #region consts

    protected const string groundName = "ground", platformName = "platform";

    #endregion //consts

    #region fields

    protected HitBoxController hitBox;
    protected HitParametres attackParametres;
    public HitParametres AttackParametres { get { return AttackParametres; } set { attackParametres = value; } }
    protected bool groundDetect = true;
    public bool GroundDetect { get { return groundDetect; } set { groundDetect = value; if (value) hitBox.SetHitBox(attackParametres); else hitBox.ResetHitBox(); } }
    Rigidbody2D rigid;

    [SerializeField]protected GameObject destroyParticles;

    #endregion //fields

    #region parametres

    [SerializeField]protected float minFallDamageSpeed = 1f;//Минимальная скорость падения, которая должна быть достигнута, чтобы падающий снаряд нанёс урон

    #endregion //parametres

    public virtual void Awake()
    {
        hitBox = GetComponentInChildren<HitBoxController>();
        hitBox.AttackEventHandler += HandleAttackProcess;
        rigid = GetComponent<Rigidbody2D>();
    }

    protected virtual void FixedUpdate()
    {
        hitBox.AttackDirection = rigid.velocity.normalized;
        if (groundDetect)
            return;
        if (rigid.velocity.y < -minFallDamageSpeed)
            GroundDetect = true;
    }

    /// <summary>
    /// Проверка на столкновение с землёй
    /// </summary>
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!groundDetect)
            return;
        string _layer = LayerMask.LayerToName(other.gameObject.layer);
        if (_layer == groundName || _layer==platformName)
            DestroyBullet();
    }

    /// <summary>
    /// Уничтожение снаряда
    /// </summary>
    protected virtual void DestroyBullet()
    {
        if (destroyParticles != null)
        {
            GameObject particles = Instantiate(destroyParticles, transform.position, Quaternion.identity);
            Vector3 scal = particles.transform.localScale;
            particles.transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * scal.x, scal.y, scal.z);
            Destroy(particles, 1.5f);
        }
        Destroy(gameObject);
    }

    #region events

    /// <summary>
    ///  Обработка события "произошла атака" (проверка на столкновение с целью)
    /// </summary>
    protected void HandleAttackProcess(object sender, HitEventArgs e)
    {
        DestroyBullet();
    }

    #endregion //events

}
