using UnityEngine;
using System.Collections;

/// <summary>
/// Специальный дроп, который содержит в себе коллекционный предмет. Он не попадает в инвентарь, а в специальное место для коллекций - экран коллекций
/// </summary>
public class CollectionDropClass : DropClass
{

    /// <summary>
    /// Взаимодействие с таким типом объектов.
    /// </summary>
    public override void Interact()
    {
        if (dropped)
        {
            SpecialFunctions.statistics.ConsiderCollectionItem(item);
            Destroy(gameObject);
            SpecialFunctions.statistics.ConsiderStatistics(this);
        }
    }

}
