using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Скрипт управляющий экраном, показываемым в конце игры
/// </summary>
public class EndScreen : MonoBehaviour
{

    #region consts

    private const string mainMenuName = "MainMenu", firstLevelName = "cave_lvl1";
    private const float endScreenTime = 3f;//Сколько времени висит этот экран перед тем, как перейти в главное меню

    #endregion //consts

    #region parametres

    private string savesInfoPath;

    #endregion //parametres

    void Start ()
    {
        savesInfoPath = (Application.dataPath) + "/StreamingAssets/SavesInfo.xml";
        SavesInfo savesInfo = Serializator.DeXmlSavesInfo(savesInfoPath);
        int profileNumber = PlayerPrefs.GetInt("Profile Number");
        SaveInfo sInfo = savesInfo.saves[profileNumber];
        sInfo.saveTime = System.DateTime.Now.ToString();
        sInfo.hasData = true;
        savesInfo.currentProfileNumb = profileNumber;
        sInfo.loadSceneName = firstLevelName;
        Serializator.SaveXmlSavesInfo(savesInfo, savesInfoPath);
        StartCoroutine(EndScreenProcess());

    }

    /// <summary>
    /// Процесс показа экрана ожидания в конце игры
    /// </summary>
    IEnumerator EndScreenProcess()
    {
        yield return new WaitForSeconds(endScreenTime);
        SceneManager.LoadScene(mainMenuName);
    }
	
}
