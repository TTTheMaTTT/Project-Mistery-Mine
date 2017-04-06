using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт, задающий логику взаимодействия с кнопкой оружия в окне экипировки
/// </summary>
public class EquipmentCellScript : MonoBehaviour
{

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
            buttonImage.color = value ? new Color(1f, 0.8f, 0f, 0.5f) : new Color(0f, 0f, 0f, 0f);
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
        weaponImage = transform.parent.FindChild("WeaponImage").GetComponent<Image>();
        eMenu = _eMenu;
    }

    /// <summary>
    /// Активировать ячейку и выбрать оружие
    /// </summary>
    public void SetActive()
    {
        Activated = true;
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
