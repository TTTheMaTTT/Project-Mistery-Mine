using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Особый тип хитбокса, который может атаковать только игрока и не через коллайдерные функции (OnTriggerStay), а через FixedUpdate()
/// </summary>
public class HitBoxCollider : MonoBehaviour
{

    #region delegates

    private delegate void UpdateAction();

    #endregion //delegates

    #region parametres

    UpdateAction updateAction;
    public Vector2 size;
    public Vector2 position;

    protected bool immobile;
    public bool Immobile { set { immobile = value; } }
    
    public bool againstHero = true;//Если true, то хитбокс может атаковать только героя
    protected string enemyLayer = "hero";
    public bool allyHitBox { get { return enemyLayer == "character"; }  set { enemyLayer = value ? "character" : "hero"; } }

    protected bool alwaysAttack = false;//Если true, то хитбокс будет пытаться нанести атаку вне зависимости от того было ли переключение или нет (постоянна ли атака хитбокса, или это хитбокс одного удара?)
    public bool AlwaysAttack { set { alwaysAttack = value; } }
    protected bool attacked;

    [SerializeField] private bool considerRotation=false;//Если true, то хитбокс будет поворачиваться вместе с использующим объектом.

    #endregion //parametres

    #region eventHandlers

    public EventHandler<HitEventArgs> AttackEventHandler;//Хэндлер события "атака произошла"

    #endregion //eventHandlers

    public void Awake()
    {
        if (considerRotation)
            updateAction = ConsiderAngleScenario;
        else
            updateAction = UsualScenario;
    }

    public void FixedUpdate()
    {
        updateAction.Invoke();
    }

    #region attackScenarios

    /// <summary>
    /// Объект, использующий хитбокс, не может повернуться. Просчёт расположения хитбокса ведётся без учёта угла поворота.
    /// </summary>
    private void UsualScenario()
    {
        if (alwaysAttack || !attacked)
        {
            Vector2 pos = transform.position;
            Vector2 _pos = immobile ? Vector2.zero : new Vector2(position.x * Mathf.Sign(transform.lossyScale.x), position.y);
            //Collider2D col = Physics2D.OverlapArea(pos + _pos + size, pos + _pos - size, LayerMask.GetMask(hLayer));
            Collider2D[] cols = Physics2D.OverlapBoxAll(pos+_pos, size, transform.eulerAngles.z, LayerMask.GetMask(enemyLayer));
            bool damaged = false;
            for (int i=0;i<cols.Length;i++)
            {
                if (againstHero ? !cols[i].gameObject.CompareTag("player") : false)
                    continue;
                GameObject obj = cols[i].gameObject;
                IDamageable target = obj.GetComponent<IDamageable>();
                if (target == null)
                    continue;
                
                float prevHP = target.GetHealth();
                OnAttack(new HitEventArgs(prevHP, obj));
                damaged = true;
            }
            if (damaged)
                attacked = true;
        }
    }

    /// <summary>
    /// Сценарий, при котором хитбокс неподвижен относительно объекта, но может повернуться
    /// </summary>
    private void ConsiderAngleScenario()
    {
        if (alwaysAttack || !attacked)
        {
            Vector2 pos = transform.position;
            Collider2D[] cols = Physics2D.OverlapBoxAll(pos,size,transform.eulerAngles.z, LayerMask.GetMask(enemyLayer));
            bool damaged = false;
            for (int i=0;i< cols.Length;i++)
            {
                if (againstHero ? !cols[i].gameObject.CompareTag("player") : false)
                    continue ;
                GameObject obj = cols[i].gameObject;
                IDamageable target = obj.GetComponent<IDamageable>();
                float prevHP = target.GetHealth();
                OnAttack(new HitEventArgs(prevHP, obj));
                damaged= true;
            }
            if (damaged)
                attacked = true;
        }
    }

    #endregion //attackScenarios

    /// <summary>
    /// Функция, которая управляет режимом работы этого компонента (вкл-выкл)
    /// </summary>
    /// <param name="_activated">включить или выключить компонент</param>
    public void Activate(bool _activated)
    {
        attacked = false;
        enabled = _activated;
    }

    #region events

    /// <summary>
    /// Событие, вызываемое при совершении атаки
    /// </summary>
    public virtual void OnAttack(HitEventArgs e)
    {
        EventHandler<HitEventArgs> handler = AttackEventHandler;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.activeObject == gameObject)
        {
            float angle = transform.eulerAngles.z/180f*Mathf.PI;
            Vector2 vectX = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))/2f;
            Vector2 vectY = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle))/2f;
            Vector2 pos = transform.position;
            Vector2 _pos = pos+ (immobile? Vector2.zero:( position.x*vectX* 2f*Mathf.Sign(transform.lossyScale.x) + position.y*2f*vectY));
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_pos + size.x*vectX+size.y*vectY, _pos + size.x * vectX - size.y * vectY);
            Gizmos.DrawLine(_pos + size.x * vectX - size.y * vectY, _pos - size.x * vectX - size.y * vectY);
            Gizmos.DrawLine(_pos - size.x * vectX - size.y * vectY, _pos - size.x * vectX + size.y * vectY);
            Gizmos.DrawLine(_pos - size.x * vectX + size.y * vectY, _pos + size.x * vectX + size.y * vectY);
        }
#endif //UNITY_EDITOR
    }

}
