using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Окошко, в котором выбирается вооружение персонажа
/// </summary>
public class EquipmentMenu : InterfaceWindow
{

    #region fields

    [HideInInspector]private EquipmentCellScript activatedCell;
    public EquipmentCellScript ActivatedCell { set { activatedCell = value; changeWeaponButton.SetActive(value != null); } }
    [HideInInspector]public EquipmentCellScript currentWeaponCell;

    HeroController hero;
    List<string> weapons = new List<string>();
    List<EquipmentCellScript> equipCells = new List<EquipmentCellScript>();
    GameObject changeWeaponButton;

    #endregion //fields

    #region parametres

    int weaponNumber = 0;
    bool dontChange = false;//Предотвращает лишний вызов ChangeWeapon(WeaponClass _weapon) при вызове ChangeWeapon()

    #endregion //parametres

    protected override void Awake()
    {
        base.Awake();
        weapons = new List<string>();
        activatedCell = null;
        currentWeaponCell = null;
        hero = SpecialFunctions.Player.GetComponent<HeroController>();
        Transform panel = transform.FindChild("Panel");
        changeWeaponButton = panel.FindChild("ChangeWeaponButton").gameObject;
        changeWeaponButton.SetActive(false);
        Transform cells = panel.FindChild("Cells");
        equipCells = new List<EquipmentCellScript>();
        for (int i = 0; i < cells.childCount; i++)
        {
            EquipmentCellScript eCell = cells.GetChild(i).GetComponentInChildren<EquipmentCellScript>();
            eCell.Initialize();
            equipCells.Add(eCell);
        }
        ClearCells();
        AddWeapon(hero.CurrentWeapon);
        ChangeWeapon(hero.CurrentWeapon);
        hero.equipmentChangedEvent += HandleEquipmentChanges;
    }

    /// <summary>
    /// Закрыть окно интерфейса
    /// </summary>
    public override void CloseWindow()
    {
        base.CloseWindow();
        if (activatedCell != null)
        {
            activatedCell.Activated = false;
            activatedCell = null;
        }
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        if (currentWeaponCell != null)
        {
            currentWeaponCell.IsCurrentWeapon = true;
        }
    }

    /// <summary>
    /// Сброс активной клетки оружия
    /// </summary>
    public void ResetActivatedCell()
    {
        if (activatedCell)
        {
            activatedCell.Activated = false;
            activatedCell = null;
        }
    }

    /// <summary>
    /// Сброс клетки текущего оружия
    /// </summary>
    public void ResetCurrentWeaponCell()
    {
        if (currentWeaponCell)
        {
            currentWeaponCell.IsCurrentWeapon = false;
            currentWeaponCell = null;
        }
    }

    /// <summary>
    /// Активировать ячейку с оружием
    /// </summary>
    /// <param name="_cell">Ячейка с оружием</param>
    public void ActivateCell(EquipmentCellScript _cell)
    {
        _cell.Activated = true;
        activatedCell = _cell;
    }

    /// <summary>
    /// Очистить все клетки с оружием
    /// </summary>
    public void ClearCells()
    {
        weaponNumber = 0;
        weapons = new List<string>();
        foreach (EquipmentCellScript _cell in equipCells)
            _cell.Weapon = null;
        AddWeapon(hero.CurrentWeapon);
    }

    /// <summary>
    /// Добавить оружие
    /// </summary>
    void AddWeapon(WeaponClass _weapon)
    {
        if (_weapon == null)
            return;
        if (weapons.Contains(_weapon.itemName))
            return;
        weapons.Add(_weapon.itemName);
        equipCells[weaponNumber].Weapon = _weapon;
        weaponNumber++;
    }

    /// <summary>
    /// Сменить оружие у главного героя
    /// </summary>
    public void ChangeWeapon()
    {
        changeWeaponButton.SetActive(false);
        if (activatedCell == null)
            return;
        dontChange = true;
        hero.CurrentWeapon = activatedCell.Weapon;
        dontChange = false;
        activatedCell.IsCurrentWeapon = true;
        if (SpecialFunctions.levelEnd)
            SpecialFunctions.gameController.SaveGame(0, true, SpecialFunctions.nextLevelName);
    }

    void ChangeWeapon(WeaponClass _weapon)
    {
        if (_weapon == null)
            return;
        for (int i = 0; i < weaponNumber; i++)
            if (equipCells[i].Weapon == _weapon)
            {
                equipCells[i].IsCurrentWeapon=true;
                ResetActivatedCell();
                break;
            }
    }

    #region eventHandlers

    /// <summary>
    /// Обработать событие "Инвентарь изменился"
    /// </summary>
    protected virtual void HandleEquipmentChanges(object sender, EquipmentEventArgs e)
    {
        if (e.CurrentWeapon != null && !dontChange)
            ChangeWeapon(e.CurrentWeapon);
        else if (e.Item != null && e.Item is WeaponClass)
            AddWeapon((WeaponClass)e.Item);
    }

    #endregion //eventHandlers


}
