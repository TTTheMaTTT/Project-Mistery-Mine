using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Дверь, служащая переходом на следующий уровень
/// </summary>
public class NextLevelDoor : DoorClass
{

    #region consts

    protected const float nextLevelTime = 2.1f;//Время, за которое происходит переход на следующий уровень
    protected const string battleMessage = "Вы не можете воспользоваться дверью, пока находитесь в бою";

    #endregion //consts

    #region parametres

    [SerializeField]
    protected string nextLevelName;//Следующий уровень, на который произойдёт переход

    public int checkpointNumber = 0;//Чекпоинт следующего уровня, который связан с этим проходом

    [SerializeField]protected bool closedByMechanism = false;//Есть двери, которые открываются только при деактивации запирающего механизма
    [SerializeField]protected bool opened = false;//Открыта ли дверь

    #endregion //parametres

    /// <summary>
    /// 
    /// </summary>
    public override void Interact()
    {
        if (SpecialFunctions.battleField.enemiesCount > 0)
        {
            SpecialFunctions.SetText(battleMessage, 2.5f);
            return;
        }
        HeroController player = SpecialFunctions.Player.GetComponent<HeroController>();
        if (!closedByMechanism || opened)
        {
            if (keyID == string.Empty)
            {
                Open();
            }
            else if (player.Equipment.bag.Find(x => x.itemName == keyID))
                Open();
            else
                SpecialFunctions.SetText(closedDoorMessage, 2.5f);
        }
        else
            SpecialFunctions.SetText(closedDoorMessage, 2.5f);
    }

    /// <summary>
    /// Можно ли провзаимодействовать с объектом в данный момент?
    /// </summary>
    public override bool IsInteractive()
    {
        return SpecialFunctions.dialogWindow.CurrentDialog == null;
    }

    /// <summary>
    /// Что произойдёт, если открыть дверь
    /// </summary>
    public override void Open()
    {
        base.Open();
        SpecialFunctions.gameController.CompleteLevel(nextLevelName, true, checkpointNumber);
    }


    /// <summary>
    /// Активировать механизм
    /// </summary>
    public override void ActivateMechanism()
    {
        opened = true;
    }

    /// <summary>
    /// Загрузить данные о двери 
    /// </summary>
    public override void SetData(InterObjData _intObjData)
    {
        DoorData dData = (DoorData)_intObjData;
        if (dData != null)
        {
            opened = dData.opened;
        }
    }

    /// <summary>
    /// Сохранить данные о двери
    /// </summary>
    public override InterObjData GetData()
    {
        DoorData dData = new DoorData(id, opened, gameObject.name);
        return dData;
    } 

}
