using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Дверь, при взаимодействии с которой начинается диалог
/// </summary>
public class DialogDoor : DoorClass
{

    #region fields

    [SerializeField]
    protected Dialog dialog;

    #endregion //fields

    /// <summary>
    /// Провзаимодействовать
    /// </summary>
    public override void Interact()
    {
        if (col.enabled)
            SpecialFunctions.dialogWindow.BeginDialog(dialog);
    }

    /// <summary>
    /// Можно ли вообще взаимодействовать с объектом?
    /// </summary>
    public override bool IsInteractive()
    {
        return col.enabled && SpecialFunctions.battleField.enemiesCount<=0;
    }


}
