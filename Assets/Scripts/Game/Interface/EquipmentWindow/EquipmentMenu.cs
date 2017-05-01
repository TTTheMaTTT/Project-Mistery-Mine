using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Окошко, в котором выбирается вооружение персонажа
/// </summary>
public class EquipmentMenu : InterfaceWindow
{

    #region fields

    HeroController hero;

    private GameObject weaponsPanel, trinketsPanel, itemsPanel;//Три окошка меню инвентаря: оружия, особые предметы, предметы

    #region weapons

    [HideInInspector]
    private static EquipmentCellScript activatedWeaponCell;
    public EquipmentCellScript ActivatedWeaponCell { set { activatedWeaponCell = value; ShowChangeWeaponButton(value != null); } }
    [HideInInspector]
    public EquipmentCellScript currentWeaponCell;
    List<string> weapons = new List<string>();
    List<EquipmentCellScript> weaponCells = new List<EquipmentCellScript>();
    GameObject changeWeaponButton;
    UIElementScript changeWeaponUIElement, returnWeaponUIElement;
    private Text weaponNameText;

    #endregion //weapons

    #region trinkets

    private List<ActiveTrinketCell> activeTrinketCells = new List<ActiveTrinketCell>();//Список ячеек с надетыми тринкетами
    private List<TrinketCell> trinketCells = new List<TrinketCell>();//Список ячеек с тринкетами
    private GameObject removeTrinketButtonObj;//Кнопка снятия тринкета
    private UIElementScript removeTrinketUIElement, returnTrinketUIElement;
    private List<string> trinkets = new List<string>();//Список названий имеющихся тринкетов (исключает повторения)
    private Text trinketNameText;
    private ActiveTrinketsPanel activeTrinketsUIPanel;

    #endregion //trinkets

    #region items

    private List<ItemCell> itemCells = new List<ItemCell>();//Список ячеек с предметами
    private Text itemNameText;
    private Text goldHeartCountText;

    #endregion //items

    #endregion //fields

    #region parametres

    private int weaponNumber = 0;
    private bool dontChange = false;//Предотвращает лишний вызов ChangeWeapon(WeaponClass _weapon) при вызове ChangeWeapon()
    private int goldHeartShardsCount = 0;//Кол-во осколков золотого сердца
    private int magicSlotsCount = 1;//Сколько одновременно тринкетов может использовать герой 

    #endregion //parametres

    public override void Initialize()
    {
        base.Initialize();

        Transform panel = transform.FindChild("Panel");
        weaponsPanel = panel.FindChild("WeaponsPanel").gameObject;
        trinketsPanel = panel.FindChild("TrinketsPanel").gameObject;
        itemsPanel = panel.FindChild("ItemsPanel").gameObject;
        hero = SpecialFunctions.Player.GetComponent<HeroController>();
        hero.equipmentChangedEvent += HandleEquipmentChanges;

        #region weapons

        weapons = new List<string>();
        activatedWeaponCell = null;
        currentWeaponCell = null;
        changeWeaponButton = weaponsPanel.transform.FindChild("ChangeWeaponButton").gameObject;
        changeWeaponUIElement = changeWeaponButton.GetComponent<UIElementScript>();
        returnWeaponUIElement = weaponsPanel.transform.FindChild("ReturnButton").GetComponent<UIElementScript>();
        ShowChangeWeaponButton(false);
        Transform cells = weaponsPanel.transform.FindChild("Cells");
        weaponCells = new List<EquipmentCellScript>();
        for (int i = 0; i < cells.childCount; i++)
        {
            EquipmentCellScript eCell = cells.GetChild(i).GetComponentInChildren<EquipmentCellScript>();
            eCell.Initialize(this);
            weaponCells.Add(eCell);
        }
        weaponNameText = weaponsPanel.transform.FindChild("WeaponNameText").GetComponent<Text>();
        EquipmentCellScript.weaponNameText = weaponNameText;
        ClearWeaponCells();
        AddWeapon(hero.CurrentWeapon);
        ChangeWeapon(hero.CurrentWeapon);

        #endregion //weapons

        #region trinkets

        trinketCells = new List<TrinketCell>();
        activeTrinketCells = new List<ActiveTrinketCell>();
        removeTrinketButtonObj = trinketsPanel.transform.FindChild("RemoveTrinketButton").gameObject;
        removeTrinketUIElement = removeTrinketButtonObj.GetComponent<UIElementScript>();
        returnTrinketUIElement = trinketsPanel.transform.FindChild("ReturnButton").GetComponent<UIElementScript>();
        ShowRemoveTrinketButton(false);
        trinketNameText = trinketsPanel.transform.FindChild("TrinketNameText").GetComponent<Text>();
        Transform activeTrinketsPanel = trinketsPanel.transform.FindChild("ActiveTrinketsPanel");
        activeTrinketsUIPanel = activeTrinketsPanel.GetComponent<ActiveTrinketsPanel>();
        for (int i = 0; i < activeTrinketsPanel.childCount; i++)
        {
            ActiveTrinketCell tCell = activeTrinketsPanel.GetChild(i).GetComponentInChildren<ActiveTrinketCell>();
            tCell.Initialize();
            activeTrinketCells.Add(tCell);
        }
        ActiveTrinketCell.eMenu = this;
        ActiveTrinketCell.trinketNameText = trinketNameText;

        Transform usualTrinketsPanel = trinketsPanel.transform.FindChild("TrinketsPanel");
        for (int i = 0; i < usualTrinketsPanel.childCount; i++)
        {
            TrinketCell tCell = usualTrinketsPanel.GetChild(i).GetComponentInChildren<TrinketCell>();
            tCell.Initialize();
            trinketCells.Add(tCell);
        }
        TrinketCell.trinketNameText = trinketNameText;
        TrinketCell.cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        ClearTrinketCells();

        #endregion //trinkets

        #region items

        goldHeartShardsCount = 0;
        goldHeartCountText = itemsPanel.transform.FindChild("GoldHeartPanel").GetComponentInChildren<Text>();
        goldHeartCountText.text = "0/5";
        itemNameText = itemsPanel.transform.FindChild("ItemNameText").GetComponent<Text>();
        itemCells = new List<ItemCell>();
        Transform itemCellsTrans = itemsPanel.transform.FindChild("ItemCells");
        for (int i = 0; i < itemCellsTrans.childCount; i++)
        {
            ItemCell iCell = itemCellsTrans.GetChild(i).GetComponentInChildren < ItemCell>();
            iCell.Initialize();
            itemCells.Add(iCell);
        }
        ItemCell.itemNameText = itemNameText;
        ClearItemCells();

        #endregion //items

    }

    /// <summary>
    /// Закрыть окно интерфейса
    /// </summary>
    public override void CloseWindow()
    {
        if (!canClose)
            return;
        base.CloseWindow();
        if (activatedWeaponCell != null)
        {
            activatedWeaponCell.Activated = false;
            activatedWeaponCell = null;
        }
        ResetActiveSlots();
        SpecialFunctions.gameController.ChangeInformationAboutEquipment(SpecialFunctions.levelEnd);
        StartCoroutine(CantInteractProcess());
    }

    public override void OpenWindow()
    {
        if (!canOpen)
            return;
        base.OpenWindow();
        if (currentWeaponCell != null)
        {
            currentWeaponCell.IsCurrentWeapon = true;
        }
        OpenPanel("weapon");
        StartCoroutine(CantInteractProcess());
    }

    /// <summary>
    /// Открыть определённую панель и закрыть остальные
    /// </summary>
    public void OpenPanel(string panelType)
    {
        ResetActiveSlots();
        UIElementScript nextPanel = null;
        switch (panelType)
        {
            case "weapon":
                nextPanel=weaponsPanel.GetComponent<UIPanel>();
                break;
            case "trinket":
                nextPanel = trinketsPanel.GetComponent<UIPanel>();
                break;
            case "item":
                nextPanel = itemsPanel.GetComponent<UIPanel>();
                break;
        }
        if (!nextPanel)
            return;
        nextPanel.SetActive();
        currentIndex = nextPanel.uiIndex;
        UIElementScript prevPanel = FindElement(currentIndex);
        childElements[childElements.IndexOf(prevPanel)] = nextPanel;
        weaponsPanel.SetActive(panelType == "weapon");
        trinketsPanel.SetActive(panelType == "trinket");
        itemsPanel.SetActive(panelType == "item");
    }

    /// <summary>
    /// Сброс активной клетки оружия
    /// </summary>
    public void ResetActivatedWeaponCell()
    {
        if (activatedWeaponCell)
        {
            activatedWeaponCell.Activated = false;
            activatedWeaponCell = null;
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
        activatedWeaponCell = _cell;
    }

    /// <summary>
    /// Очистить все клетки с оружием
    /// </summary>
    public void ClearWeaponCells()
    {
        weaponNumber = 0;
        weapons = new List<string>();
        foreach (EquipmentCellScript _cell in weaponCells)
            _cell.Weapon = null;
        if (hero!=null)
            AddWeapon(hero.CurrentWeapon);
    }

    /// <summary>
    /// Очистить все клетки с тринкетами
    /// </summary>
    public void ClearTrinketCells()
    {
        trinkets = new List<string>();
        magicSlotsCount = 1;
        foreach (TrinketCell tCell in trinketCells)
            tCell.Trinket = null;
        for (int i = 0; i < activeTrinketCells.Count; i++)
        {
            ActiveTrinketCell tCell = activeTrinketCells[i];
            tCell.SetTrinket(null);
            if (i >= magicSlotsCount)
                tCell.transform.parent.gameObject.SetActive(false);
        }

        activeTrinketsUIPanel.SetActiveChildElements();

    }

    /// <summary>
    /// Очистить все клетки с предметами
    /// </summary>
    public void ClearItemCells()
    {
        foreach (ItemCell iCell in itemCells)
            iCell.Item = null;
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
        weaponCells[weaponNumber].Weapon = _weapon;
        weaponNumber++;
    }

    /// <summary>
    /// Сменить оружие у главного героя
    /// </summary>
    public void ChangeWeapon()
    {
        ShowChangeWeaponButton(false);
        if (activatedWeaponCell == null)
            return;
        dontChange = true;
        hero.CurrentWeapon = activatedWeaponCell.Weapon;
        dontChange = false;
        activatedWeaponCell.IsCurrentWeapon = true;
    }

    /// <summary>
    /// Отобразить (или скрыть) кнопку смены оружия
    /// </summary>
    public void ShowChangeWeaponButton(bool _show)
    {

        changeWeaponButton.SetActive(_show);
        if (_show)
        {
            changeWeaponUIElement.uiIndex = new UIElementIndex(1, 0);
            returnWeaponUIElement.uiIndex = new UIElementIndex(1, 1);
        }
        else
        {
            changeWeaponUIElement.uiIndex = new UIElementIndex(-1, -1);
            returnWeaponUIElement.uiIndex = new UIElementIndex(1, 0);
        }
    }

    /// <summary>
    /// Добавить тринкет в ячейки для тринкетов
    /// </summary>
    public void AddTrinket(TrinketClass _trinket)
    {
        if (trinkets.Contains(_trinket.itemName))
            return;
        foreach (TrinketCell tCell in trinketCells)
            if (tCell.Trinket == null)
            {
                tCell.Trinket = _trinket;
                break;
            }
    }

    /// <summary>
    /// Добавить тринкет в ячейки активных тринкетов
    /// </summary>
    public void SetActiveTrinket(TrinketClass _trinket)
    {
        if (_trinket == null)
            return;
        if (_trinket is MutagenClass)
        {
            AddMutagen((MutagenClass)_trinket);
            return;
        }
        foreach (ActiveTrinketCell tCell in activeTrinketCells)
            if (tCell.Trinket != null ? tCell.Trinket.itemName == _trinket.itemName:false)
                return;
        foreach (ActiveTrinketCell tCell in activeTrinketCells)
            if (!tCell.Trinket)
                tCell.Trinket = _trinket;
    }

    /// <summary>
    /// Добавить мутаген в активные ячейки тринкетов
    /// </summary>
    /// <param name="_mutagen"></param>
    public void AddMutagen(MutagenClass _mutagen)
    {
        if (trinkets.Contains(_mutagen.itemName))
            return;
        magicSlotsCount++;
        List<TrinketClass> currentTrinkets = new List<TrinketClass>();
        foreach (ActiveTrinketCell tCell in activeTrinketCells)
        {
            if (tCell.Trinket)
            {
                currentTrinkets.Add(tCell.Trinket);
                tCell.SetTrinket(null);
            }
        }
        activeTrinketCells[0].SetTrinket(_mutagen);
        for (int i = 0; i < currentTrinkets.Count; i++)
            activeTrinketCells[i+1].SetTrinket(currentTrinkets[i]);
        for (int i = 0; i < activeTrinketCells.Count; i++)
            activeTrinketCells[i].transform.parent.gameObject.SetActive(i < magicSlotsCount);
    }

    /// <summary>
    /// Снять тринкет
    /// </summary>
    public void TakeOffTrinket(TrinketClass _trinket)
    {
        SpecialFunctions.gameController.RemoveEffectsOfTrinket(_trinket);
    }

    /// <summary>
    /// Надеть тринкет
    /// </summary>
    public void PutOnTrinket(TrinketClass _trinket)
    {
        SpecialFunctions.gameController.AddTrinketEffect(_trinket);
    }

    /// <summary>
    /// Снять выделенный тринкет
    /// </summary>
    public void TakeOffActivatedTrinket()
    {
        if (ActiveTrinketCell.activatedTrinketCell)
        {
            TakeOffTrinket(ActiveTrinketCell.activatedTrinketCell.Trinket);
            ActiveTrinketCell.activatedTrinketCell.Trinket = null; 
        }
    }

    /// <summary>
    /// Отобразить (или скрыть) кнопку снимания тринкета
    /// </summary>
    public void ShowRemoveTrinketButton(bool _show)
    {
        
        removeTrinketButtonObj.SetActive(_show);
        if (_show)
        {
            removeTrinketUIElement.uiIndex = new UIElementIndex(1, 1);
            returnTrinketUIElement.uiIndex = new UIElementIndex(1, 2);
        }
        else
        {
            removeTrinketUIElement.uiIndex = new UIElementIndex(-1, -1);
            returnTrinketUIElement.uiIndex = new UIElementIndex(1, 1);
        }
    }

    /// <summary>
    /// Добавить предмет в ячейки предметов
    /// </summary>
    public void AddItem(ItemClass _item)
    {
        if (_item == null)
            return;
        ItemCell iCell = itemCells.Find(x => x.Item == null);
        if (iCell != null)
            iCell.Item = _item;
    }

    /// <summary>
    /// Убрать предмет из списка предметов
    /// </summary>
    public void RemoveItem(ItemClass _item)
    {
        if (_item == null)
            return;
        ItemCell iCell = itemCells.Find(x => (x.Item != null ? x.Item.itemName == _item.itemName : false));
        if (iCell == null)
            return;
        int index = itemCells.IndexOf(iCell);
        for (int i = 0; i < itemCells.Count - 1; i++)
            itemCells[i].Item = itemCells[i + 1].Item;
        itemCells[itemCells.Count - 1].Item = null;
    }

    /// <summary>
    /// Учесть заполучение персонажем золотого сердца
    /// </summary>
    public void ConsiderGoldHeart(ItemClass _item)
    {
        if (_item == null)
            return;
        if (_item.itemName == "GoldHeartShard")
        {
            goldHeartShardsCount++;
            if (goldHeartShardsCount >= 5)
                goldHeartShardsCount = 0;
            goldHeartCountText.text = goldHeartShardsCount.ToString() + "/5";
        }
    }

    /// <summary>
    /// Учесть заполучение персонажем страницы из книги жизни
    /// </summary>
    public void ConsiderLifeBookPage(ItemClass _item)
    {
        if (_item == null)
            return;
        if (_item.itemName != "LifeBookPage")
            return;
        magicSlotsCount++;
        for (int i = 0; i < activeTrinketCells.Count; i++)
            activeTrinketCells[i].transform.parent.gameObject.SetActive(i < magicSlotsCount);

        activeTrinketsUIPanel.SetActiveChildElements();
    }

    /// <summary>
    /// Сбросить все активированные слоты меню инвентаря
    /// </summary>
    public void ResetActiveSlots()
    {
        if (activatedWeaponCell != null)
        {
            activatedWeaponCell.Activated = false;
            activatedWeaponCell = null;
        }
        if (TrinketCell.activeTrinketCell)
            TrinketCell.activeTrinketCell.Activated = false;
        TrinketCell.draggedTrinket = null;
        if (ActiveTrinketCell.activatedTrinketCell)
            ActiveTrinketCell.activatedTrinketCell.Activated = false;
        removeTrinketButtonObj.SetActive(false);
        weaponNameText.text = "";
        trinketNameText.text = "";
        itemNameText.text = "";
    }

    /// <summary>
    /// Функция, которая заставляет окно инвентаря считать за активное оружие то, что указано в аргументах функции
    /// </summary>
    /// <param name="_weapon"></param>
    void ChangeWeapon(WeaponClass _weapon)
    {
        if (_weapon == null)
            return;
        for (int i = 0; i < weaponNumber; i++)
            if (weaponCells[i].Weapon.itemName == _weapon.itemName)
            {
                weaponCells[i].IsCurrentWeapon=true;
                ResetActivatedWeaponCell();
                break;
            }
    }

    /// <summary>
    /// Учесть смену главного героя
    /// </summary>
    public void ConsiderPlayer(HeroController _hero)
    {
        hero.equipmentChangedEvent -= HandleEquipmentChanges;
        hero = _hero;
        _hero.equipmentChangedEvent += HandleEquipmentChanges;
    }

    #region eventHandlers

    /// <summary>
    /// Обработать событие "Инвентарь изменился"
    /// </summary>
    protected virtual void HandleEquipmentChanges(object sender, EquipmentEventArgs e)
    {
        if (e.CurrentWeapon != null && !dontChange)
            ChangeWeapon(e.CurrentWeapon);
        else if (e.Item != null)
        {
            if (e.Item is WeaponClass)
                AddWeapon((WeaponClass)e.Item);
            else if (e.Item is MutagenClass)
                AddMutagen((MutagenClass)e.Item);
            else if (e.Item is TrinketClass)
                AddTrinket((TrinketClass)e.Item);
            else if (e.Item.itemName == "GoldHeartShard")
                ConsiderGoldHeart(e.Item);
            else if (e.Item.itemName == "LifeBookPage")
                ConsiderLifeBookPage(e.Item);
            else
                AddItem(e.Item);
        }
        if (e.RemovedItem != null)
            RemoveItem(e.RemovedItem);
    }

    #endregion //eventHandlers


}
