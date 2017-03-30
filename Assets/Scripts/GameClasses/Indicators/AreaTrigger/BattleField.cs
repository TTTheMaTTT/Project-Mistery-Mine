using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Специальный компонент, который отыскивает объекты с компонентом AreaTrigger и взаимодействует с ними. Важнейший компонент для оптимизации. Предполагается, что он находится у главного героя в индикаторах
/// Также этот компонент участвует в контроле битвы - именно он определяет лимит количества атакующих монстров и контролирует этот лимит
/// </summary>
public class BattleField : MonoBehaviour
{

    #region eventHandlers

    public EventHandler<EventArgs> NoEnemiesRemainEventHandler;//Обработчик события "Врагов больше не осталось"

    #endregion eventHandlers

    #region fields

    protected List<CharacterController> allies = new List<CharacterController>();//Какие персонажи, являющиеся союзниками главного героя (вкоючая самого ГГ) находятся в области.
    protected List<AIController> enemies = new List<AIController>();//Какие монстры в агрессивном состоянии находятся в области
    public int enemiesCount { get { return enemies.Count; } }
    protected List<AIController> agressiveEnemies = new List<AIController>();//Какие монстры нападают на героя в данный момент

    public List<CharacterController> Allies { get { return allies; } }
    public List<AIController> Enemies { get { return enemies; } }

    #endregion //fields

    #region parametres

    [SerializeField]
    protected int agressiveMonstersLimit = 8;//Максимальное число агрессивных монстров в области
    protected bool canCount;
    public float Radius { get { CircleCollider2D col = GetComponent<CircleCollider2D>(); if (col != null) return col.radius; else return 0f; } }

    #endregion //parametres

    protected virtual void Awake()
    {
        allies = new List<CharacterController>();
        allies.Add(SpecialFunctions.Player.GetComponent<HeroController>());
        agressiveEnemies = new List<AIController>();
        enemies = new List<AIController>();
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
                ai.LoyaltyChangeEvent += HandleAIChangeLoyaltyEvent;
                if (ai.Loyalty == LoyaltyEnum.ally)
                {
                    if (!allies.Contains(ai))
                        allies.Add(ai);
                }
                else
                {
                    ai.healthChangedEvent += HandleEnemyDamageEvent;
                    ai.BehaviorChangeEvent += HandleEnemyChangeBehaviorEvent;
                    if (ai.Behavior == BehaviorEnum.agressive)
                    {
                        if (!enemies.Contains(ai))
                        {
                            enemies.Add(ai);
                            agressiveEnemies.Add(ai);
                        }
                        CheckAgressiveEnemies();
                    }
                }
            }
            aTrigger.TriggerIn();
        }
    }

    /// <summary>
    /// Найти ближайшего противника на поле боя
    /// </summary>
    /// <param name="currentPosition">текущая позиция, откуда идёт запрос</param>
    /// <param name="findEnemy">Если true, то поиск будет происходить среди врагов, иначе - среди союзников</param>
    /// <returns>Игровой объект, представляющий живую потенциальную цель</returns>
    public Transform GetNearestCharacter(Vector2 currentPosition, bool findEnemy)
    {
        float minDistance = Mathf.Infinity;
        Transform obj = null;
        int length = findEnemy ? enemies.Count : allies.Count;
        for (int i=0; i< length;i++)
        {
            CharacterController character = findEnemy ? enemies[i] : allies[i];
            float sqDistance = Vector2.SqrMagnitude((Vector2)character.transform.position - currentPosition);
            if (sqDistance < minDistance && character.Health>0f)
            {
                minDistance = sqDistance;
                obj = character.transform;
            }
        }
        return obj;
    }

    /// <summary>
    /// Найти ближайшего противника на поле боя
    /// </summary>
    /// <param name="currentPosition">текущая позиция, откуда идёт запрос</param>
    /// <param name="findEnemy">Если true, то поиск будет происходить среди врагов, иначе - среди союзников</param>
    /// <param name="prevTarget">Следующий противник должен иметь Transform отличный от prevTarget</param>
    /// <returns>Игровой объект, представляющий живую потенциальную цель</returns>
    public Transform GetNearestCharacter(Vector2 currentPosition, bool findEnemy, Transform prevTarget)
    {
        float minDistance = Mathf.Infinity;
        Transform obj = null;
        int length = findEnemy ? enemies.Count : allies.Count;
        for (int i = 0; i < length; i++)
        {
            CharacterController character = findEnemy ? enemies[i] : allies[i];
            if (character.transform == prevTarget)
                continue;
            float sqDistance = Vector2.SqrMagnitude((Vector2)character.transform.position - currentPosition);
            if (sqDistance < minDistance && character.Health > 0f)
            {
                minDistance = sqDistance;
                obj = character.transform;
            }
        }
        return obj;
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
                ai.LoyaltyChangeEvent -= HandleAIChangeLoyaltyEvent;
                if (ai.Loyalty == LoyaltyEnum.ally)
                {
                    if (allies.Contains(ai))
                        allies.Remove(ai);
                }
                else
                {
                    ai.healthChangedEvent -= HandleEnemyDamageEvent;
                    ai.BehaviorChangeEvent -= HandleEnemyChangeBehaviorEvent;
                    if (enemies.Contains(ai))
                        enemies.Remove(ai);
                    if (agressiveEnemies.Contains(ai))
                        agressiveEnemies.Remove(ai);
                    CheckAgressiveEnemies();
                    if (enemies.Count == 0)
                        OnNoEnemiesRemains();
                }
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
            AIController furthestEnemy = null;
            foreach (AIController enemy in agressiveEnemies)
            {
                float _distance = Vector2.SqrMagnitude((Vector2)enemy.transform.position - enemy.MainTarget);
                if (_distance > maxDistance)
                {
                    maxDistance = _distance;
                    furthestEnemy = enemy;
                }
            }
            agressiveEnemies.Remove(furthestEnemy);
            furthestEnemy.Waiting = true;
        }
        else if (aECount < agressiveMonstersLimit && enemies.Count > aECount)
        {
            float minDistance = Mathf.Infinity;
            AIController nearestEnemy = null;
            foreach (AIController enemy in enemies)
                if (!agressiveEnemies.Contains(enemy))
                {
                    float _distance = Vector2.SqrMagnitude((Vector2)enemy.transform.position - enemy.MainTarget);
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
        agressiveEnemies = new List<AIController>();
        foreach (AIController ai in enemies)
        {
            ai.Waiting = true;
        }
        enemies.Sort((x, y) => { return Vector2.SqrMagnitude((Vector2)x.transform.position - x.MainTarget).CompareTo(Vector2.SqrMagnitude((Vector2)y.transform.position - y.MainTarget)); });
        for (int i = 0; i < agressiveMonstersLimit && i < enemies.Count; i++)
        {
            agressiveEnemies.Add(enemies[i]);
            enemies[i].Waiting = false;
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

    /// <summary>
    /// Функция, убивающая всех союзников
    /// </summary>
    public void KillAllies()
    {
        for (int i = 1; i < allies.Count; i++)//Героя эта функция не должна убивать, поэтому начинаем цикл с единицы
        {
            allies[i].TakeDamage(10000f, DamageType.Physical);
        }
    }

    public void ResetBattlefield()
    {
        //foreach (AIController enemy in enemies)
        //enemy.Waiting = false;
        KillAllies();
        agressiveEnemies.Clear();
        foreach (AIController enemy in enemies)
            enemy.ATrigger.TriggerOut();
        enemies.Clear();
    }
    
    #region events

    /// <summary>
    /// Событие "Больше врагов не осталось"
    /// </summary>
    protected virtual void OnNoEnemiesRemains()
    {
        if (NoEnemiesRemainEventHandler != null)
            NoEnemiesRemainEventHandler(this, new EventArgs());
    }

    #endregion //events

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
        ai.CharacterDeathEvent -= HandleEnemyDeathEvent;
        ai.healthChangedEvent -= HandleEnemyDamageEvent;
        ai.BehaviorChangeEvent -= HandleEnemyChangeBehaviorEvent;
        if (enemies.Contains(ai))
            enemies.Remove(ai);
        if (agressiveEnemies.Contains(ai))
        {
            agressiveEnemies.Remove(ai);
            CheckAgressiveEnemies();
        }
        if (allies.Contains(ai))
            allies.Remove(ai);
        if (enemies.Count == 0)
            OnNoEnemiesRemains();
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
            if (!enemies.Contains(ai))
            {
                enemies.Add(ai);
                agressiveEnemies.Add(ai);
                CheckAgressiveEnemies();
            }
        }
        else
        {
            if (enemies.Contains(ai))
                enemies.Remove(ai);
            if (agressiveEnemies.Contains(ai))
            {
                agressiveEnemies.Remove(ai);
                CheckAgressiveEnemies();
            }
            if (enemies.Count == 0)
                OnNoEnemiesRemains();
        }
    }

    /// <summary>
    /// Обработка события - противник сменил сторону конфликта
    /// </summary>
    /// <param name="sender">Что вызвало событие</param>
    /// <param name="e">Данные события</param>
    protected virtual void HandleAIChangeLoyaltyEvent(object sender, LoyaltyEventArgs e)
    {
        AIController ai = (AIController)sender;
        if (ai.Loyalty == e.Loyalty)
            return;//Враг и до этой функции так же лоялен к герою, поэтому нет смысла снова вызывать функцию
        if (e.Loyalty == LoyaltyEnum.ally)
        {
            if (enemies.Contains(ai))
                enemies.Remove(ai);
            if (agressiveEnemies.Contains(ai))
                agressiveEnemies.Remove(ai);
            if (!allies.Contains(ai))
                allies.Add(ai);
            ai.BehaviorChangeEvent -= HandleEnemyChangeBehaviorEvent;
            ai.healthChangedEvent -= HandleEnemyDamageEvent;
            if (enemies.Count == 0)
                OnNoEnemiesRemains();
        }
        else if (e.Loyalty == LoyaltyEnum.enemy)
        {
            if (ai.Behavior == BehaviorEnum.agressive)
            {
                if (!enemies.Contains(ai))
                    enemies.Add(ai);
                if (!agressiveEnemies.Contains(ai))
                    RefreshAgressiveEnemies();
            }
            if (allies.Contains(ai))
                allies.Remove(ai);
            ai.BehaviorChangeEvent += HandleEnemyChangeBehaviorEvent;
            ai.healthChangedEvent -= HandleEnemyDamageEvent;
            if (enemies.Count == 0)
                OnNoEnemiesRemains();
        }
    }

    #endregion //eventHandlers

}