using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class JoystickController : MonoBehaviour
{
    public static JoystickController instance;

    private List<ButtonPressedState> wasPressed;
    private List<ButtonPressedState> wasUpped;

    void Start()
    {
        if (instance == null || instance == this)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            wasPressed = new List<ButtonPressedState>();
            wasUpped = new List<ButtonPressedState>();
            var buttons = Enum.GetValues(typeof(JButton)).Cast<JButton>();
            foreach (JButton button in buttons)
            {
                wasPressed.Add(new ButtonPressedState()
                {
                    key =
                        "joystick " + button.ToString().Insert(6, " "),
                    itWasPressed = false
                });
                wasUpped.Add(new ButtonPressedState()
                {
                    key =
                        "joystick " + button.ToString().Insert(6, " "),
                    itWasPressed = false
                });
            }

            return;
        }


        Destroy(this);
    }

    void Update()
    {
        for (int i = 0; i < wasPressed.Count; i++)
        {
            var current = wasPressed[i];
            if (Input.GetKey(current.key))
            {
                if (!current.notSet && current.lastFrame < Time.frameCount)
                {
                    current.lastFrame = Time.frameCount;
                    current.itWasPressed = true;
                    current.notSet = true;
                }
            }
            else
            {
                current.notSet = false;
            }

            if (current.lastFrame < Time.frameCount)
            {
                current.itWasPressed = false;
            }
        }

        for (int i = 0; i < wasUpped.Count; i++)
        {
            var current = wasUpped[i];
            current.itWasPressed = false;
            if (Input.GetKey(current.key))
            {
                current.notSet = true;
            }
            else
            {
                if (current.notSet)
                {
                    current.itWasPressed = true;
                    current.notSet = false;
                }
            }
        }
    }

    public float GetAxis(JAxis axis)
    {
        return Input.GetAxis("Joystick " + axis);
    }

    public bool GetButton(JButton button)
    {
        return Input.GetKey("joystick " + button.ToString().Insert(6, " "));
    }

    public bool GetButtonDown(JButton button)
    {
        return wasPressed[(int.Parse(button.ToString().Remove(0, 6)))].itWasPressed;
    }

    public bool GetButtonUp(JButton button)
    {
        return wasUpped[(int.Parse(button.ToString().Remove(0, 6)))].itWasPressed;
    }

    private class ButtonPressedState
    {
        public string key;
        public bool itWasPressed;
        public bool notSet;
        public int lastFrame;
    }
}

public enum JAxis
{
    Vertical,
    Horizontal
}

public enum JButton
{
    button0,
    button1,
    button2,
    button3,
    button4,
    button5,
    button6,
    button7,
}
