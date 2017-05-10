using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

public class InputCollection : MonoBehaviour
{

    #region consts

    private const float initializeTime = 1f;

    #endregion //consts

    public static InputCollection instance;

    public List<InputDataClass> inputData = new List<InputDataClass>();
    //private bool initialized;

    public void Start()
    {
        StartCoroutine("InitializeProcess");
        if (instance == null)
            instance = this;
        else
        {
            Destroy(this);
            return;
        }
        if (InputManager.ActiveDevice != null)
            InitializeData();
        InputManager.OnActiveDeviceChanged += inputData => StartCoroutine("InitializeProcess");
        InputManager.OnDeviceAttached += inputDevice => StartCoroutine("InitializeProcess"); 
    }

    public void StartInitializeProcess()
    {
        StartCoroutine("InitializeProcess");
    }


    /// <summary>
    /// Процесс инициализации управления
    /// </summary>
    IEnumerator InitializeProcess()
    {
        yield return new WaitForSeconds(initializeTime);
        if (InputManager.ActiveDevice != null)
            InitializeData();
        InputManager.OnDeviceAttached += inputDevice => InitializeData();
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSecondsRealtime(initializeTime);
            if (InputManager.ActiveDevice != null)
                InitializeData();
        }
    }

/*    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            InitializeData();
    }*/

    /// <summary>
    /// Настроить элементы управления
    /// </summary>
    public void InitializeData()
    {
        foreach (InputDataClass _data in inputData)
        {
            _data.ClearInputData();
            _data.SetControl();
        }
    }

    /// <summary>
    /// Возвращает класс, ответственный за управление при помощи конкретного элемента геймпада
    /// </summary>
    /// <param name="_inputName"></param>
    /// <returns></returns>
    public InputControl GetInputControl(string _inputName)
    {
        InputDataClass _inputData = inputData.Find(x => x.inputName == _inputName);
        return _inputData != null ? _inputData.joystickControl : null;
    }
        
    /// <summary>
    /// Нажат ли элемент управления с данным названием?
    /// </summary>
    public bool GetButtonDown(string _inputName)
    {
        InputDataClass _inputData = inputData.Find(x => x.inputName == _inputName);
        return _inputData != null ? _inputData.GetButtonDown() : false;
    }

    /// <summary>
    /// Зажат ли элемент управления с данным названием?
    /// </summary>
    public bool GetButton(string _inputName)
    {
        InputDataClass _inputData = inputData.Find(x => x.inputName == _inputName);
        return _inputData != null ? _inputData.GetButton() : false;
    }

    /// <summary>
    /// Отжат ли элемент управления с данным названием?
    /// </summary>
    public bool GetButtonUp(string _inputName)
    {
        InputDataClass _inputData = inputData.Find(x => x.inputName == _inputName);
        return _inputData != null ? _inputData.GetButtonUp() : false;
    }

    /// <summary>
    /// Вернуть значение данного элемента управления
    /// </summary>
    public float GetAxis(string _inputName)
    {
        InputDataClass _inputData = inputData.Find(x => x.inputName == _inputName);
        return _inputData != null ? _inputData.GetAxis() : 0f;
    }

}

/// <summary>
/// Структура данных, которая содержит информацию о том, какие кнопки нужно нажать, чтобы задействовался инпут с названием, записанном в этой структуре
/// </summary>
[System.Serializable]
public class InputDataClass
{
    public string inputName;
    public string inputManagerName;
    public string joystickName;
    public InputControl joystickControl;
    public OneAxisInputControl joystickAxis;

    public void ClearInputData()
    {
        joystickControl = null;
        joystickAxis = null;
    }

    public void SetControl()
    {
        switch (joystickName)
        {
            case "action1":
                joystickControl = InputManager.ActiveDevice.Action1;
                break;
            case "action2":
                joystickControl = InputManager.ActiveDevice.Action2;
                break;
            case "action3":
                joystickControl = InputManager.ActiveDevice.Action3;
                break;
            case "action4":
                joystickControl = InputManager.ActiveDevice.Action4;
                break;
            case "leftBumper":
                joystickControl = InputManager.ActiveDevice.LeftBumper;
                break;
            case "rightBumper":
                joystickControl = InputManager.ActiveDevice.RightBumper;
                break;
            case "rightTrigger":
                joystickControl = InputManager.ActiveDevice.RightTrigger;
                break;
            case "leftTrigger":
                joystickControl = InputManager.ActiveDevice.LeftTrigger;
                break;
            case "command":
                joystickControl = InputManager.ActiveDevice.Command;
                break;
            case "leftStickUp":
                joystickControl = InputManager.ActiveDevice.LeftStickUp;
                break;
            case "leftStickDown":
                joystickControl = InputManager.ActiveDevice.LeftStickDown;
                break;
            case "leftStickRight":
                joystickControl = InputManager.ActiveDevice.LeftStickRight;
                break;
            case "leftStickLeft":
                joystickControl = InputManager.ActiveDevice.LeftStickLeft;
                break;
            case "leftStickX":
                joystickAxis = InputManager.ActiveDevice.LeftStickX;
                break;
            case "leftStickY":
                joystickAxis = InputManager.ActiveDevice.LeftStickY;
                break;
            case "rightStickX":
                joystickAxis = InputManager.ActiveDevice.RightStickX;
                break;
            case "rightStickY":
                joystickAxis = InputManager.ActiveDevice.RightStickY;
                break;
            case "dPadX":
                joystickAxis = InputManager.ActiveDevice.DPadX;
                break;
            case "dPadY":
                joystickAxis = InputManager.ActiveDevice.DPadY;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Были ли нажаты кнопки, соответствующие данному элементу управления
    /// </summary>
    public bool GetButtonDown()
    {
        /*if (joystickControl!=null?joystickControl.Target == InputControlType.None: false)
        {
            InputCollection.instance.StartInitializeProcess();
        }*/
        return Input.GetButtonDown(inputManagerName) ? true : (joystickControl != null ? joystickControl.WasPressed : false) || (joystickAxis != null ? joystickAxis.WasPressed : false);
    }

    /// <summary>
    /// Зажаты ли кнопки, соответствующие данному элементу управления
    /// </summary>
    public bool GetButton()
    {
        return Input.GetButton(inputManagerName) ? true : (joystickControl != null ? joystickControl.IsPressed : false) || (joystickAxis != null ? joystickAxis.IsPressed : false);
    }

    /// <summary>
    /// Были ли отпущены кнопки, соответствующие данному элементу управления
    /// </summary>
    public bool GetButtonUp()
    {
        return Input.GetButtonUp(inputManagerName) ? true : (joystickControl != null ? joystickControl.WasReleased : false) || (joystickAxis != null ? joystickAxis.WasReleased : false);
    }

    /// <summary>
    /// Вернуть значение данного элемента управления
    /// </summary>
    /// <returns></returns>
    public float GetAxis()
    {
        float value1 = Input.GetAxis(inputManagerName);
        float value2 = joystickControl!=null? joystickControl.Value: joystickAxis!=null? joystickAxis.Value:0f;
        if (Mathf.Abs(value1) > Mathf.Abs(value2))
            return value1;
        else
            return value2;
    }

}