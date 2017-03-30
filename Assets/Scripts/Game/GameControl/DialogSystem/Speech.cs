using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif 

/// <summary>
/// Класс, представляющий собой реплику, что говорит персонаж
/// </summary>
[System.Serializable]
public class Speech 
{
    public string speechName;//Название реплики. Нужна для того, чтобы отличать одни строчки от других в скрипте

    public bool edit = false;//Редактировать ли параметры реплики?

    public SpeechModEnum speechMod = SpeechModEnum.usual;//Режим отображения реплики

    public bool hasText=false;//Отображается ли текст какого либо персонажа?
    public bool hasPositionChange = false;//Меняется ли положение какого-либо объекта?
    public bool hasAnimation = false;//Происходит ли какая-нибудь анимация?
    public bool hasOrientationChange = false;//Меняется ли ориентация какого-либо персонажа?

    public float fadeSpeed=0;//Какова скорость затухания или проявления экрана (используется только при режимах waitFadeInOut, waitFadeIn и waitFadeOut)
    public float waitTime = 0f;//Сколько времени должно пройти, чтобы произошёл переход к следующей реплике? (используется только при режимах типа wait)

    [TextArea(3, 10)]
    public string text="";//Текст реплики
    public Sprite portrait=null;//Иконка, произносящего реплику... Используется при hasText==true

    public SpeechAnswerClass answer1 = null, answer2 = null;//Ответы на реплику (используется только при режиме answer)

    public List<SpeechChangePositionClass> changePositionData = new List<SpeechChangePositionClass>();//Как должны измениться позиции персонажей при использовании реплики 
                                                                                                      //(Используется при hasPositionChange==true)
    public List<SpeechChangeOrientationClass> changeOrientationData = new List<SpeechChangeOrientationClass>();//Как должны измениться ориентации персонажей при использовании реплики (Используется при hasOrientationChange==true)

    public List<SpeechAnimationClass> animationData = new List<SpeechAnimationClass>();//Какие анимации должны проиграться при репликах(Используется при hasAnimation==true)

    public CameraModEnum camMod=CameraModEnum.playerMove;//В какой режим должна перейти камера, когда происходит реплика
    public Vector3 camPosition=Vector3.zero;//Позиция камеры при реплике 
    public int camObjectID=-1;//За каким объектом следит камера

    public Speech(Speech _speech)
    {
        speechName = _speech.speechName;
        edit = _speech.edit;
        speechMod = _speech.speechMod;
        hasText = _speech.hasText;
        hasPositionChange = _speech.hasPositionChange;
        hasOrientationChange = _speech.hasOrientationChange;
        hasAnimation = _speech.hasAnimation;
        fadeSpeed = _speech.fadeSpeed;
        waitTime = _speech.waitTime;
        text = _speech.text;
        portrait = _speech.portrait;
        answer1 = _speech.answer1;
        answer2 = _speech.answer2;
        changePositionData = _speech.changePositionData;
        changeOrientationData = _speech.changeOrientationData;
        animationData = _speech.animationData;
        camMod = _speech.camMod;
        camPosition = _speech.camPosition;
        camObjectID = _speech.camObjectID;
    }


}

/// <summary>
/// Класс, представляющий обой ответ на реплику НПС
/// </summary>
[System.Serializable]
public class SpeechAnswerClass
{
    public string answerText="";//Содержание ответа
    public Dialog nextDialog;//Какой диалог следует за данным ответом?

    public SpeechAnswerClass()
    {
        answerText = "";
        nextDialog = null;
    }

    public SpeechAnswerClass(SpeechAnswerClass _answer)
    {
        answerText = _answer.answerText;
        nextDialog = _answer.nextDialog;
    }

}

/// <summary>
/// Класс, содержащий информацию о том, как изменится положение персонажа с заданным dialogID при произношении реплики
/// </summary>
[System.Serializable]
public class SpeechChangePositionClass
{
    public int dialogID = -1;//Персонаж с таким ID должен изменить своё положение
    public Vector3 position;//Новое положение персонажа

    public SpeechChangePositionClass()
    {
        dialogID = -1;
        position = Vector3.zero;
    }

    public SpeechChangePositionClass(SpeechChangePositionClass _speechPositionData)
    {
        dialogID = _speechPositionData.dialogID;
        position = _speechPositionData.position;
    }

}

/// <summary>
/// Класс, содержащий информацию о том, как изменится ориентация персонажа с заданным dialogID при произношении реплики
/// </summary>
[System.Serializable]
public class SpeechChangeOrientationClass
{
    public int dialogID = -1;//Персонаж с таким ID должен изменить своё положение
    public OrientationEnum orientation=OrientationEnum.right;//Новая ориентация персонажа

    public SpeechChangeOrientationClass()
    {
        dialogID = -1;
        orientation = OrientationEnum.right;
    }

    public SpeechChangeOrientationClass(SpeechChangeOrientationClass _speechOrientationData)
    {
        dialogID = _speechOrientationData.dialogID;
        orientation = _speechOrientationData.orientation;
    }

}

/// <summary>
/// Класс, содержащий информацию о том, кем (чем) какая анимация должна проиграться при произношении реплики
/// </summary>
[System.Serializable]
public class SpeechAnimationClass
{
    public int dialogID = -1;//Объект с таким диалоговым id должен воспроизвести анимацию
    public string animationName="";//Название анимации
    public string id = "";   //Параметры
    public int argument = 0;// анимации

    public SpeechAnimationClass()
    {
        dialogID = -1;
        animationName = "";
        id = "";
        argument = 0;
    }

    public SpeechAnimationClass(SpeechAnimationClass _speechAnimation)
    {
        dialogID = _speechAnimation.dialogID;
        animationName = _speechAnimation.animationName;
        id = _speechAnimation.id;
        argument = _speechAnimation.argument;
    }

}