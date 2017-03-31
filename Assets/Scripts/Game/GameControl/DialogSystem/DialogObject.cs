using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Объект - участник диалога. Диалоговая система использует этот объект.
/// </summary>
[ExecuteInEditMode]
public class DialogObject : MonoBehaviour
{

    #region parametres

    [SerializeField]protected int id = 0;
    public int ID { get { return id;} set { id = value; } }

    #endregion //parametres

    public void Initialize()
    {
        if (id == 0)
        {
            if (GetComponent<HeroController>() != null? !(GetComponent<HeroController>() is SpiderHeroController):false)
                id = 0;
            else
                id = SpecialFunctions.dialogWindow.GetDialogID();
        }  
    }

    public void OnEnable()
    {
        Initialize();
    }

    /// <summary>
    /// Функция, что анимирует объект с данным компонентом
    /// </summary>
    public void Animate(AnimationEventArgs e)
    {
        CharacterController charControl = GetComponent<CharacterController>();
        Animator anim = GetComponent<Animator>();
        if (charControl != null)
            charControl.Animate(e);
        else if (anim != null)
            anim.Play(e.AnimationType);
    }

    /// <summary>
    /// Установить позицию данному объекту
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    /// <summary>
    /// Установить ориентацию данному объекту
    /// </summary>
    public void SetOrientation(OrientationEnum orientation)
    {
        CharacterController charControl = GetComponent<CharacterController>();
        if (charControl != null)
            charControl.Turn(orientation);
        else
        {
            Vector3 scal = transform.localScale;
            float scaleX = Mathf.Abs(scal.x)*(int)orientation;
            transform.localScale = new Vector3(scaleX, scal.y, scal.z);
        }
    }

    /// <summary>
    /// Сделать объект неактивным, чтобы он мог вести диалог
    /// </summary>
    public void SetImmobile(bool _immobile)
    {
        CharacterController charControl = GetComponent<CharacterController>();
        if (charControl != null)
            charControl.SetImmobile(_immobile);
    }
    
    /// <summary>
    /// Переключить объект в состояние разговора, если это возможно
    /// </summary>
    public void SetTalking(bool _talking)
    {
        NPCController npcControl = GetComponent<NPCController>();
        if (npcControl != null)
        {
            if (_talking)
                npcControl.StartTalking();
            else
                npcControl.StopTalking();
        }
    }

}
