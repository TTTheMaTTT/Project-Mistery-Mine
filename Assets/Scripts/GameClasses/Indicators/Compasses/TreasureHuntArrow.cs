using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Контроллер стрелки, указывающей на ближайшее сокровище
/// </summary>
public class TreasureHuntArrow : MonoBehaviour
{

    #region eventHandlers

    public EventHandler<EventArgs> DestroyEvent;//Событие, связанное с исчезновением стрелки

    #endregion //eventHandlers

    #region fields

    protected Transform target;//На какую цель указывает стрелка

    #endregion //fields

    void Update()
    {
        if (target == null)
            return;
        if (transform.lossyScale.x < 0f)
        {
            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
        }
        Vector2 direction = (target.position - transform.position);
        transform.eulerAngles = new Vector3(0f, 0f, Vector2.Angle(Vector2.right, direction) * Mathf.Sign(direction.y));
    }

    /// <summary>
    /// Найти ближайшее сокровище и начать указывать на него
    /// </summary>
    public void Initialize(float _time)
    {
        float minDistance = Mathf.Infinity;
        ChestController[] chests = FindObjectsOfType<ChestController>();
        BoxController[] boxes = FindObjectsOfType<BoxController>();
        target = null;
        Vector3 pos = transform.position;
        foreach (ChestController chest in chests)
        {
            float sqDistance = Vector2.SqrMagnitude(chest.transform.position - pos);
            if (sqDistance < minDistance)
            {
                minDistance = sqDistance;
                target = chest.transform;
            }
        }
        foreach (BoxController box in boxes)
        {
            if (box.gameObject.name.Contains("Exploding"))
                continue;
            float sqDistance = Vector2.SqrMagnitude(box.transform.position - pos);
            if (sqDistance < minDistance)
            {
                minDistance = sqDistance;
                target = box.transform;
            }
        }
        if (target == null)
        {
            SpecialFunctions.SetSecretText(2f, "Рядом с Вами нет ничего ценного");
            DestroyArrow();
        }
        else
        {
            BoxController box = target.GetComponent<BoxController>();
            ChestController chest = target.GetComponent<ChestController>();
            if (box != null)
                box.BoxDestroyedEvent += HandleBoxDestroy;
            else
                chest.ChestOpenEvent += HandleChestOpen;
            StartCoroutine(WorkProcess(_time));
        }
    }

    /// <summary>
    /// Специальная функция для правильного удаления стрелки
    /// </summary>
    public void DestroyArrow()
    {
        if (target!=null)
        {
            BoxController box = target.GetComponent<BoxController>();
            ChestController chest = target.GetComponent<ChestController>();
            if (box != null)
                box.BoxDestroyedEvent -= HandleBoxDestroy;
            else
                chest.ChestOpenEvent -= HandleChestOpen;
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

    void HandleChestOpen(object sender, EventArgs e)
    {
        DestroyArrow();
    }

    void HandleBoxDestroy(object sender, EventArgs e)
    {
        DestroyArrow();
    }

    #endregion //eventHandlers

}
