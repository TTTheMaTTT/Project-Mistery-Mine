﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Хит-бокс - это объект, область, посредством которого наносят урон
/// </summary>
public class HitBox : MonoBehaviour
{
    #region consts 

    protected const float eps=.0001f;

    #endregion //consts

    #region fields

    protected BoxCollider2D col;//Область удара
    protected BoxCollider2D Col { get { if (col == null); col = GetComponent<BoxCollider2D>(); return col; } }
    private List<string> enemies;//По каким тегам искать врагов?

    protected List<GameObject> list=new List<GameObject>();//Список всех атакованных противников. (чтобы один удар не отнимал hp дважды)
    private HitClass hitData=new HitClass();

    #endregion //fields

    #region parametres

    protected bool activated;
    protected bool immobile;//Запрет на перемещение хитбокса
    public bool Immobile { set { immobile = value; } }

    protected int k=1;

    #endregion //parametres

    #region eventHandlers

    public EventHandler<HitEventArgs> AttackEventHandler;//Хэндлер события о визуализации действия

    #endregion //eventHandlers

    //Инициализация
    public void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.enabled = activated;
    }

    /// <summary>
    /// Функция, обеспечивающая постоянную работу коллайдера
    /// </summary>
    public void FixedUpdate()
    {
        k *= -1;
        transform.localPosition += new Vector3(k * eps, 0f, 0f);
    }

    public void ResetHitBox()
    {
        StopAllCoroutines();
        activated = false;
        list.Clear();
        Col.enabled = false;
    }

    /// <summary>
    /// Настройка ХитБокса
    /// </summary>
    public void SetHitBox(HitClass _hitData)
    {
        activated = true;
        hitData = _hitData;
        Col.enabled = true;
        if (!immobile)
            transform.localPosition = hitData.hitPosition;
        col.size = hitData.hitSize;
        if (hitData.actTime != -1f)
        {
            StartCoroutine(HitProcess(hitData.actTime));
        }
    }

    /// <summary>
    /// Настройка хитбокса (без изменения положения и размера хитбокса)
    /// </summary>
    /// <param name="_damage">Наносимый урон</param>
    /// <param name="_actTime">Время действи (если -1, то всегда действует)</param>
    /// <param name="_hitForce">Сила отталкивания, действующая на цель</param>
    public void SetHitBox(float _damage, float _actTime, float _hitForce)
    {
        activated = true;
        Col.enabled = true;
        hitData = new HitClass(_damage,_actTime, _hitForce);
        if (hitData.actTime != -1f)
        {
            StartCoroutine(HitProcess(hitData.actTime));
        }
    }

    /// <summary>
    /// Процесс нанесения урона
    /// </summary>
    protected IEnumerator HitProcess(float hitTime)
    {
        activated = true;
        yield return new WaitForSeconds(hitTime);
        activated = false;
        list.Clear();
        col.enabled = false;
    }

    /// <summary>
    /// Cмотрим, попал ли хитбокс по врагу, и, если попал, то идёт расчёт урона
    /// </summary>
    void OnTriggerStay2D(Collider2D other)
    {
        
        if (activated)
        {
            if (enemies != null ? (enemies.Count == 0 ? false : enemies.Contains(other.gameObject.tag)) : true)
            {
                IDamageable target = other.gameObject.GetComponent<IDamageable>();
                if (target != null)
                {
                    float prevHP = target.GetHealth();
                    if (hitData.actTime == -1f)
                    {
                        Rigidbody2D rigid;
                        if ((rigid = other.GetComponent<Rigidbody2D>()) != null && !target.InInvul())
                        {
                            rigid.AddForce((new Vector2(Mathf.Sign(transform.lossyScale.x), 0f)) * hitData.hitForce);//Атака всегда толкает вперёд
                        }
                        target.TakeDamage(hitData.damage);
                        OnAttack(new HitEventArgs(target.GetHealth()-prevHP));
                        return;
                    }
                    if (!list.Contains(other.gameObject))
                    {
                        list.Add(other.gameObject);
                        Rigidbody2D rigid;
                        if ((rigid = other.GetComponent<Rigidbody2D>()) != null && !target.InInvul())
                        {
                            rigid.velocity = Vector2.zero;
                            rigid.AddForce((new Vector2(Mathf.Sign(transform.lossyScale.x), 0f)) * hitData.hitForce);//Атака всегда толкает вперёд
                        }
                        target.TakeDamage(hitData.damage);
                        OnAttack(new HitEventArgs(target.GetHealth() - prevHP));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Установить, по каким целям будет попадать хитбокс
    /// </summary>
    public void SetEnemies(List<string> _enemies)
    {
        enemies = _enemies;
    }

    #region events

    /// <summary>
    /// Событие, вызываемое при совершении атаки
    /// </summary>
    public void OnAttack(HitEventArgs e)
    {
        EventHandler<HitEventArgs> handler = AttackEventHandler;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

}


/// <summary>
/// Класс, содержащий данные об атаке
/// </summary>
[System.Serializable]
public class HitClass
{
    public float damage;
    public float actTime;
    public Vector2 hitSize;
    public Vector2 hitPosition;
    public float hitForce;//Насколько сильно отталкивает атака

    public HitClass(float _damage, float _actTime, Vector2 _size, Vector2 _position, float _hitForce)
    {
        damage = _damage;
        actTime = _actTime;
        hitSize = _size;
        hitPosition = _position;
        hitForce = _hitForce;
    }

    public HitClass(float _damage, float _actTime, float _hitForce)
    {
        damage = _damage;
        actTime = _actTime;
        hitSize = Vector2.zero;
        hitPosition = Vector2.zero;
        hitForce = _hitForce;
    }

    public HitClass()
    { }

}