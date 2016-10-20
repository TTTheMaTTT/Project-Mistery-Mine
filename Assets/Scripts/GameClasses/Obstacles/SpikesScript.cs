using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Скрипт, реализующий механику шипов
/// </summary>
public class SpikesScript : MonoBehaviour
{

    #region fields

    [SerializeField] private List<string> enemies;//По каким тегам искать врагов?
    public List<string> Enemies { set { enemies = value; } }

    protected List<GameObject> list = new List<GameObject>();//Список всех атакованных противников. (чтобы один удар не отнимал hp дважды)

    [SerializeField]protected float damage;
    public float Damage { set { damage = value; } }

    #endregion //fields

    #region parametres

    protected bool activated;

    #endregion //parametres

    protected void Awake()
    {
        SetHitBox();
    }

    /// <summary>
    /// Сбросить хитбокс
    /// </summary>
    public void ResetHitBox()
    {
        activated = false;
        list.Clear();
    }

    /// <summary>
    /// Настройка ХитБокса
    /// </summary>
    public void SetHitBox()
    {
        activated = true;
        list = new List<GameObject>();
    }

    /// <summary>
    /// Cмотрим, попал ли хитбокс по врагу, и, если попал, то идёт расчёт урона
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (activated)
        {
            if (enemies != null ? (enemies.Count == 0 ? false : enemies.Contains(other.gameObject.tag)) : true)
            {
                IDamageable target = other.gameObject.GetComponent<IDamageable>();
                if (target != null)
                {
                    if (!list.Contains(other.gameObject))
                    {
                        list.Add(other.gameObject);
                        target.TakeDamage(damage,true);
                    }
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (activated)
        {
            if (enemies != null ? (enemies.Count == 0 ? false : enemies.Contains(other.gameObject.tag)) : true)
            {
                if (list.Contains(other.gameObject))
                {
                    list.Remove(other.gameObject);
                }
            }
        }
    }

}
