using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Сильно упрощённый вариант класса Battlefield, который просто отыскивает объекты с AreaTrigger и задаёт им оптимизированные, либо полноценные версии, согласуя это с полем боя
/// Этот класс навешивается на камеру
/// </summary>
public class AreaTriggerIndicator : MonoBehaviour
{

    #region fields

    private BattleField bField;//Экземпляр класса боевого поля, с которым согласуются результаты оптимизации
    private List<AreaTrigger> objectsInside = new List<AreaTrigger>();//Объекты с поддержкой режима оптимизации, которые находятся внутри коллайдера, и которые могут учитываться только данным индикатором

    #endregion //fields

    #region parametres

    private float bFieldRadius = 0f;

    #endregion //parametres

    void Awake()
    {
        objectsInside = new List<AreaTrigger>();
        GameObject player = SpecialFunctions.Player;
        bField = SpecialFunctions.battleField;
        bFieldRadius = bField.Radius + .2f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        AreaTrigger aTrigger = other.GetComponent<AreaTrigger>();
        if (aTrigger == null)
            return;
        if (objectsInside.Contains(aTrigger))
            return;
        Vector3 bFieldPos = bField.transform.position;
        if (Vector2.SqrMagnitude((Vector2)(bFieldPos - other.gameObject.transform.position)) > bFieldRadius * bFieldRadius)
        {
            objectsInside.Add(aTrigger);
            aTrigger.TriggerIn();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        AreaTrigger aTrigger = other.GetComponent<AreaTrigger>();
        if (aTrigger == null)
            return;
        if (!objectsInside.Contains(aTrigger))
            return;
        Vector3 bFieldPos = bField.transform.position;
        if (Vector2.SqrMagnitude((Vector2)(bFieldPos - other.gameObject.transform.position)) > bFieldRadius * bFieldRadius)
        {
            objectsInside.Remove(aTrigger);
            aTrigger.TriggerOut();
        }
    }

    /// <summary>
    /// Активировать или деактивировать компонент
    /// </summary>
    /// <param name="_activate">Если true, то объект активируется, иначе деактивируется</param>
    public void Activate(bool _activate)
    {
        if (!_activate)
        {
            Vector3 bFieldPos = bField.transform.position;
            foreach (AreaTrigger aTrigger in objectsInside)
                if (Vector2.SqrMagnitude((Vector2)(bFieldPos - aTrigger.transform.position)) > bFieldRadius * bFieldRadius)
                    aTrigger.TriggerOut();
            objectsInside.Clear();
        }
        gameObject.SetActive(_activate);
    }

}
