﻿using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MisteryCaveEditor : Editor{

    /*[MenuItem("Mystery Mine/Create Experimental Thing")]
    public static void CreateExperimentalThing()
    {
    }*/

    [MenuItem("Mystery Mine/Create Speech")]
    public static void CreateDialog()
    {
        EditorWindow.GetWindow(typeof(DialogCreateWindow));
    }

    [MenuItem("Mystery Mine/Create Item")]
    public static void CreateItem()
    {
        EditorWindow.GetWindow(typeof(ItemCreator));
    }

    [MenuItem("Mystery Mine/Create Quest")]
    public static void CreateQuest()
    {
        EditorWindow.GetWindow(typeof(QuestCreator));
    }

    [MenuItem("Mystery Mine/Create Story")]
    public static void CreateStory()
    {
        EditorWindow.GetWindow(typeof(StoryCreator));
    }

    [MenuItem("Mystery Mine/Create Database")]
    public static void CreateDatabase()
    {
        EditorWindow.GetWindow(typeof(DatabaseCreator));
    }

    [MenuItem("Mystery Mine/LevelEditor/LevelEditor")]
    public static void LevelEditor()
    {
        EditorWindow.GetWindow(typeof(LevelEditor));
    }

    [MenuItem("Mystery Mine/GameController/Set IDs")]
    public static void SetIDs()
    {
        GameController gameController = SpecialFunctions.gameController;
        gameController.GetLists(true);
        gameController.IDSetted = true;
    }

    /// <summary>
    /// Обновить данные о сохранениях (полностью очистить их)
    /// </summary>
    [MenuItem("Mystery Mine/Create SavesInfo")]
    public static void CreateSavesInfo()
    {
        /*
        FileInfo[] fInfos = new DirectoryInfo(Application.streamingAssetsPath).GetFiles();
        for (int i = 0; i < fInfos.Length; i++)
            if (fInfos[i].Name == "SavesInfo.xml")
                fInfos[i].Delete();
        fInfos = new DirectoryInfo(Application.streamingAssetsPath + "/Saves/").GetFiles();
        for (int i = 0; i < fInfos.Length; i++)
            if (fInfos[i].Name.Contains("Profile"))
                fInfos[i].Delete();
        */

        PlayerPrefs.DeleteKey("DefaultLanguage");
        Serializator.SaveXmlSavesInfo(new SavesInfo(3), "Assets/StreamingAssets/SavesInfo.xml");
        Serializator.SaveXml(null, "Assets/StreamingAssets/Saves/Profile0.xml");
        Serializator.SaveXml(null, "Assets/StreamingAssets/Saves/Profile1.xml");
        Serializator.SaveXml(null, "Assets/StreamingAssets/Saves/Profile2.xml");
        PlayerPrefs.SetInt("Checkpoint Number", 0);
    }

}
    