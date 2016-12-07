using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Объект, ответственный за управление игрой
/// </summary>
public class GameController : MonoBehaviour
{

    #region eventHandlers

    public EventHandler<StoryEventArgs> StartGameEvent;

    #endregion //eventHandlers

    #region fields

    protected DialogWindowScript dialogWindow;//Окно диалога
    protected GameMenuScript gameMenu;//игровой интерфейс
    protected LevelCompleteScreenScript levelCompleteScreen;//Окошко, открывающееся при завершении уровня

    List<CheckpointController> checkpoints = new List<CheckpointController>();

    #region saveSystem

    /// <summary>
    /// Списки, используемые для инициализации всех активных игровых объектов
    /// </summary>
    private List<AIController> monsters = new List<AIController>();
    private List<GameObject> intObjects = new List<GameObject>();
    private List<NPCController> NPCs = new List<NPCController>();

    #endregion //saveSystem

    #endregion //fields

    #region parametres

    int profileNumber;

    string datapath;
    string savesInfoPath;

    int startCheckpointNumber = 0;

    int secretsFoundNumber = 0, secretsTotalNumber = 0;//Сколько секретных мест на уровне было найдено и сколько их всего

    [SerializeField]protected bool idSetted=false;//Были ли установлены id для всех игровых объектов
    public bool IDSetted
    {
        get { return idSetted; }
        set
        {
            idSetted = value;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif //UNITY_EDITOR

        }
    }

    #endregion //parametres

    protected void Update()
    {
        if (Input.GetButtonDown("Cancel"))
            gameMenu.ChangeGameMod();
    }

    protected void Awake()
    {
        Initialize();
    }

    protected void Start()
    {
        SpecialFunctions.SetDark();
        SpecialFunctions.SetFade(false);

        #region RegisterObjects

        //Пройдёмся по всем объектам уровня, дадим им id и посмотрим, как изменятся эти объекты в соответсвтии с сохранениями
        GetLists(!idSetted);

        #endregion //RegisterObjects

        LoadGame();

        monsters = null;
        intObjects =null;
        NPCs = null;

        SpecialFunctions.history.Initialize();
        SpecialFunctions.StartStoryEvent(this, StartGameEvent, new StoryEventArgs());
    }

    protected void Initialize()
    {
        datapath = (Application.dataPath) + "/StreamingAssets/Saves/Profile";
        savesInfoPath = (Application.dataPath) + "/StreamingAssets/SavesInfo.xml";

        Transform interfaceWindows = SpecialFunctions.gameInterface.transform;
        dialogWindow = interfaceWindows.GetComponentInChildren<DialogWindowScript>();
        gameMenu = interfaceWindows.GetComponentInChildren<GameMenuScript>();
        levelCompleteScreen = interfaceWindows.GetComponentInChildren<LevelCompleteScreenScript>();
        SpecialFunctions.PlayGame();
        if (SceneManager.GetActiveScene().name != "MainMenu")
            Cursor.visible = false;
        if (PlayerPrefs.HasKey("Profile Number"))
            profileNumber = PlayerPrefs.GetInt("Profile Number");
        else
        {
            PlayerPrefs.SetInt("Profile Number", 0);
            profileNumber = 0;
        }
        GameObject[] checkpointObjects = GameObject.FindGameObjectsWithTag("checkpoint");
        checkpoints = new List<CheckpointController>();
        List<int> checkpointNumbers = new List<int>();
        for (int i = 0; i < checkpointObjects.Length; i++)
        {
            checkpoints.Add(checkpointObjects[i].GetComponent<CheckpointController>());
        }

        if (!PlayerPrefs.HasKey("Checkpoint Number"))
            PlayerPrefs.SetInt("Checkpoint Number", 0);
        startCheckpointNumber = PlayerPrefs.GetInt("Checkpoint Number");

        //Определим, сколько секретных мест на уровне
        secretsTotalNumber = 0; secretsFoundNumber = 0;
        GameObject[] secretPlaces = GameObject.FindGameObjectsWithTag("mechanism");
        foreach (GameObject secretPlace in secretPlaces)
            if (secretPlace.GetComponent<SecretPlaceTrigger>() != null)
                secretsTotalNumber++;

    }

    /*
    /// <summary>
    /// Вернуться в главное меню
    /// </summary>
    public static void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    */

    /// <summary>
    ///  Начать диалог
    /// </summary>
    public void StartDialog(NPCController npc, Dialog dialog)
    {
        Transform player = SpecialFunctions.player.transform;
        dialogWindow.BeginDialog(npc, dialog);
    }

    /// <summary>
    /// Функция, вызывающаяся при нахождении секретного места
    /// </summary>
    public void FindSecretPlace()
    {
        secretsFoundNumber++;
    }

    /// <summary>
    /// Функция сохранения игры. Поддерживается 2 режима сохранения: сохранения данных игры в целом и сохранение текущего уровня 
    /// </summary>
    public void SaveGame(int checkpointNumb, bool generally, string levelName)
    {
        Serializator.SaveXml(GetGameData(generally, checkpointNumb), datapath + profileNumber.ToString()+".xml");
        SavesInfo savesInfo = Serializator.DeXmlSavesInfo(savesInfoPath);
        SaveInfo sInfo = savesInfo.saves[profileNumber];
        sInfo.saveTime = System.DateTime.Now.ToString();
        sInfo.hasData = true;
        savesInfo.currentProfileNumb = profileNumber;
        sInfo.loadSceneName = levelName;
        Serializator.SaveXmlSavesInfo(savesInfo, savesInfoPath);
    }

    /// <summary>
    /// Загрузка игры
    /// </summary>
    public void LoadGame()
    {

        SavesInfo savesInfo = Serializator.DeXmlSavesInfo(savesInfoPath);
        if (!Serializator.HasSavesInfo(savesInfoPath))
            return;
        if (savesInfo == null)
            return;
        if (!savesInfo.saves[profileNumber].hasData)
            return;
        GameData gData = Serializator.DeXml(datapath + profileNumber.ToString()+".xml");
        if (gData == null)
            return;
        savesInfo.saves[profileNumber].loadSceneName = SceneManager.GetActiveScene().name;

        GameGeneralData gGData = gData.gGData;
        LevelData lData = gData.lData;
        GameStatistics gStats = GetComponent<GameStatistics>();

        #region GeneralLoad

        if (lData == null? true: !lData.active)//Произошёл переход на новый уровень и нужно учесть только данные, необходимые на протяжении всей игры
        {
            if (gGData != null)
            {
                startCheckpointNumber = gGData.firstCheckpointNumber;
                HeroController hero = SpecialFunctions.player.GetComponent<HeroController>();

                //Сначала переместим главного героя к последнему чекпоинту
                CheckpointController currentCheckpoint = checkpoints.Find(x => (x.checkpointNumb == startCheckpointNumber));
                if (currentCheckpoint != null)
                    SpecialFunctions.MoveToCheckpoint(currentCheckpoint);

                #region heroEquipment

                EquipmentInfo eInfo = gGData.eInfo;
                if (eInfo != null && gStats != null)
                {
                    if (gStats.WeaponDict.ContainsKey(eInfo.weapon))
                    {
                        hero.SetItem(gStats.WeaponDict[eInfo.weapon],true);
                    }
                    foreach (string itemName in eInfo.bagItems)
                    {
                        hero.Bag.Clear();
                        if (gStats.ItemDict.ContainsKey(itemName))
                        {
                            hero.Bag.Add(gStats.ItemDict[itemName]);
                        }
                    }
                }

                List<CollectionInfo> cInfo = gGData.cInfo;
                List<ItemCollection> _collection = gStats.ItemCollections;
                if (cInfo != null && _collection != null)
                {
                    foreach (CollectionInfo cData in cInfo)
                    {
                        ItemCollection iCollection = _collection.Find(x => x.collectionName == cData.collectionName);
                        if (iCollection != null)
                            for (int i = 0; i < cData.itemsFound.Count && i < iCollection.collection.Count; i++)
                                iCollection.collection[i].itemFound = cData.itemsFound[i]; 
                    }
                }

                GetComponent<GameStatistics>().ResetStatistics();

                #endregion //heroEquipment

            }
        }

        #endregion //GeneralLoad

        #region LevelLoad

        else//Если игрок сохранился на чекпоинте, то у него есть прогресс на уровне и именно его мы и загружаем
        {
            HeroController hero = SpecialFunctions.player.GetComponent<HeroController>();

            //Сначала переместим главного героя к последнему чекпоинту
            CheckpointController currentCheckpoint = checkpoints.Find(x => (x.checkpointNumb == lData.checkpointNumber));
            if (currentCheckpoint != null)
                SpecialFunctions.MoveToCheckpoint(currentCheckpoint);

            #region heroEquipment

            EquipmentInfo eInfo = lData.eInfo;
            if (eInfo != null && gStats != null)
            {
                if (gStats.WeaponDict.ContainsKey(eInfo.weapon))
                {
                    hero.SetItem(gStats.WeaponDict[eInfo.weapon],true);
                }
                foreach (string itemName in eInfo.bagItems)
                {
                    hero.Bag.Clear();
                    if (gStats.ItemDict.ContainsKey(itemName))
                    {
                        hero.Bag.Add(gStats.ItemDict[itemName]);
                    }
                }
            }

            #endregion //heroEquipment

            #region gameCollections

            List<CollectionInfo> cInfo = lData.cInfo;
            List<ItemCollection> _collection = gStats.ItemCollections;
            if (cInfo != null && _collection != null)
            {
                foreach (CollectionInfo cData in cInfo)
                {
                    ItemCollection iCollection = _collection.Find(x => x.collectionName == cData.collectionName);
                    if (iCollection != null)
                        for (int i = 0; i < cData.itemsFound.Count && i < iCollection.collection.Count; i++)
                            iCollection.collection[i].itemFound = cData.itemsFound[i];
                }
            }

            #endregion //gameCollections

            GameHistory gHistory = GetComponent<GameHistory>();
            History history = gHistory!=null? gHistory.history:null;

            #region Quests&Story

            QuestInfo qInfo = lData.qInfo;
            StoryInfo sInfo = lData.sInfo;
            if (qInfo != null && sInfo!=null && history != null)
            {
                history.LoadHistory(sInfo, qInfo);
            }

            #endregion //Quests&Story

            #region levelStatistics

            LevelStatsData lStatsInfo = lData.lStatsInfo;
            if (lStatsInfo != null && gStats != null)
            {
                gStats.LoadStaticstics(lStatsInfo);
                gStats.InitializeAllStatistics();
            }

            #endregion //levelStatistics

            #region Drop

            DropData dropInfo = lData.dropInfo;

            //Сначала надо уничтожить все объекты типа drop на уровне
            GameObject[] drops = GameObject.FindGameObjectsWithTag("drop");
            for (int i = drops.Length - 1; i >= 0; i--)
            {
                DestroyImmediate(drops[i]);
            }
            drops = GameObject.FindGameObjectsWithTag("heartDrop");
            for (int i = drops.Length - 1; i >= 0; i--)
            {
                DestroyImmediate(drops[i]);
            }

            //А затем заново их создать
            if (dropInfo != null && gStats != null)
            {
                foreach (DropInfo _dInfo in dropInfo.drops)
                {
                    if (gStats.DropDict.ContainsKey(_dInfo.itemName) && !_dInfo.customDrop)
                    {
                        Instantiate(gStats.DropDict[_dInfo.itemName], _dInfo.position, Quaternion.identity);
                    }
                    else if (gStats.WeaponDict.ContainsKey(_dInfo.itemName))
                    {
                        GameObject newDrop = Instantiate(gStats.itemBase.customDrop, _dInfo.position, Quaternion.identity) as GameObject;
                        newDrop.GetComponent<DropClass>().item = gStats.WeaponDict[_dInfo.itemName];
                    }
                    else if (gStats.ItemDict.ContainsKey(_dInfo.itemName))
                    {
                        GameObject newDrop = Instantiate(gStats.itemBase.customDrop, _dInfo.position, Quaternion.identity) as GameObject;
                        newDrop.GetComponent<DropClass>().item = gStats.ItemDict[_dInfo.itemName];
                    }
                }
            }

            #endregion //Drop

            #region Enemies

            List<EnemyData> enInfo = lData.enInfo;

            if (enInfo != null && monsters != null)
            {
                foreach (EnemyData enData in enInfo)
                {
                    int objId = enData.objId;
                    if (objId < monsters.Count? monsters[objId]!=null:false)
                    {
                        monsters[objId].SetAIData(enData);
                        monsters[objId] = null;
                    }
                }
                for (int i = 0; i < monsters.Count; i++)
                {
                    if (monsters[i] != null)
                        DestroyImmediate(monsters[i].gameObject);
                }
            }

            #endregion //Enemies

            #region InteractiveObjects

            List<InterObjData> intInfo = lData.intInfo;

            if (intInfo != null && intObjects != null)
            {
                foreach (InterObjData interData in intInfo)
                {
                    int objId = interData.objId;
                    if (objId < intObjects.Count ? intObjects[objId] != null : false)
                    {
                        IInteractive iInter = intObjects[objId].GetComponent<IInteractive>();
                        IMechanism iMech = intObjects[objId].GetComponent<IMechanism>();
                        IDamageable iDmg = intObjects[objId].GetComponent<IDamageable>();
                        if (iInter != null)
                        {
                            iInter.SetData(interData);
                        }
                        else if (iMech != null)
                        {
                            iMech.SetData(interData);
                        }
                        else if (iDmg != null)
                        {
                            iDmg.SetData(interData);
                        }
                        intObjects[objId] = null;
                    }
                }
                for (int i = 0; i < intObjects.Count; i++)
                {
                    if (intObjects[i] != null)
                    {
                        if (intObjects[i].GetComponent<ChestController>() != null)
                        {
                            ChestController chest = intObjects[i].GetComponent<ChestController>();
                            chest.DestroyClosedChest();
                        }
                        else if (intObjects[i].GetComponent<CheckpointController>() != null)
                        {
                            CheckpointController checkpoint = intObjects[i].GetComponent<CheckpointController>();
                            checkpoint.DestroyCheckpoint();
                        }
                        else if (intObjects[i].GetComponent<SecretPlaceTrigger>())
                        {
                            SecretPlaceTrigger secretPlace = intObjects[i].GetComponent<SecretPlaceTrigger>();
                            FindSecretPlace();
                            Destroy(secretPlace);
                        }
                        else
                            DestroyImmediate(intObjects[i]);
                    }
                }
            }

            #endregion //InteractiveObjects

            #region NPCs

            List<NPCData> npcInfo = lData.npcInfo;

            if (npcInfo != null && NPCs != null)
            {
                foreach (NPCData npcData in npcInfo)
                {
                    int objId = npcData.objId;
                    if (objId < NPCs.Count ? NPCs[objId] != null : false)
                    {
                        NPCs[objId].SetData(npcData);
                        NPCs[objId] = null;
                    }
                }
                for (int i = 0; i < NPCs.Count; i++)
                {
                    if (NPCs[i] != null)
                        DestroyImmediate(NPCs[i].gameObject);
                }
            }

            #endregion //NPCs

        }

        #endregion //LevelLoad

        /*
        //Сначала переместим главного героя к последнему чекпоинту
        CheckpointController currentCheckpoint = checkpoints.Find(x => (x.checkpointNumb == PlayerPrefs.GetInt("Checkpoint Number")));
        if (currentCheckpoint != null)
            SpecialFunctions.MoveToCheckpoint(currentCheckpoint);
            */

    }

    /// <summary>
    /// Функция, собирающая информацию о текущем состояние всей игры (на данном уровне). Может собирать информацию о текущем уровне, а может об игре в целом - 
    /// в зависимости от параметра general
    /// </summary>
    /// <returns></returns>
    GameData GetGameData(bool general, int cNumber)
    {
        GameData _gData= new GameData();
        if (general)
        {
            _gData.ResetLevelData();
            _gData.SetGeneralGameData(cNumber, SpecialFunctions.player.GetComponent<HeroController>(), SpecialFunctions.statistics.ItemCollections);
        }
        else
        {
            GameObject[] dropObjs = GameObject.FindGameObjectsWithTag("drop");
            GameObject[] heartDropObjs = GameObject.FindGameObjectsWithTag("heartDrop");
            List<DropClass> drops = new List<DropClass>();
            foreach (GameObject dropObj in dropObjs)
            {
                DropClass drop = dropObj.GetComponent<DropClass>();
                if (drop != null)
                    drops.Add(drop);
            }
            foreach (GameObject dropObj in heartDropObjs)
            {
                DropClass drop = dropObj.GetComponent<DropClass>();
                if (drop != null)
                    drops.Add(drop);
            }

            GetLists(false);
            List<EnemyData> enInfo = new List<EnemyData>();
            List<InterObjData> intInfo = new List<InterObjData>();
            List<NPCData> npcInfo = new List<NPCData>();

            foreach (AIController monster in monsters)
                enInfo.Add(monster.GetAIData());

            foreach (GameObject intObject in intObjects)
                intInfo.Add(intObject.GetComponent<IHaveID>().GetData());

            foreach (NPCController npc in NPCs)
                npcInfo.Add((NPCData)npc.GetData());

            List<ItemCollection> _collection = SpecialFunctions.statistics.ItemCollections;

            _gData.SetLevelData(cNumber, SpecialFunctions.player.GetComponent<HeroController>(),  _collection, drops, GetComponent<GameHistory>().history,
                                                                                                        GetComponent<GameStatistics>(),enInfo,intInfo, npcInfo);
        }
        return _gData;
    }

    /// <summary>
    /// Сбросить прогресс уровня
    /// </summary>
    public void ResetLevelData()
    {
        GameData gData = Serializator.DeXml(datapath + profileNumber.ToString() + ".xml");
        if (gData != null)
        {
            gData.ResetLevelData();
        }
        Serializator.SaveXml(gData, datapath + profileNumber.ToString() + ".xml");
    }

    /// <summary>
    /// Функция, что составляет списки объектов
    /// </summary>
    public void GetLists(bool setID)
    {

        #region enemies

        GameObject[] enemiesObjs = GameObject.FindGameObjectsWithTag("enemy");
        monsters = new List<AIController>();
        foreach (GameObject obj in enemiesObjs)
        {
            AIController ai = obj.GetComponent<AIController>();
            if (ai != null)
            {
                if (setID)
                    ai.ID = monsters.Count;
                monsters.Add(ai);
            }
        }

        enemiesObjs = GameObject.FindGameObjectsWithTag("boss");
        foreach (GameObject obj in enemiesObjs)
        {
            AIController ai = obj.GetComponent<AIController>();
            if (ai != null)
            {
                if (setID)
                    ai.ID = monsters.Count;
                monsters.Add(ai);
            }
        }

        #endregion //enemies

        monsters.Sort((x, y) => { return x.ID.CompareTo(y.ID); });

        intObjects = new List<GameObject>();
        ConsiderObjectsWithTag("lever", setID);

        ConsiderObjectsWithTag("door", setID);

        ConsiderObjectsWithTag("checkpoint", setID);

        ConsiderObjectsWithTag("mechanism", setID);

        ConsiderObjectsWithTag("box", setID);

        intObjects.Sort((x, y) => { return x.GetComponent<IHaveID>().GetID().CompareTo(y.GetComponent<IHaveID>().GetID()); });

        #region NPCs

        GameObject[] npcObjs = GameObject.FindGameObjectsWithTag("npc");
        NPCs = new List<NPCController>();
        foreach (GameObject obj in npcObjs)
        {
            NPCController npc = obj.GetComponent<NPCController>();
            if (npc != null)
            {
                if (setID)
                    npc.SetID(NPCs.Count);
                NPCs.Add(npc);
            }
        }

        GameObject spiritObj = GameObject.FindGameObjectWithTag("spirit");
        if (spiritObj != null)
        {
            NPCController spirit = spiritObj.GetComponent<NPCController>();
            if (spirit != null)
            {
                if (setID)
                    spirit.SetID(NPCs.Count);
                NPCs.Add(spirit);
            }
        }

        NPCs.Sort((x, y) => { return x.GetID().CompareTo(y.GetID()); });

        #endregion //NPCs

    }

    public void ConsiderObjectsWithTag(string tag,bool setID)
    {
        GameObject[] intObjs = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in intObjs)
        {
            IHaveID inter = obj.GetComponent<IHaveID>();
            if (inter != null)
            {
                if (setID)
                    inter.SetID(intObjects.Count);
                intObjects.Add(obj);
            }
        }
    }

    /// <summary>
    /// Функция завершения данного уровня
    /// </summary>
    /// <param name="nextLevelName">Название следующего уровня</param>
    public void CompleteLevel(string nextLevelName)
    {
        GameStatistics statistics = SpecialFunctions.statistics;
        List<ItemCollection> itemCollections = statistics != null ? statistics.ItemCollections : null;
        string sceneName = SceneManager.GetActiveScene().name;
        ItemCollection _collection = itemCollections != null ? itemCollections.Find(x=>sceneName.Contains(x.settingName)):null;
        levelCompleteScreen.SetLevelCompleteScreen(nextLevelName, secretsFoundNumber, secretsTotalNumber, _collection);
    }

}
