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

    #region consts

    #region gameEffectConsts

    protected const float ancestorsRevengeTime = 60f;//Время и шанс эффекта "Месть предков"
    protected const float ancestorsRevengeProbability = .1f;

    protected const float tribalLeaderTime = 30f;//Время и шанс эффекта "Вождь племени"
    protected const float tribalLeaderProbability =.07f;

    protected const float battleCryProbability = .05f;//Шанс и радиус действия эффекта "Боевой клич"
    protected const float battleCryRadius = 6f;

    protected const float treasureHunterTime = 30f;//Время действия и шанс эффекта "Кладоискатель"
    protected const float treasureHunterProbability = .4f;

    protected const float collectorTime = 15f;//Время действия и шанс эффекта "Коллекционер"
    protected const float collectorProbability = .3f;

    protected const float compassArrowOffsetY = 0.17f;//Насколько смещена по вертикали стрелка компаса относительно персонажа

    protected const float ancientDarknessTime = 15f;//Время и шанс эффекта "Древняя тьма"
    protected const float ancientDarknessProbability = .05f;

    protected const float totemAnimalProbability = .07f;//Шанс эффекта "Тотемное животное"

    protected const float tribalRitualProbability = .07f;//Шанс эффекта "Ритуал племени"

    #endregion //gameEffectConsts

    #endregion //consts

    #region dictionaries

    //protected Dictionary<string, BuffFunction> buffFunctions = new Dictionary<string, BuffFunction>();
    protected Dictionary<string, StopBuffFunction> stopBuffFunctions = new Dictionary<string, StopBuffFunction>();
    protected Dictionary<string, string> buffNamesDict = new Dictionary<string, string>() { {"AncestorsRevenge","Месть предков"},
                                                                                            {"TribalLeader", "Вождь племени" },
                                                                                            {"TreasureHunter", "Кладоскатель"},
                                                                                            {"Collector", "Коллекционер"},
                                                                                            {"AncientDarkness","Древняя тьма"},
                                                                                            {"TotemAnimal", "Тотемное животное"},
                                                                                            {"TribalRitual", "Ритуал племени"} };

    #endregion //dictionaries

    #region delegates

    //protected delegate void BuffFunction(CharacterController _char, float _time, int argument, string id);
    protected delegate void StopBuffFunction(int argument, string id);

    #endregion //delegates

    #region eventHandlers

    public EventHandler<StoryEventArgs> StartGameEvent, EndGameEvent;

    #endregion //eventHandlers

    #region fields

    protected DialogWindowScript dialogWindow;//Окно диалога
    protected GameMenuScript gameMenu;//игровой интерфейс
    protected LevelCompleteScreenScript levelCompleteScreen;//Окошко, открывающееся при завершении уровня
    private HeroController hero;//Главный герой
    private HeroController Hero { get { if (hero == null) hero = SpecialFunctions.Player.GetComponent<HeroController>(); return hero; } }

    private List<CheckpointController> checkpoints = new List<CheckpointController>();

    private static List<string> activeGameEffects = new List<string>();//Названия активных игровычх эффектов

    [SerializeField]private GameObject treasureHunterArrow;//Стрелка компаса охотника за сокровищами
    [SerializeField]private GameObject collectorArrow;//Стрелка компаса коллекционера

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
    static int monstersIdCount = 0, objectsIdCount=0, npcsIdCount=0;//Количество персонажей с id-шниками

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
        monstersIdCount = monsters.Count;
        npcsIdCount = NPCs.Count;
        objectsIdCount = intObjects.Count;

        #endregion //RegisterObjects

        LoadGame();

        monsters = null;
        intObjects =null;
        NPCs = null;

        SpecialFunctions.history.Initialize();
        SpecialFunctions.StartStoryEvent(this, StartGameEvent, new StoryEventArgs());
        Resources.UnloadUnusedAssets();

        Debug.LogWarning(GetComponent<GameStatistics>().gameHistoryProgress.GetStoryProgress("spiderStory"));

    }

    protected void Initialize()
    {
        Resources.UnloadUnusedAssets();
        monstersIdCount = 0;
        objectsIdCount = 0;
        npcsIdCount = 0;
        SpecialFunctions.InitializeObjects();
        InitializeDictionaries();
        datapath = (Application.dataPath) + "/StreamingAssets/Saves/Profile";
        savesInfoPath = (Application.dataPath) + "/StreamingAssets/SavesInfo.xml";
        GameObject p=SpecialFunctions.Player;
        Transform interfaceWindows = SpecialFunctions.gameInterface.transform;
        dialogWindow = interfaceWindows.GetComponentInChildren<DialogWindowScript>();
        gameMenu = interfaceWindows.GetComponentInChildren<GameMenuScript>();
        //if (gameMenu != null)
            //Debug.LogError("GameMenu initialized");
        levelCompleteScreen = interfaceWindows.GetComponentInChildren<LevelCompleteScreenScript>();
        SpecialFunctions.PlayGame();
        if (SceneManager.GetActiveScene().name != "MainMenu")
            Cursor.visible = true;
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
        activeGameEffects = new List<string>();
    }

    /// <summary>
    /// Инициализировать нужные словари
    /// </summary>
    void InitializeDictionaries()
    {
        stopBuffFunctions.Add("AncestorsRevenge", StopAncestorsRevenge);
        stopBuffFunctions.Add("TribalLeader", StopTribalLeader);
        stopBuffFunctions.Add("TreasureHunter", StopTreasureHunt);
        stopBuffFunctions.Add("Collector", StopCollectorProcess);
        stopBuffFunctions.Add("AncientDarkness", StopAncientDarkness);
        stopBuffFunctions.Add("TotemAnimal", StopTotemAnimal);
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
        dialogWindow.BeginDialog(npc, dialog);
    }

    public void StartDialog(Dialog dialog)
    {
        dialogWindow.BeginDialog(dialog);
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
        SpecialFunctions.battleField.KillAllies();//Все союзники героя погибают (ввиду их временного характера)
        Hero.RestoreStats();//При сохранении герой восстанавливает свои характеристики (восстановление здоровья, сброс отрицательных боевых эффектов)
        Serializator.SaveXml(GetGameData(generally, checkpointNumb), datapath + profileNumber.ToString()+".xml");
        SavesInfo savesInfo = Serializator.DeXmlSavesInfo(savesInfoPath);
        SaveInfo sInfo = savesInfo.saves[profileNumber];
        sInfo.saveTime = System.DateTime.Now.ToString();
        sInfo.hasData = true;
        savesInfo.currentProfileNumb = profileNumber;
        sInfo.loadSceneName = levelName;
        if (generally)
            SpecialFunctions.nextLevelName = levelName;
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
        Summoner summoner = GetComponent<Summoner>();

        #region GeneralLoad

        if (lData == null? true: !lData.active)//Произошёл переход на новый уровень и нужно учесть только данные, необходимые на протяжении всей игры
        {
            if (gGData != null)
            {
                startCheckpointNumber = gGData.firstCheckpointNumber;

                //Сначала переместим главного героя к последнему чекпоинту
                CheckpointController currentCheckpoint = checkpoints.Find(x => (x.checkpointNumb == startCheckpointNumber));
                if (currentCheckpoint != null)
                    SpecialFunctions.MoveToCheckpoint(currentCheckpoint);

                #region heroEquipment

                EquipmentInfo eInfo = gGData.eInfo;
                if (eInfo != null && gStats != null)
                {
                    EquipmentClass equip = Hero.Equipment;
                    SpecialFunctions.equipWindow.ClearCells();
                    equip.bag.Clear();
                    foreach (string itemName in eInfo.bagItems)
                        if (gStats.ItemDict.ContainsKey(itemName))
                            equip.bag.Add(gStats.ItemDict[itemName]);
                    equip.weapons.Clear();
                    foreach (string itemName in eInfo.weapons)
                        if (gStats.WeaponDict.ContainsKey(itemName))
                            Hero.SetItem(gStats.WeaponDict[itemName]);
                    if (gStats.WeaponDict.ContainsKey(eInfo.weapon))
                    {
                        Hero.CurrentWeapon = gStats.WeaponDict[eInfo.weapon];
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

                Hero.SetBuffs(new BuffListData(new List<BuffClass>()));

                gStats.ResetStatistics();
                gStats.gameHistoryProgress.SetStoryProgressData(gGData.progressInfo);

                #endregion //heroEquipment

            }
        }

        #endregion //GeneralLoad

        #region LevelLoad

        else//Если игрок сохранился на чекпоинте, то у него есть прогресс на уровне и именно его мы и загружаем
        {

            //Сначала переместим главного героя к последнему чекпоинту
            CheckpointController currentCheckpoint = checkpoints.Find(x => (x.checkpointNumb == lData.checkpointNumber));
            if (currentCheckpoint != null)
                SpecialFunctions.MoveToCheckpoint(currentCheckpoint);

            #region heroEquipment

            EquipmentInfo eInfo = lData.eInfo;
            if (eInfo != null && gStats != null)
            {
                EquipmentClass equip = Hero.Equipment;
                SpecialFunctions.equipWindow.ClearCells();
                equip.bag.Clear();
                foreach (string itemName in eInfo.bagItems)
                    if (gStats.ItemDict.ContainsKey(itemName))
                        equip.bag.Add(gStats.ItemDict[itemName]);
                equip.weapons.Clear();
                foreach (string itemName in eInfo.weapons)
                    if (gStats.WeaponDict.ContainsKey(itemName))
                        Hero.SetItem(gStats.WeaponDict[itemName]);
                if (gStats.WeaponDict.ContainsKey(eInfo.weapon))
                {
                    Hero.CurrentWeapon = gStats.WeaponDict[eInfo.weapon];
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

            gStats.gameHistoryProgress.SetStoryProgressData(lData.progressInfo);

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
            Dictionary<string, GameObject> monsterObjects = new Dictionary<string, GameObject>();

            if (enInfo != null && monsters != null)
            {
                foreach (EnemyData enData in enInfo)
                {
                    int objId = enData.objId;
                    if (objId < monsters.Count ? monsters[objId] != null : false)
                    {
                        monsters[objId].SetAIData(enData);
                        monsters[objId] = null;
                    }
                    else if (objId >= monsters.Count)
                    {
                        GameObject newMonster = null;
                        GameObject _obj = null;
                        SummonClass summon = null;
                        if (summoner != null)
                            summon = summoner.GetSummon(enData.objName);
                        if (summon != null)
                        {
                            _obj = summon.summon;
                            _obj.SetActive(true);
                        }
                        else
                        {
                            if (!monsterObjects.ContainsKey(enData.objName))
                            {
                                newMonster = Resources.Load(enData.objName) as GameObject;
                                monsterObjects.Add(enData.objName, newMonster);
                            }
                            else
                                newMonster = monsterObjects[enData.objName];
                            if (newMonster != null)
                                _obj = Instantiate(newMonster, enData.position, Quaternion.identity);
                             
                        }
                        if (_obj == null)
                            continue;
                        AIController _ai = _obj.GetComponent<AIController>();
                        if (_ai != null)
                            _ai.SetAIData(enData);
                        if (objId >= monstersIdCount)
                            monstersIdCount = objId + 1;
                    }
                }
                for (int i = 0; i < monsters.Count; i++)
                {
                    if (monsters[i] != null)
                        Destroy(monsters[i].gameObject);
                }
            }

            #endregion //Enemies

            #region InteractiveObjects

            List<InterObjData> intInfo = lData.intInfo;
            Dictionary<string, GameObject> interObjects = new Dictionary<string, GameObject>();

            if (intInfo != null && intObjects != null)
            {
                foreach (InterObjData interData in intInfo)
                {
                    int objId = interData.objId;

                    if (objId < intObjects.Count && objId >= 0 ? intObjects[objId] != null : false)
                    {
                        IHaveID inter = intObjects[objId].GetComponent<IHaveID>();
                        if (inter != null)
                            inter.SetData(interData);
                        intObjects[objId] = null;
                    }
                    else if (objId >= intObjects.Count)
                    {
                        GameObject newObject = null;
                        GameObject _obj = null;
                        SummonClass summon = null;
                        if (summoner != null)
                            summon = summoner.GetSummon(interData.objName);
                        if (summon != null)
                        {
                            _obj = summon.summon;
                            _obj.SetActive(true);
                        }
                        else
                        {
                            if (!interObjects.ContainsKey(interData.objName))
                            {
                                newObject = Resources.Load(interData.objName + ".prefab") as GameObject;
                                interObjects.Add(interData.objName, newObject);
                            }
                            else
                                newObject = interObjects[interData.objName];
                            if (newObject != null)
                                _obj = Instantiate(newObject, interData.position, Quaternion.identity);
                        }
                        if (_obj == null)
                            continue;
                        IHaveID _inter = _obj.GetComponent<IHaveID>();
                        if (_inter != null)
                            _inter.SetData(interData);
                        if (objId >= objectsIdCount)
                            objectsIdCount = objId + 1;
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
                            checkpoint.ChangeCheckpoint();
                        }
                        else if (intObjects[i].GetComponent<SecretPlaceTrigger>())
                        {
                            SecretPlaceTrigger secretPlace = intObjects[i].GetComponent<SecretPlaceTrigger>();
                            FindSecretPlace();
                            secretPlace.RevealTruth();
                        }
                        else
                            DestroyImmediate(intObjects[i]);
                    }
                }
            }

            #endregion //InteractiveObjects

            #region NPCs

            List<NPCData> npcInfo = lData.npcInfo;
            Dictionary<string, GameObject> npcObjects = new Dictionary<string, GameObject>();

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
                    else if (objId >= NPCs.Count)
                    {
                        GameObject newNPC = null;
                        GameObject _obj = null;
                        SummonClass summon = null;
                        if (summoner != null)
                            summon = summoner.GetSummon(npcData.objName);
                        if (summon != null)
                        {
                            _obj = summon.summon;
                            _obj.SetActive(true);
                        }
                        else
                        {
                            if (!npcObjects.ContainsKey(npcData.objName))
                            {
                                newNPC = Resources.Load(npcData.objName + ".prefab") as GameObject;
                                npcObjects.Add(npcData.objName, newNPC);
                            }
                            else
                                newNPC = npcObjects[npcData.objName];
                            if (newNPC != null)
                                _obj = InstantiateWithId(newNPC, npcData.position, Quaternion.identity);
                        }
                        if (_obj == null)
                            continue;
                        NPCController _npc = _obj.GetComponent<NPCController>();
                        if (_npc != null)
                            _npc.SetData(npcData);
                        if (objId >= npcsIdCount)
                            npcsIdCount = objId + 1;

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
            _gData.SetGeneralGameData(cNumber, Hero, SpecialFunctions.statistics.ItemCollections);
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

            _gData.SetLevelData(cNumber, Hero,  _collection, drops, GetComponent<GameHistory>().history,
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

        if (setID)
        {
            monstersIdCount = 0; objectsIdCount = 0; npcsIdCount = 0;
        }
        GameObject[] enemiesObjs = GameObject.FindGameObjectsWithTag("enemy");
        monsters = new List<AIController>();
        foreach (GameObject obj in enemiesObjs)
        {
            AIController ai = obj.GetComponent<AIController>();
            if (ai != null? !ai.Dead:false)
            {
                if (setID)
                {
                    ai.ID = monstersIdCount;
                    monstersIdCount++;
                }
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
                {
                    ai.ID = monstersIdCount;
                    monstersIdCount++;
                }
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

        ConsiderObjectsWithTag("interObject", setID);

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
                {
                    npc.SetID(npcsIdCount);
                    npcsIdCount++;
                }
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
                {
                    spirit.SetID(npcsIdCount);
                    npcsIdCount++;
                }
                NPCs.Add(spirit);
            }
        }

        NPCs.Sort((x, y) => { return x.GetID().CompareTo(y.GetID()); });

        #endregion //NPCs

    }

    /// <summary>
    /// Функция, которая заставляет всех патрулирующих 
    /// </summary>
    public void SetPatrollingEnemiesToHome()
    {
        AIController[] ais = FindObjectsOfType<AIController>();
        foreach (AIController ai in ais)
            if (ai.Loyalty == LoyaltyEnum.enemy && ai.Behavior == BehaviorEnum.patrol)
                ai.GoHome();
    }

    /// <summary>
    /// Учесть все объекты с данным тэгом и внести их в список объектов, задав им id при необходимости
    /// </summary>
    public void ConsiderObjectsWithTag(string tag,bool setID)
    {
        GameObject[] intObjs = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in intObjs)
        {
            IHaveID inter = obj.GetComponent<IHaveID>();
            if (inter != null)
            {
                if (setID)
                {
                    inter.SetID(objectsIdCount);
                    objectsIdCount++;
                }
                intObjects.Add(obj);
            }
        }
    }

    /// <summary>
    /// Создать игровой объект и задать ему id
    /// </summary>
    public static GameObject InstantiateWithId(GameObject _gameObject, Vector3 _position, Quaternion _rotation)
    {
        GameObject obj = Instantiate(_gameObject, _position, _rotation) as GameObject;
        SetIDToNewObject(obj);
        return obj;        
    }

    /// <summary>
    /// Задать id новому объекту
    /// </summary>
    public static void SetIDToNewObject(GameObject obj)
    {
        AIController ai = obj.GetComponent<AIController>();
        IHaveID inter = obj.GetComponent<IHaveID>();
        NPCController npc = obj.GetComponent<NPCController>();
        DialogObject dObj = obj.GetComponent<DialogObject>();
        if (ai != null)
        {
            ai.ID = monstersIdCount;
            monstersIdCount++;
        }
        else if (npc != null)
        {
            npc.SetID(npcsIdCount);
            npcsIdCount++;
        }
        else if (inter != null)
        {
            inter.SetID(objectsIdCount);
            objectsIdCount++;
        }
        
        if (dObj!=null)
            if (SpecialFunctions.dialogWindow != null)
            {
                
            }
    }


    #region gameEffects

    /*
    /// <summary>
    /// Добавить бафф, описанный в геймконтроллере, запрашивающему персонажу и задать этому баффу необходимые параметры
    /// </summary>
    /// <param name="_bData">Данные о баффе</param>
    /// <param name="_char">Персонаж, который запрашивает бафф</param>
    public void SetBuffData(BuffData _bData, CharacterController _char)
    {
        if (buffFunctions.ContainsKey(_bData.buffName))
            buffFunctions[_bData.buffName].Invoke(_char, _bData.buffDuration, _bData.buffArgument, _bData.buffID);
    }*/

    /// <summary>
    /// Вызвать случайный игровой эффект (функция вызывет с некоторым шансом случайный положительный исследовательский объект при выполнении задания)
    /// </summary>
    public void AddRandomGameEffect()
    {
        float rand = UnityEngine.Random.RandomRange(0f, 1f);
        if (rand < treasureHunterProbability)
        {
            StartTreasureHunt();
            return;
        }
        else
            rand -= treasureHunterProbability;
        if (rand < collectorProbability)
            StartСollectorProcess();
    }

    /// <summary>
    /// Вызвать случайный игровой эффект
    /// </summary>
    /// <param name="_char">Персонаж, который вызывает случайный эффект (например, своей смертью)</param>
    public void AddRandomGameEffect(CharacterController _char)
    {
        float rand = UnityEngine.Random.RandomRange(0f, 1f);
        if (rand < ancestorsRevengeProbability)
        {
            StartAncestorsRevenge(_char);
            return;
        }
        else
            rand -= ancestorsRevengeProbability;

        if (rand < tribalLeaderProbability)
        {
            StartTribalLeader(_char);
            return;
        }
        else
            rand -= tribalLeaderProbability;

        if (rand < battleCryProbability)
        {
            StartBattleCry();
            return;
        }
        else
            rand -= battleCryProbability;

        if (rand < ancientDarknessProbability)
        {
            StartAncientDarkness();
            return;
        }
        else
            rand -= ancientDarknessProbability;

        if (rand < totemAnimalProbability)
        {
            StartTotemAnimal();
            return;
        }
        else
            rand -= totemAnimalProbability;

        if (rand<tribalRitualProbability)
            StartTribalRitual();
    }

    /// <summary>
    /// Остановить действия указанного баффа (заданного в этом скрипте) на персонажа.
    /// </summary>
    /// <param name="_bData">Данные о баффе</param>
    /// <param name="_char"></param>
    public void StopBuff(BuffData _bData)
    {
        if (stopBuffFunctions.ContainsKey(_bData.buffName))
            stopBuffFunctions[_bData.buffName].Invoke(_bData.buffArgument, _bData.buffID);
    }

    /// <summary>
    /// Вернуть по названию баффа текст, который должен вывестись на экран
    /// </summary>
    /// <param name="_bName">Название баффа в коде</param>
    public string GetBuffText(string _bName)
    {
        if (buffNamesDict.ContainsKey(_bName))
            return "Вы находитесь под действием эффекта \"" + buffNamesDict[_bName]+"\"";
        return "";
    }

    /// <summary>
    /// Инициализировать действие баффа "Месть предков"
    /// </summary>
    /// <param name="_char">Смерть какого персонажа вызвал этот бафф (или на какого персонажа он должен подействовать</param>
    /// <param name="_time">Как долго он будет длит</param>
    /// <param name="argument">Какой тип урона наносил персонаж, смертью которого был вызван этот эффект (0, если это неизвестно)</param>
    void StartAncestorsRevenge(CharacterController _char)
    {
        if (activeGameEffects.Contains("AncestorsRevenge"))
            return;
        WeaponClass _weapon = Hero.CurrentWeapon;
        DamageType newType = _weapon.attackType;
        DamageType dType = ((AIController)_char).AttackParametres.damageType;
        if (!(_char is AIController))
            return;
        switch (dType)
        {
            case DamageType.Fire:
                {
                    newType = DamageType.Water;
                    break;
                }
            case DamageType.Cold:
                {
                    newType = DamageType.Fire;
                    break;
                }
            case DamageType.Water:
                {
                    newType = DamageType.Fire;
                    break;
                }
            case DamageType.Poison:
                {
                    newType = DamageType.Crushing;
                    break;
                }
            default:
                break;
        }
        if (newType != _weapon.attackType)
        {
            _weapon.attackType = newType;
            Hero.AddBuff(new BuffClass("AncestorsRevenge", Time.fixedTime, ancestorsRevengeTime));
            activeGameEffects.Add("AncestorsRevenge");
            StartCoroutine("AncestorsRevengeProcess");
        }
    }

    /// <summary>
    /// Процесс действия баффа "Месть предков"
    /// </summary>
    /// <param name="_time">Время действия</param>
    IEnumerator AncestorsRevengeProcess()
    {
        yield return new WaitForSeconds(ancestorsRevengeTime);
        GameStatistics statistics = GetComponent<GameStatistics>();
        if (statistics != null)
        {
            WeaponClass _weapon = SpecialFunctions.Player.GetComponent<HeroController>().CurrentWeapon;
            if (statistics.WeaponDict.ContainsKey(_weapon.itemName))
                _weapon.attackType = statistics.WeaponDict[_weapon.itemName].attackType;
        }
        hero.RemoveBuff("AncestorsRevenge");
        activeGameEffects.Remove("AncestorsRevenge");
    }

    /// <summary>
    /// Остановить бафф "Месть предков"
    /// </summary>
    void StopAncestorsRevenge(int argument, string id)
    {
        if (!activeGameEffects.Contains("AncestorsRevenge"))
            return;
        StopCoroutine("AncestorsRevengeProcess");
        GameStatistics statistics = GetComponent<GameStatistics>();
        if (statistics != null)
        {
            WeaponClass _weapon = hero.CurrentWeapon;
            if (statistics.WeaponDict.ContainsKey(_weapon.itemName))
                _weapon.attackType = statistics.WeaponDict[_weapon.itemName].attackType;
        }
        hero.RemoveBuff("AncestorsRevenge");
        activeGameEffects.Remove("AncestorsRevenge");
    }

    /// <summary>
    /// Начать действие эффекта "Вождь племени"
    /// </summary>
    /// <param name="_char">На какого персонажа действует эффект</param>
    void StartTribalLeader(CharacterController _char)
    {
        if (!(_char is AIController) || activeGameEffects.Contains("TribalLeader"))
            return;
        ((AIController)_char).Loyalty = LoyaltyEnum.ally;
        _char.AddBuff(new BuffClass("TribalLeader", Time.fixedTime, tribalLeaderTime));
        Hero.AddBuff(new BuffClass("TribalLeader", Time.fixedTime, tribalLeaderTime));
        activeGameEffects.Add("TribalLeader");
        StartCoroutine("TribalLeaderProcess",_char);
    }

    /// <summary>
    /// Процесс действия эффекта "Вождь племени"
    /// </summary>
    /// <param name="_char">Персонаж, на которого действует эффект</param>
    /// <param name="_time">Длительность действия эффекта</param>
    IEnumerator TribalLeaderProcess(CharacterController _char)
    {
        yield return new WaitForSeconds(tribalLeaderTime);
        if (_char != null ? !_char.Dead : false)
            _char.TakeDamage(10000f, DamageType.Physical);
    }

    /// <summary>
    /// Остановить действие эффекта "Вождь племенеи"
    /// </summary>
    void StopTribalLeader(int argument, string id)
    {
        if (!activeGameEffects.Contains("TribalLeader"))
            return;
        Hero.RemoveBuff("TribalLeader");
        activeGameEffects.Remove("TribalLeader");
        StopCoroutine("TribalLeaderProcess");
    }

    /// <summary>
    /// Вызвать "Боевой клич"
    /// </summary>
    void StartBattleCry()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("enemy");
        Vector2 cryPosition = Hero.transform.position;
        foreach (GameObject enemy in enemies)
        {
            AIController ai = enemy.GetComponent<AIController>();
            if (!ai)
                continue;
            if (Vector2.SqrMagnitude((Vector2)enemy.transform.position - cryPosition) <= battleCryRadius*battleCryRadius)
                ai.HearBattleCry(cryPosition);
        }
        hero.BattleCry();
    }

    /// <summary>
    /// Вызвать эффект "Охотник за сокровищами"
    /// </summary>
    void StartTreasureHunt()
    {
        if (activeGameEffects.Contains("TreasureHunter"))
            return;
        else if (activeGameEffects.Contains("Collector"))
        {
            StopCollectorProcess(0,"");
        }
        Hero.AddBuff(new BuffClass("TreasureHunter", Time.fixedTime,treasureHunterTime));
        activeGameEffects.Add("TreasureHunter");
        GameObject _treasureHunterArrow = Instantiate(treasureHunterArrow, Hero.transform.position + Vector3.up * compassArrowOffsetY, Quaternion.identity, hero.transform) as GameObject;
        TreasureHuntArrow thArrow = _treasureHunterArrow.GetComponent<TreasureHuntArrow>();
        thArrow.DestroyEvent += HandleCompassArrowDestroy;
        thArrow.Initialize(treasureHunterTime);
    }

    /// <summary>
    /// Прервать эффект "Охотник за сокровищами"
    /// </summary>
    void StopTreasureHunt(int argument, string id)
    {
        if (!activeGameEffects.Contains("TreasureHunter"))
            return;
        TreasureHuntArrow thArrow = Hero.GetComponentInChildren<TreasureHuntArrow>();
        if (thArrow != null)
            thArrow.DestroyArrow();
    }

    /// <summary>
    /// Вызвать эффект "Коллекционер"
    /// </summary>
    void StartСollectorProcess()
    {
        if (activeGameEffects.Contains("Collector"))
            return;
        else if (activeGameEffects.Contains("TreasureHunter"))
        {
            StopTreasureHunt(0,"");
        }
        Hero.AddBuff(new BuffClass("Collector", Time.fixedTime, collectorTime));
        activeGameEffects.Add("Collector");
        GameObject _collectorArrow = Instantiate(collectorArrow, Hero.transform.position + Vector3.up * compassArrowOffsetY, Quaternion.identity, hero.transform) as GameObject;
        CollectorArrow colArrow = _collectorArrow.GetComponent<CollectorArrow>();
        colArrow.DestroyEvent += HandleCompassArrowDestroy;
        colArrow.Initialize(collectorTime);
    }

    /// <summary>
    /// Прервать эффект "Коллекционер"
    /// </summary>
    void StopCollectorProcess(int argument, string id)
    {
        if (!activeGameEffects.Contains("Collector"))
            return;
        CollectorArrow colArrow = Hero.GetComponentInChildren<CollectorArrow>();
        if (colArrow != null)
            colArrow.DestroyArrow();
    }

    /// <summary>
    /// Начать действие эффекта "Древняя тьма"
    /// </summary>
    void StartAncientDarkness()
    {
        if (activeGameEffects.Contains("AncientDarkness"))
            return;
        StartCoroutine("AncientDarknessProcess");
    }

    /// <summary>
    /// Процесс действия эффекта "Древняя тьма"
    /// </summary>
    /// <param name="_time">Длительность эффекта</param>
    IEnumerator AncientDarknessProcess()
    {
        SpriteLightKitImageEffect lightManager = SpecialFunctions.СamController.GetComponent<SpriteLightKitImageEffect>();
        int prevIntensity = Mathf.RoundToInt(lightManager.intensity * 100f);
        Hero.AddBuff(new BuffClass("AncientDarkness", Time.fixedTime, ancientDarknessTime, prevIntensity, (lightManager.HDRRatio > .1f ? "changed" : "")));
        activeGameEffects.Add("AncientDarkness");
        lightManager.intensity = Mathf.Clamp(lightManager.intensity - 1f, 0f, 2f);
        bool changedHDRRatio = false;
        if (lightManager.HDRRatio > 0.1f)
        {
            changedHDRRatio = true;
            lightManager.HDRRatio -= .1f;
        }
        yield return new WaitForSeconds(ancientDarknessTime);
        lightManager.intensity = prevIntensity/100f;
        if (changedHDRRatio)
            lightManager.HDRRatio += .1f;
        hero.RemoveBuff("AncientDarkness");
        activeGameEffects.Remove("AncientDarkness");
    }

    /// <summary>
    /// Остановить действие эффекта "Древняя тьма"
    /// </summary>
    void StopAncientDarkness(int argument, string id)
    {
        if (!activeGameEffects.Contains("AncientDarkness"))
            return;
        StopCoroutine("AncientDarknessProcess");
        SpriteLightKitImageEffect lightManager = SpecialFunctions.СamController.GetComponent<SpriteLightKitImageEffect>();
        lightManager.intensity = argument / 100f;
        if (id == "changed")
            lightManager.HDRRatio += .1f;
        hero.RemoveBuff("AncientDarkness");
        activeGameEffects.Remove("AncientDarkness");
    }

    /// <summary>
    /// Вызвать эффект "Тотемное животное"
    /// </summary>
    void StartTotemAnimal()
    {
        if (activeGameEffects.Contains("TotemAnimal"))
            return;
        Hero.SummonTotemAnimal();
        activeGameEffects.Add("TotemAnimal");
    }

    /// <summary>
    /// Прервать эффект "Тотемное животное"
    /// </summary>
    void StopTotemAnimal(int argument,string id)
    {
        if (!activeGameEffects.Contains("TotemAnimal"))
            return;
        hero.RemoveBuff("TotemAnimal");
        activeGameEffects.Remove("TotemAnimal");
    }

    /// <summary>
    /// Вызвать эффект "Племенной ритуал"
    /// </summary>
    void StartTribalRitual()
    {
        Hero.StartTribalRitual();
    }

    #endregion //gameEffects

    /// <summary>
    /// Функция завершения данного уровня
    /// </summary>
    /// <param name="nextLevelName">Название следующего уровня</param>
    public void CompleteLevel(string nextLevelName)
    {
        SpecialFunctions.StartStoryEvent(this, EndGameEvent, new StoryEventArgs());
        GameStatistics statistics = SpecialFunctions.statistics;
        List<ItemCollection> itemCollections = statistics != null ? statistics.ItemCollections : null;
        string sceneName = SceneManager.GetActiveScene().name;
        ItemCollection _collection = itemCollections != null ? itemCollections.Find(x=>sceneName.Contains(x.settingName)):null;
        levelCompleteScreen.SetLevelCompleteScreen(nextLevelName, secretsFoundNumber, secretsTotalNumber, _collection);
    }

    #region eventHandlers

    /// <summary>
    /// Обработка события "Стрелка компаса перестала работать"
    /// </summary>
    void HandleCompassArrowDestroy(object sender, EventArgs e)
    {
        if (sender is TreasureHuntArrow)
        {
            Hero.RemoveBuff("TreasureHunter");
            activeGameEffects.Remove("TreasureHunter");
        }
        else if (sender is CollectorArrow)
        {
            Hero.RemoveBuff("Collector");
            activeGameEffects.Remove("Collector");
        }
    }

    #endregion //eventHandlers

}
