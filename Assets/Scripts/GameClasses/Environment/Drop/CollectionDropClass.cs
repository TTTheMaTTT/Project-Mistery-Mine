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
            OnDropGet(new System.EventArgs());
            SpecialFunctions.statistics.ConsiderCollectionItem(item);
            Destroy(gameObject);
            SpecialFunctions.statistics.ConsiderStatistics(this);
            if (gameObject.layer == LayerMask.NameToLayer("hidden"))
                gameObject.layer = LayerMask.NameToLayer("drop");
            SpecialFunctions.gameController.PlaySound("Collection");
        }
    }

}
