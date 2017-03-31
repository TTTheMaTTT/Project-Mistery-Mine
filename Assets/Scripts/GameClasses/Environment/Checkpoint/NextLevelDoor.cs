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
    /// Что произойдёт, если открыть дверь
    /// </summary>
    public override void Open()
    {
        base.Open();
        StartCoroutine(NextLevelProcess());
    }

    /// <summary>
    /// Процесс перехода на следующий уровень
    /// </summary>
    protected IEnumerator NextLevelProcess()
    {
        SpecialFunctions.gameController.RemoveHeroDeathLevelEnd();
        PlayerPrefs.SetInt("Checkpoint Number", checkpointNumber);
        SpecialFunctions.gameController.SaveGame(checkpointNumber,true, nextLevelName);
        PlayerPrefs.SetFloat("Hero Health", SpecialFunctions.Player.GetComponent<HeroController>().MaxHealth);
        SpecialFunctions.SetFade(true);
        yield return new WaitForSeconds(nextLevelTime);
        if (nextLevelName != string.Empty)
            SpecialFunctions.gameController.CompleteLevel(nextLevelName);
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
