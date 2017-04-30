using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIImageTextButtonScript : UIImageButtonScript
{

    #region fields

    protected Text text;

    #endregion //fields

    #region parametres

    [SerializeField]protected Color inactiveTextColor, activeTextColor;
    public override UIElementStateEnum ElementState
    {
        get
        {
            return base.ElementState;
        }

        set
        {
            base.ElementState = value;
            text.color = value == UIElementStateEnum.inactive? inactiveTextColor: activeTextColor;
        }
    }

    #endregion //parametres

    public override void Initialize()
    {
        base.Initialize();
        text = transform.FindChild("Text").GetComponent<Text>();
    }

}
