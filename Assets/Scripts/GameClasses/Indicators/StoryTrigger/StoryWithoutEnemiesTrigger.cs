using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Особый триггер сюжетного события, который может сработать только если нет агрессивных противников
/// </summary>
public class StoryWithoutEnemiesTrigger : StoryTrigger
{

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "player")
        {
            if (SpecialFunctions.battleField.enemiesCount > 0)
            {
                SpecialFunctions.battleField.NoEnemiesRemainEventHandler += HandleNoEnemiesRemain;
                return;
            }
            SpecialFunctions.StartStoryEvent(this, TriggerEvent, new StoryEventArgs());
            if (!triggered)
            {
                triggered = true;
                SpecialFunctions.statistics.ConsiderStatistics(this);
            }
                
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag != "player")
            return;
        if (SpecialFunctions.battleField.enemiesCount > 0)
            SpecialFunctions.battleField.NoEnemiesRemainEventHandler -= HandleNoEnemiesRemain;
    }

    #region eventHandlers

    /// <summary>
    ///Обработчик события "Врагов больше не осталось"
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void HandleNoEnemiesRemain(object sender, EventArgs e)
    {
        Vector2 heroPoint = SpecialFunctions.Player.transform.position;
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (Collider2D col in cols)
            if (col.OverlapPoint(heroPoint))
            {
                SpecialFunctions.StartStoryEvent(this, TriggerEvent, new StoryEventArgs());
                if (!triggered)
                {
                    triggered = true;
                    SpecialFunctions.statistics.ConsiderStatistics(this);
                }
                break;
            }
        SpecialFunctions.battleField.NoEnemiesRemainEventHandler -= HandleNoEnemiesRemain;
    }

    #endregion //eventHandlers

}
