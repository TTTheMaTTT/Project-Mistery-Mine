using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoodoDollClass : BowClass
{

    #region parametres

    public int attackBalance;//Баланс персонажа, когда он атакует этим оружием
    protected int prevBalance;//Баланс персонажа перед атакой
    public LayerMask whatIsVoodoAim;//По каким целям бьёт кукла вуду?

    protected HeroController hero;
    protected HeroController Hero{get {if (hero ==null) hero=SpecialFunctions.Player.GetComponent<HeroController>(); return hero;}}


    #endregion //parametres

    /// <summary>
    /// Функция, что возвращает новый экземпляр класса, который имеет те же данные, что и экземпляр, выполняющий этот метод
    /// </summary>
    public override WeaponClass GetWeapon()
    {
        return new VoodoDollClass(this);
    }

    public VoodoDollClass(VoodoDollClass _doll): base(_doll)
    {
        attackBalance = _doll.attackBalance;
        whatIsVoodoAim = _doll.whatIsVoodoAim;
    }

    /// <summary>
    /// Функция выстрела из оружия
    /// </summary>
    public override void Shoot(HitBoxController hitBox, Vector3 position, int orientation, LayerMask whatIsAim, List<string> enemies)
    {
        hitBox.StartCoroutine(DontShootProcess());

        Collider2D[] cols = Physics2D.OverlapCircleAll(position, shootDistance, whatIsVoodoAim);
        List<IDamageable> targets = new List<IDamageable>();
        List<GameObject> targetObjs = new List<GameObject>();
        foreach (Collider2D col in cols)
        {
            IDamageable target = col.GetComponent<IDamageable>();
            GameObject targetObj = col.gameObject;
            if (target != null && enemies.Contains(targetObj.tag))
            {
                targetObjs.Add(targetObj);
                targets.Add(target);
            }
        }
        if (targets.Count>0)
        {
            int index = Random.Range(0, targets.Count);
            IDamageable target = targets[index];
            GameObject targetObj = targetObjs[index];
            
            AIController ai = targetObj.GetComponent<AIController>();
            if (ai != null)
                ai.TakeAttackerInformation(new AttackerClass(SpecialFunctions.player, AttackTypeEnum.range));
            target.TakeDamage(new HitParametres(damage, attackType, attackPower, effectChance));
        }
        chargeValue = 0f;
        SpecialFunctions.camControl.ShakeCamera();
    }

    /// <summary>
    /// Начало атаки
    /// </summary>
    public override void StartAttack()
    {
        prevBalance = Hero.Balance;
        Hero.Balance = attackBalance;
    }

    /// <summary>
    /// Конец атаки
    /// </summary>
    public override void StopAttack()
    {
        Hero.Balance = prevBalance;
    }


}
