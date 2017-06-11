using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class BuildManager : MonoBehaviour {

    [PostProcessBuild(10000)]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
        /*if (buildTarget == BuildTarget.StandaloneOSXIntel ||
            buildTarget == BuildTarget.StandaloneOSXIntel64 ||
            buildTarget == BuildTarget.StandaloneOSXUniversal)
        {
            FileInfo[] fInfos = new DirectoryInfo(path + "Contents/Resources/Data/StreamingAssets/").GetFiles();
            for (int i = 0; i < fInfos.Length; i++)
                if (fInfos[i].Name == "SavesInfo.xml")
                    fInfos[i].Delete();
            fInfos = new DirectoryInfo(path + "Contents/Resources/Data/StreamingAssets/Saves/").GetFiles();
            for (int i = 0; i < fInfos.Length; i++)
                if (fInfos[i].Name.Contains("Profile"))
                    fInfos[i].Delete();
        }
        else
        {
            FileInfo[] fInfos = new DirectoryInfo(path + "MysteryMine_Data/StreamingAssets/").GetFiles();
            for (int i = 0; i < fInfos.Length; i++)
                if (fInfos[i].Name == "SavesInfo.xml")
                    fInfos[i].Delete();
            fInfos = new DirectoryInfo(path + "MysteryMine_Data/StreamingAssets/Saves/").GetFiles();
            for (int i = 0; i < fInfos.Length; i++)
                if (fInfos[i].Name.Contains("Profile"))
                    fInfos[i].Delete();
        }*/
    }
}
