using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт, задающий логику взаимодействия с кнопкой оружия в окне экипировки
/// </summary>
public class EquipmentCellScript : UIElementScript
{

    #region const

    protected const float inactiveIntensity = 1f, activeIntensity = .8f, clickedIntensity = .6f;//Как будет подкрашиваться кнопка при различных уровнях взаимодействия с ней

    #endregion const

    #region fields

    protected WeaponClass weapon;
    public WeaponClass Weapon
    {
        get
        {
            return weapon;
        }

        set
        {
            weapon = value;
            if (value != null)
            {
                weaponImage.sprite = value.itemImage;
                weaponImage.color = Color.white;
                button.enabled = true;
            }
            else
            {
                button.enabled = false;//Кнопка неактивна, если нет оружия
                weaponImage.sprite = null;
                weaponImage.color = new Color(0f, 0f, 0f, 0f);
            }
        }
    }

    protected Image weaponImage;
    protected Image buttonImage;
    protected Image cellImage;
    protected Button button;
    protected EquipmentMenu eMenu;

    protected bool activated = false;
    protected bool isCurrentWeapon = false;
    public bool Activated
    {
        get
        {
            return activated;
        }
        set
        {
            if (value)
                eMenu.ResetActivatedWeaponCell();
            eMenu.ActivatedWeaponCell = this;
            button.enabled = !value;
            buttonImage.color = value ? new Color(0f, 0.67f, 0.72f, 0.5f) : new Color(0f, 0f, 0f, 0f);
            activated = value;
        }
    }

    public bool IsCurrentWeapon
    {
        get
        {
            return isCurrentWeapon;
        }
        set
        {
            if (value)
                eMenu.ResetCurrentWeaponCell();
            activated = false;
            eMenu.ActivatedWeaponCell = null;
            eMenu.currentWeaponCell = this;
            button.enabled = !value && weapon!=null;
            buttonImage.color = value ? new Color(0f, 1f, 0.5f, 0.5f) : new Color(0f, 0f, 0f, 0f);
            isCurrentWeapon = value;
            SetInactive();
        }
    }

    public override UIElementStateEnum ElementState
    {
        get
        {
            return base.ElementState;
        }

        set
        {
            if (isCurrentWeapon || weapon==null)
                return;
            base.ElementState = value;
            switch (value)
            {
                case UIElementStateEnum.inactive:
                    cellImage.color = new Color(inactiveIntensity, inactiveIntensity, inactiveIntensity, 1f);
                    break;
                case UIElementStateEnum.active:
                    cellImage.color = new Color(activeIntensity, activeIntensity, activeIntensity, 1f);
                    break;
                case UIElementStateEnum.clicked:
                    cellImage.color = new Color(clickedIntensity, clickedIntensity, clickedIntensity, 1f);
                    break;
                default:
                    cellImage.color = new Color(inactiveIntensity, inactiveIntensity, inactiveIntensity, 1f);
                    break;
            }
        }
    }

    public static Text weaponNameText;//Текст, на котором отображается название оружия

    #endregion //fields

    public void Initialize(EquipmentMenu _eMenu)
    {
        activated = false;
        isCurrentWeapon = false;
        buttonImage = GetComponent<Image>();
        button = GetComponent<Button>();
        cellImage = transform.parent.GetComponent<Image>();
        weaponImage = transform.parent.FindChild("WeaponImage").GetComponent<Image>();
        button.enabled = false;
        eMenu = _eMenu;
    }

    /// <summary>
    /// Активировать ячейку и выбрать оружие
    /// </summary>
    public void SetEquipmentCellActive()
    {
        if (weapon!=null)
            Activated = true;
    }

    /// <summary>
    /// Активировать данную кнопку
    /// </summary>
    public override void Activate()
    {
        if (isCurrentWeapon)
            return;
        base.Activate();
        button.onClick.Invoke();
        SetActive();
    }

    /// <summary>
    /// Отобразить текст с названием оружия
    /// </summary>
    public void ShowWeaponText()
    {
        if (weaponNameText != null && weapon != null)
            weaponNameText.text = weapon.itemTextName;
    }


}
