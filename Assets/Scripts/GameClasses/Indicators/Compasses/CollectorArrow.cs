using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Контроллер стрелки, указывающей на ближайший элемент коллекции
/// </summary>
public class CollectorArrow : MonoBehaviour
{

    #region consts

    private const float collectorTime = 15f;//Время действия эффекта "Коллекционер"

    #endregion //consts

    #region eventHandlers

    public EventHandler<EventArgs> DestroyEvent;//Событие, связанное с исчезновением стрелки

    #endregion //eventHandlers

    #region fields

    protected Transform target;//На какую цель указывает стрелка

    #endregion //fields

    #region parametres

    protected float beginTime = 0f;//Время начала работы стрелки

    #endregion //parametres

    void Update()
    {
        if (target == null)
        {
            Initialize(collectorTime - Time.fixedTime + beginTime);
        }
        if (transform.lossyScale.x < 0f)
        {
            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
        }
        Vector2 direction = (target.position - transform.position);
        transform.eulerAngles = new Vector3(0f, 0f, Vector2.Angle(Vector2.right, direction) * Mathf.Sign(direction.y));
    }

    /// <summary>
    /// Найти ближайший коллекционный предмет и начать указывать на него
    /// </summary>
    public void Initialize(float _time)
    {
        if (beginTime == 0f)
            beginTime = Time.fixedTime;
        float minDistance = Mathf.Infinity;
        CollectionDropClass[] drops = FindObjectsOfType<CollectionDropClass>();
        ChestController[] chests = FindObjectsOfType<ChestController>();
        target = null;
        Vector3 pos = transform.position;
        foreach (CollectionDropClass drop in drops)
        {
            float sqDistance = Vector2.SqrMagnitude(drop.transform.position - pos);
            if (sqDistance < minDistance)
            {
                minDistance = sqDistance;
                target = drop.transform;
            }
        }

        if (target == null)
        {
            foreach (ChestController chest in chests)
            {
                bool hasCollection = false;
                foreach (DropClass drop1 in chest.content)
                    if (drop1 is CollectionDropClass)
                    {
                        hasCollection = true;
                        break;
                    }
                float sqDistance = Vector2.SqrMagnitude(chest.transform.position - pos);
                if (sqDistance < minDistance && hasCollection)
                {
                    minDistance = sqDistance;
                    target = chest.transform;
                }
            }
        }

        if (target == null)
        {
            SpecialFunctions.SetSecretText(2f, "Рядом с Вами нет ничего ценного");
            DestroyArrow();
        }
        else
        {
            CollectionDropClass drop = target.GetComponent<CollectionDropClass>();
            if (drop == null)
                DestroyArrow();
            else
            {
                drop.DropIsGot += HandleDropGet;
                StartCoroutine(WorkProcess(_time));
            }
        }
    }

    /// <summary>
    /// Специальная функция для правильного удаления стрелки
    /// </summary>
    public void DestroyArrow()
    {
        if (target != null)
        {
            CollectionDropClass drop = target.GetComponent<CollectionDropClass>();
            if (drop != null)
                drop.DropIsGot -= HandleDropGet;
        }
        OnDestroyed(new EventArgs());
        Destroy(gameObject);
    }

    /// <summary>
    /// Процесс работы стрелки
    /// </summary>
    /// <param name="_time">Длительность работы</param>
    IEnumerator WorkProcess(float _time)
    {
        yield return new WaitForSeconds(_time);
        DestroyArrow();
    }

    #region events

    /// <summary>
    /// Событие "стрелка исчезла"
    /// </summary>
    protected virtual void OnDestroyed(EventArgs e)
    {
        EventHandler<EventArgs> handler = DestroyEvent;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    #endregion //events

    #region eventHandlers

    /// <summary>
    /// Обработка события "дроп был взят"
    /// </summary>
    void HandleDropGet(object sender, EventArgs e)
    {
        DestroyArrow();
    }

    #endregion //eventHandlers
}
