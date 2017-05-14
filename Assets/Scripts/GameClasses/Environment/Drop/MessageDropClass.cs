using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Особый дроп, который начинает диалог при взятии
/// </summary>
public class MessageDropClass : DropClass
{

    #region fields

    [SerializeField]
    protected Dialog dialog;//Диалог, который начинается при взятии дропа

    #endregion //fields

    #region parametres

    [SerializeField]
    protected bool getItem = false;//Если true, то предмет подбирается при взятии дропа

    #endregion //parametres

    /// <summary>
    /// Начать диалог (в отличие от функции talk, эта функция не может вызваться при взаимодействии, а только при использовании сюжетного действия)
    /// </summary>
    protected virtual void StartDialog(Dialog _dialog)
    {
        SpecialFunctions.gameController.StartDialog(_dialog); 
    }

    #region IInteractive

    /// <summary>
    /// Провзаимодействовать с дропом
    /// </summary>
    public override void Interact()
    {
        if (dropped)
        {
            if (getItem)
                SpecialFunctions.Player.GetComponent<HeroController>().SetItem(item);
            if (dialog != null)
                StartDialog(dialog);
            OnDropGet(new EventArgs());
            Destroy(gameObject);
            SpecialFunctions.statistics.ConsiderStatistics(this);
            SpecialFunctions.StartStoryEvent(this, StoryDropIsGot, new StoryEventArgs());
            if (gameObject.layer == LayerMask.NameToLayer("hidden"))
                gameObject.layer = LayerMask.NameToLayer("drop");
            SpecialFunctions.gameController.PlaySound(dropSoundName);
        }
    }

    /// <summary>
    /// Можно ли провзаимодействовать с объектом в данный момент?
    /// </summary>
    public override bool IsInteractive()
    {
        return SpecialFunctions.battleField.enemiesCount == 0 && SpecialFunctions.dialogWindow.CurrentDialog == null;
    }

    #endregion //IInteractive

}
