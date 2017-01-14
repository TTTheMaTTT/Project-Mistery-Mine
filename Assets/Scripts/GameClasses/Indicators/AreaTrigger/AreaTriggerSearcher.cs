using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Специальный компонент, который отыскивает объекты с компонентом AreaTrigger и взаимодействует с ними. Важнейший компонент для оптимизации. Предполагается, что он находится у главного героя в индикаторах
/// Также этот компонент участвует в контроле битвы - именно он определяет лимит количества атакующих монстров и контролирует этот лимит
/// </summary>
public class AreaTriggerSearcher : MonoBehaviour
{

    #region fields

    protected List<AIController> enemiesInArea=new List<AIController>();//Какие монстры в агрессивном состоянии находятся в области
    protected List<AIController> agressiveEnemies=new List<AIController>();//Какие монстры нападают на героя в данный момент

    #endregion //fields

    #region parametres

    [SerializeField]
    protected int agressiveMonstersLimit = 8;//Максимальное число агрессивных монстров в области
    protected bool canCount;

    #endregion //parametres

    protected virtual void Awake()
    {
        agressiveEnemies = new List<AIController>();
        enemiesInArea = new List<AIController>();
        canCount = true;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        AreaTrigger aTrigger = other.GetComponent<AreaTrigger>();
        if (aTrigger != null)
        {
            AIController ai = aTrigger.TriggerHolder.GetComponent<AIController>();
            if (ai != null)
            {
                ai.CharacterDeathEvent += HandleEnemyDeathEvent;
                ai.healthChangedEvent += HandleEnemyDamageEvent;
                ai.BehaviorChangeEvent += HandleEnemyChangeBehaviorEvent;
                if (ai.Behavior == BehaviorEnum.agressive)
                {
                    if (!enemiesInArea.Contains(ai))
                    {
                        enemiesInArea.Add(ai);
                        agressiveEnemies.Add(ai);
                    }
                    CheckAgressiveEnemies();
                }
            }
            aTrigger.TriggerIn();
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        AreaTrigger aTrigger = other.GetComponent<AreaTrigger>();
        if (aTrigger != null)
        {
            AIController ai = aTrigger.TriggerHolder.GetComponent<AIController>();
            if (ai != null)
            {
                ai.CharacterDeathEvent -= HandleEnemyDeathEvent;
                ai.healthChangedEvent -= HandleEnemyDamageEvent;
                ai.BehaviorChangeEvent -= HandleEnemyChangeBehaviorEvent;
                if (enemiesInArea.Contains(ai))
                    enemiesInArea.Remove(ai);
                if (agressiveEnemies.Contains(ai))
                    enemiesInArea.Remove(ai);
                CheckAgressiveEnemies();
            }
            aTrigger.TriggerOut();
        }
    }

    /// <summary>
    /// Функция, что контролирует количество агрессивных активных противников
    /// </summary>
    protected void CheckAgressiveEnemies()
    {
        int aECount = agressiveEnemies.Count;
        if (aECount > agressiveMonstersLimit)
        {
            float maxDistance = 0f;
            AIController furthestEnemy=null;
            Vector2 pos = transform.position;
            foreach (AIController enemy in agressiveEnemies)
            {
                float _distance = Vector2.SqrMagnitude((Vector2)enemy.transform.position - pos);
                if (_distance > maxDistance)
                {
                    maxDistance = _distance;
                    furthestEnemy = enemy;
                }
            }
            agressiveEnemies.Remove(furthestEnemy);
            furthestEnemy.Waiting = true;
        }
        else if (aECount < agressiveMonstersLimit && enemiesInArea.Count > aECount)
        {
            Vector2 pos = transform.position;
            float minDistance=Mathf.Infinity;
            AIController nearestEnemy=null;
            foreach (AIController enemy in enemiesInArea)
                if (!agressiveEnemies.Contains(enemy))
                {
                    float _distance = Vector2.SqrMagnitude((Vector2)enemy.transform.position-pos);
                    if (_distance < minDistance)
                    {
                        minDistance = _distance;
                        nearestEnemy = enemy;
                    }
                }
            agressiveEnemies.Add(nearestEnemy);
            nearestEnemy.Waiting = false;
        }
    }

    /// <summary>
    /// Сделать пересчёт всех агрессивных врагов, учитывая их текущие позиции
    /// </summary>
    protected void RefreshAgressiveEnemies()
    {
        if (!canCount)
            return;
        Vector2 pos = transform.position;
        agressiveEnemies = new List<AIController>();
        foreach (AIController ai in enemiesInArea)
        {
            ai.Waiting = true;
        }
        enemiesInArea.Sort((x, y) => { return Vector2.SqrMagnitude((Vector2)x.transform.position - pos).CompareTo(Vector2.SqrMagnitude((Vector2)y.transform.position - pos)); });
        for (int i = 0; i < agressiveMonstersLimit && i < enemiesInArea.Count; i++)
        {
            agressiveEnemies.Add(enemiesInArea[i]);
            enemiesInArea[i].Waiting = false;
        }
        StartCoroutine(NotCountProcess());
    }

    /// <summary>
    /// Процесс, в течение которого нельзя делать пересчёт агрессивных врагов
    /// </summary>
    /// <returns></returns>
    protected IEnumerator NotCountProcess()
    {
        canCount = false;
        yield return new WaitForSeconds(1f);
        canCount = true;
    }

    #region eventHandlers

    /// <summary>
    /// Обработка события - противник понёс урон
    /// </summary>
    /// <param name="sender">Что вызвало событие</param>
    /// <param name="e">Данные события</param>
    protected virtual void HandleEnemyDamageEvent(object sender, HealthEventArgs e)
    {
        AIController ai = (AIController)sender;
        if (ai.Behavior != BehaviorEnum.agressive)
            return;
        else
            if (!agressiveEnemies.Contains(ai))
                RefreshAgressiveEnemies();
    }

    /// <summary>
    /// Обработка события - умер противник
    /// </summary>
    /// <param name="sender">Причина события</param>
    /// <param name="e">Данные события</param>
    protected virtual void HandleEnemyDeathEvent(object sender, StoryEventArgs e)
    {
        AIController ai = (AIController)sender;
        if (ai.Behavior != BehaviorEnum.agressive)
            return;
        else
        {
            ai.CharacterDeathEvent -= HandleEnemyDeathEvent;
            ai.healthChangedEvent -= HandleEnemyDamageEvent;
            ai.BehaviorChangeEvent -= HandleEnemyChangeBehaviorEvent;
            if (agressiveEnemies.Contains(ai))
                agressiveEnemies.Remove(ai);
            enemiesInArea.Remove(ai);
            CheckAgressiveEnemies();
        }

    }

    /// <summary>
    /// Обработка события - противник сменил модель поведения
    /// </summary>
    /// <param name="sender">Что вызвало событие</param>
    /// <param name="e">Данные события</param>
    protected virtual void HandleEnemyChangeBehaviorEvent(object sender, BehaviorEventArgs e)
    {
        AIController ai = (AIController)sender;
        if (e.Behaviour == BehaviorEnum.agressive)
        {
            if (!enemiesInArea.Contains(ai))
            {
                enemiesInArea.Add(ai);
                agressiveEnemies.Add(ai);
                CheckAgressiveEnemies();
            }
        }
        else
        {
            if (enemiesInArea.Contains(ai))
                enemiesInArea.Remove(ai);
            if (agressiveEnemies.Contains(ai))
            {
                agressiveEnemies.Remove(ai);
                CheckAgressiveEnemies();
            }
        }
    }

    #endregion //eventHandlers

}
