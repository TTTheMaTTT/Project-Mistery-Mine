using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Дверь от гробницы
/// </summary>
public class TombDoorClass : DoorClass
{

    #region fields

    private TombRiddleWindowScript riddleWindow;

    #endregion //fields

    protected override void Awake()
    {
        base.Awake();
        riddleWindow = FindObjectOfType<TombRiddleWindowScript>();
    }

    #region IInteractive

    public override void Interact()
    {
        if (riddleWindow != null)
        {
            EquipmentClass equip = SpecialFunctions.player.GetComponent<HeroController>().Equipment;
            if (equip.GetItem("FragmentOrdo") != null && equip.GetItem("FragmentPerditio") && equip.GetItem("FragmentAqua") && equip.GetItem("FragmentIgnus"))
                riddleWindow.OpenWindow();
            else
                SpecialFunctions.SetText(closedDoorMessage, 2.5f);
        }
    }

    #endregion //IInteractive

}
