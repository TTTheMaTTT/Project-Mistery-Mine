using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Кнопка, которая начинает новый диалог
/// </summary>
public class DialogAnswerButton : MonoBehaviour
{
    #region fields

    protected Dialog nextDialog;//Диалог, к которому ведёт нажатие на кнопку
    protected Text text;//Текст на кнопке

    DialogWindowScript dialogWindow;

    #endregion //fields

    void Awake()
    {
        text = GetComponentInChildren<Text>();
        dialogWindow = SpecialFunctions.dialogWindow;
    }

    /// <summary>
    /// Установить текст на кнопке ответа 
    /// </summary>
    /// <param name="_text"></param>
    public void SetText(string _text)
    {
        text.text = _text;
    }

    public void SetAnswer(Dialog _dialog)
    {
        nextDialog = _dialog;
    }

    /// <summary>
    /// Инициализировать кнопку ответа, используя экземпляр класса ответа на реплику
    /// </summary>
    public void InitializeAnswerButton(SpeechAnswerClass _answer)
    {
        SetText(_answer.answerText);
        SetAnswer(_answer.nextDialog);
    }

    /// <summary>
    ///  Начать новый диалог при нажатии кнопки
    /// </summary>
    public void BeginNextDialog()
    {
        dialogWindow.StopDialog();
        if (nextDialog == null)
            return;
        dialogWindow.BeginDialog(nextDialog);
    }

}
