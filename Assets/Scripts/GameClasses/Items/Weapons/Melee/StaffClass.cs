using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, описывающий работу посоха, который способен создавать воздушные волны
/// </summary>
public class StaffClass : SwordClass
{

    #region consts

    protected const float shakeTime = .5f;

    #endregion //consts

    #region fields

    public GameObject wave;

    #endregion //fields

    #region parametres

    protected static List<string> enemies = new List<string> { "enemy", "box", "destroyable", "boss" };

    public float waveSpeed;
    public Vector2 wavePosition;
    public float waveRate;
    public bool twoWaves = false;
    public bool canShoot = true;

    #endregion //parametres

    /// <summary>
    /// Функция, что возвращает новый экземпляр класса, который имеет те же данные, что и экземпляр, выполняющий этот метод
    /// </summary>
    public override WeaponClass GetWeapon()
    {
        return new StaffClass(this);
    }

    public StaffClass(StaffClass _sword) : base(_sword)
    {
        wave = _sword.wave;
        waveSpeed = _sword.waveSpeed;
        wavePosition = _sword.wavePosition;
        waveRate = _sword.waveRate;
        twoWaves = _sword.twoWaves;
        canShoot = true;
    }

    /// <summary>
    /// Совершить атаку
    /// </summary>
    public override void Attack(HitBoxController hitBox, Vector3 position)
    {
        int orientation = Mathf.RoundToInt(Mathf.Sign(hitBox.transform.lossyScale.x));
        Vector2 frontWavePosition = (Vector2)position+new Vector2(wavePosition.x * orientation, wavePosition.y);
        Vector2 backWavePosition= (Vector2)position + new Vector2(-wavePosition.x * orientation, wavePosition.y);
        if (canShoot)
        {
            hitBox.StartCoroutine(DontShootProcess());
            GameObject newWave = Instantiate(wave, frontWavePosition, Quaternion.identity) as GameObject;
            if (orientation < 0)
                newWave.transform.localScale -= Vector3.right * 2 * newWave.transform.localScale.x;
            Rigidbody2D waveRigid = newWave.GetComponent<Rigidbody2D>();
            waveRigid.velocity = Vector2.right * orientation * waveSpeed;
            HitBoxController waveHitBox = waveRigid.GetComponentInChildren<HitBoxController>();
            if (waveHitBox != null)
            {
                waveHitBox.SetEnemies(enemies);
                waveHitBox.SetHitBox(new HitParametres(damage, -1f, attackForce, attackType, effectChance, attackPower));
                waveHitBox.heroHitBox = true;
                waveHitBox.AttackerInfo = new AttackerClass(hitBox.transform.parent.gameObject, AttackTypeEnum.range);
            }

            if (twoWaves)
            {
                newWave = Instantiate(wave, backWavePosition, Quaternion.identity) as GameObject;
                if (orientation > 0)
                    newWave.transform.localScale -= Vector3.right * 2 * newWave.transform.localScale.x;
                waveRigid = newWave.GetComponent<Rigidbody2D>();
                waveRigid.velocity = -Vector2.right * orientation * waveSpeed;
                waveHitBox = waveRigid.GetComponentInChildren<HitBoxController>();
                if (waveHitBox != null)
                {
                    waveHitBox.SetEnemies(enemies);
                    waveHitBox.SetHitBox(new HitParametres(damage, -1f, attackForce, attackType, effectChance, attackPower));
                    waveHitBox.heroHitBox = true;
                    waveHitBox.AttackerInfo = new AttackerClass(hitBox.transform.parent.gameObject, AttackTypeEnum.range);
                }
            }

            SpecialFunctions.camControl.ShakeCamera(shakeTime);
        }
        else
        {
            hitBox.SetHitBox(new HitParametres(damage, attackTime, attackSize, attackPosition, attackForce, attackType, effectChance, attackPower));
            chargeValue = 0f;
        }
    }

    /// <summary>
    /// Процесс, в течении которого нельзя стрелять
    /// </summary>
    protected virtual IEnumerator DontShootProcess()
    {
        canShoot = false;
        yield return new WaitForSeconds(waveRate);
        canShoot = true;
    }

}
