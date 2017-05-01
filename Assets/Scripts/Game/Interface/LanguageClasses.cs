//В этом документе приведены описания всех классов, используемых для поддержки мультиязычности игры

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Енам, несущий информацию об языке
/// </summary>
public enum LanguageEnum { russian=0, english=1, ukrainian=2, polish=3, french=4}

/// <summary>
/// Текст, который имеет несколько вариантов на разных языках
/// </summary>
[System.Serializable]
public struct MultiLanguageText
{
    [TextArea(3, 10)]
    public string russian, english, ukranian, polish, french;

    public MultiLanguageText(string _russian, string _english, string _ukranian, string _polish, string _french)
    {
        russian = _russian;
        english = _english;
        ukranian = _ukranian;
        polish = _polish;
        french = _french;
    }

    /// <summary>
    /// Вернуть текст, соответствующий заданному языку
    /// </summary>
    public string GetText(LanguageEnum language)
    {
        switch (language)
        {
            case LanguageEnum.russian:
                return russian;
                break;
            case LanguageEnum.english:
                return english;
                break;
            case LanguageEnum.ukrainian:
                return ukranian;
                break;
            case LanguageEnum.polish:
                return polish;
                break;
            case LanguageEnum.french:
                return french;
                break;
            default:
                return russian;
                break;
        }
    }

    /// <summary>
    /// Выставить текст на заданный язык
    /// </summary>
    public void SetText(LanguageEnum language, string _text)
    {
        switch (language)
        {
            case LanguageEnum.russian:
                russian=_text;
                break;
            case LanguageEnum.english:
                english = _text;
                break;
            case LanguageEnum.ukrainian:
                ukranian = _text;
                break;
            case LanguageEnum.polish:
                polish=_text;
                break;
            case LanguageEnum.french:
                french=_text;
                break;
            default:
                russian=_text;
                break;
        }
    }

    public void ClearTexts()
    {
        russian = "";
        english = "";
        ukranian = "";
        polish = "";
        french = "";
    }

}

/// <summary>
/// Класс, в котором содержится информация о том, какой экземпляр компонента Text какие содержит тексты разных языков
/// </summary>
[System.Serializable]
public class MultiLanguageTextInfo
{
    public Text text;
    public MultiLanguageText mLanguageText;

}

/// <summary>
/// Спрайт, который имеет несколько вариантов на разных языках игры
/// </summary>
[System.Serializable]
public struct MultiLanguageSprite
{

    public Sprite russian, english, ukranian, polish, french;

    public MultiLanguageSprite(Sprite _russian, Sprite _english, Sprite _ukranian, Sprite _polish, Sprite _french)
    {
        russian = _russian;
        english = _english;
        ukranian = _ukranian;
        polish = _polish;
        french = _french;
    }

    /// <summary>
    /// Вернуть текст, соответствующий заданному языку
    /// </summary>
    public Sprite GetSprite(LanguageEnum language)
    {
        switch (language)
        {
            case LanguageEnum.russian:
                return russian;
                break;
            case LanguageEnum.english:
                return english;
                break;
            case LanguageEnum.ukrainian:
                return ukranian;
                break;
            case LanguageEnum.polish:
                return polish;
                break;
            case LanguageEnum.french:
                return french;
                break;
            default:
                return russian;
                break;
        }
    }
}