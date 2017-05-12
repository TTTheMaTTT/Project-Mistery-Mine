using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using InControl;
using Steamworks;

/// <summary>
/// Объект, ответственный за управление игрой
/// </summary>
public class GameController : MonoBehaviour
{

    #region consts

    #region gameEffectConsts

    protected const float ancestorsRevengeTime = 60f;//Время и шанс эффекта "Месть предков"
    protected const float tribalLeaderTime = 30f;//Время и шанс эффекта "Вождь племени"
    protected const float battleCryRadius = 6f;
    protected const float treasureHunterTime = 30f;//Время действия и шанс эффекта "Кладоискатель"
    protected const float collectorTime = 15f;//Время действия и шанс эффекта "Коллекционер"
    protected const float compassArrowOffsetY = 0.17f;//Насколько смещена по вертикали стрелка компаса относительно персонажа
    protected const float ancientDarknessTime = 15f;//Время и шанс эффекта "Древняя тьма"

    protected const float deathTime = 2.1f;

    #endregion //gameEffectConsts

    protected const float nextLevelTime = 2.1f;//Время, за которое происходит переход на следующий уровень

    protected const int maxLevelWithSecretsCount = 18;//число уровней, в которых можно найти секретные места
    protected const int gameEffectsCount = 10;//Кол-во игровых эффектов
    protected const int achievementVulnerableEnemiesCount = 5;//Сколько врагов должно быть убито с использованием из уязвимостей, чтобы засчиталось достижение

    #endregion //consts

    #region dictionaries

    //protected Dictionary<string, BuffFunction> buffFunctions = new Dictionary<string, BuffFunction>();
    protected Dictionary<string, StopBuffFunction> stopBuffFunctions = new Dictionary<string, StopBuffFunction>();
    protected Dictionary<string, MultiLanguageText> buffNamesDict = new Dictionary<string, MultiLanguageText>()
    { {"AncestorsRevenge", new MultiLanguageText("Месть предков","Revenge of the ancestors","Помста предків","Zemsta przodków","Vengeance des ancêtres")},
    {"TribalLeader", new MultiLanguageText("Вождь племени", "The Tribe Leader", "Вождь племені","Wódz plemienia","Chef de la tribu")},
    {"TreasureHunter", new MultiLanguageText("Кладоскатель", "The Seeker of treasures", "Шукач скарбів", "Poszukiwacz skarbów","Chercheur de trésors")},
    {"Collector", new MultiLanguageText("Коллекционер", "The Collectioneer", "Колекціонер","Kolekcjoner","Collectioneur")},
    {"AncientDarkness",new MultiLanguageText("Древняя тьма", "The Ancient Darkness","Древня пітьма","Starożytna Ciemność","Obscurité ancienne")},
    { "TotemAnimal", new MultiLanguageText("Тотемное животное", "The Spiritual Animal", "Тотемна тварина","Duchowe zwierze","Animal de totem")},
    { "TribalRitual", new MultiLanguageText("Ритуал племени", "The Ritual of the tribe", "Ритуал племені", "Rytułał plemienia", "Rituel de tribu")} };

    protected Dictionary<string, GameEffectDeathFunction> deathEffectsDictionary = new Dictionary<string, GameEffectDeathFunction>();//Словарь игровых эффектов, вызываемых при смерти персонажей (монстров)
    protected Dictionary<string, GameEffectUsualFunction> usualEffectsDictionary = new Dictionary<string, GameEffectUsualFunction>();//Словарь игровых эффектов, вызываемых при, например, 
                                                                                                                                     //срабатывании исследовательского триггера

    #endregion //dictionaries

    #region delegates

    //protected delegate void BuffFunction(CharacterController _char, float _time, int argument, string id);
    protected delegate void StopBuffFunction(int argument, string id);

    protected delegate void GameEffectDeathFunction(CharacterController _char);//Делегат функций - игровых эффектов, которые вызываются при смерти персонажей (монстров)
    protected delegate void GameEffectUsualFunction();//Делегат функций - игровых эффектов, которые вызываются при достаточно простых условиях (например, при возникновении какой-нибудь игровой истории)

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

    private List<TrinketEffectClass> deathEffects = new List<TrinketEffectClass>(), usualEffects = new List<TrinketEffectClass>();//Список доступных игровых эффектов
    private List<string> activeGameEffects = new List<string>();//Названия активных игровычх эффектов

    [SerializeField]private GameObject treasureHunterArrow;//Стрелка компаса охотника за сокровищами
    [SerializeField]private GameObject collectorArrow;//Стрелка компаса коллекционера

    private AudioSource ambientSource, musicSource, soundSource;//Источники звуков окружающего мира и музыки, а также источник игровых звуков
    [SerializeField]
    private List<AudioClip> ambientClips = new List<AudioClip>(), musicClips = new List<AudioClip>(), soundClips = new List<AudioClip>();

    [SerializeField]
    private List<QuestLine> languageChanges = new List<QuestLine>();
    public List<QuestLine> LanguageChanges { get { return languageChanges; } }

    private List<string> gameEffectsNames = new List<string>();
    private List<string> vulnerableEnemiesNames = new List<string>();
    
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

    public bool haveActiveDevice;

    protected MultiLanguageText underEffectMLText = new MultiLanguageText("На вас действует эффект \"",
                                                                          "You are under effect \"",
                                                                          "На вас діє ефект \"",
                                                                          "Jesteś pod efektem\"", "");

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
        /*if (Input.GetKeyDown(KeyCode.I))
            SpecialFunctions.gameUI.ChangeVisibility();
        if (Input.GetKeyDown(KeyCode.J))
            SpecialFunctions.CamController.ChangeFreeMode();*/

        if (UIElementScript.activePanel!=null)
        {
            bool up = Input.GetButtonDown("Up"), down = Input.GetButtonDown("Down"), left = Input.GetButtonDown("Left"), right = Input.GetButtonDown("Right");
            if (InputCollection.instance.GetButtonDown("InterfaceMoveHorizontal"))
                UIElementScript.activePanel.MoveHorizontal(Mathf.RoundToInt(Mathf.Sign(InputCollection.instance.GetAxis("InterfaceMoveHorizontal"))));
            if (InputCollection.instance.GetButtonDown("InterfaceMoveVertical"))
                UIElementScript.activePanel.MoveVertical(-Mathf.RoundToInt(Mathf.Sign(InputCollection.instance.GetAxis("InterfaceMoveVertical"))));
            if (up)
                UIElementScript.activePanel.MoveVertical(-1);
            if (down)
                UIElementScript.activePanel.MoveVertical(1);
            if (left)
                UIElementScript.activePanel.MoveHorizontal(-1);
            if (right)
                UIElementScript.activePanel.MoveHorizontal(1);
            if (InputCollection.instance.GetButtonDown("Cancel"))
                UIElementScript.activePanel.Cancel();
        }

        if (UIElementScript.activeElement != null)
        {
            if (InputCollection.instance.GetButtonDown("Submit"))
                UIElementScript.activeElement.Activate();
        }
        if (InputCollection.instance.GetButtonDown("Menu"))
            gameMenu.ChangeGameMod();

        //haveActiveDevice = InputManager.ActiveDevice.IsAttached;

        /*if (InputManager.ActiveDevice.GetControl(InputControlType.Action1).IsPressed)
        {
            InputControl inp = InputManager.ActiveDevice.GetControl(InputControlType.Action1);
            InputControl inp1 = InputCollection.instance.GetInputControl("Jump");
            bool k = (inp == inp1);
            bool h = false;
        }*/
    }

    protected void Awake()
    {
        Initialize();
    }

    protected void Start()
    {
        if (GetComponent<InControlManager>()==null)
        {
            //GameObject inputManager = new GameObject("InputManager");
            //inputManager.AddComponent<JoystickController>();
            InControlManager _control=gameObject.AddComponent<InControlManager>();
        }

        SpecialFunctions.SetDark();
        SpecialFunctions.SetFade(false);

        #region RegisterObjects

        //Пройдёмся по всем объектам уровня, дадим им id и посмотрим, как изменятся эти объекты в соответсвтии с сохранениями
        GetLists(!idSetted);
        monstersIdCount = monsters.Count;
        npcsIdCount = NPCs.Count;
        objectsIdCount = intObjects.Count;

        #endregion //RegisterObjects
        GetComponent<GameStatistics>().ClearStatistics();
        LoadGame();

        //monsters = null;
        //intObjects =null;
        //NPCs = null;

        SpecialFunctions.history.Initialize();
        SpecialFunctions.StartStoryEvent(this, StartGameEvent, new StoryEventArgs());
        Resources.UnloadUnusedAssets();

        Debug.LogWarning(GetComponent<GameStatistics>().gameHistoryProgress.GetStoryProgress("strangerStory"));

    }

    protected void Initialize()
    {
        Resources.UnloadUnusedAssets();
        deathEffects = new List<TrinketEffectClass>();
        usualEffects = new List<TrinketEffectClass>();
        monstersIdCount = 0;
        objectsIdCount = 0;
        npcsIdCount = 0;
        SpecialFunctions.InitializeObjects();
        InitializeDictionaries();
        datapath = (Application.streamingAssetsPath)+"/Saves/Profile";
        savesInfoPath = (Application.streamingAssetsPath) + "/SavesInfo.xml";
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
        SecretPlaceTrigger secretPlace1 = null;
        foreach (GameObject secretPlace in secretPlaces)
            if ((secretPlace1 = secretPlace.GetComponent<SecretPlaceTrigger>()) != null ? secretPlace1.ConsiderByGameController : false)
                secretsTotalNumber++;
        activeGameEffects = new List<string>();
        SetHeroDeathLevelEnd();

        AudioSource[] audioSources = SpecialFunctions.CamController.GetComponents<AudioSource>();
        if (audioSources.Length >= 1)
        {
            musicSource = audioSources[0];
            musicSource.volume = PlayerPrefs.GetFloat("MusicVolume");
            if (musicClips.Count > 0)
            {
                musicSource.clip = musicClips[0];
                musicSource.Play();
            }
        }
        if (audioSources.Length >= 2)
        {
            ambientSource = audioSources[1];
            ambientSource.volume = PlayerPrefs.GetFloat("SoundVolume");
            SpecialFunctions.Settings.soundEventHandler += HandleSoundVolumeChange;
            if (ambientClips.Count > 0)
            {
                ambientSource.clip = ambientClips[0];
                ambientSource.Play();
            }
        }
        if (audioSources.Length >= 3)
        {
            soundSource = audioSources[2];
            soundSource.volume = PlayerPrefs.GetFloat("SoundVolume");
        }
    }

    /// <summary>
    /// Инициализировать нужные словари
    /// </summary>
    void InitializeDictionaries()
    {

        deathEffectsDictionary = new Dictionary<string, GameEffectDeathFunction>();
        deathEffectsDictionary.Add("AncestorsRevenge", StartAncestorsRevenge);
        deathEffectsDictionary.Add("TribalLeader", StartTribalLeader);
        deathEffectsDictionary.Add("BattleCry", StartBattleCry);
        deathEffectsDictionary.Add("AncientDarkness", StartAncientDarkness);
        deathEffectsDictionary.Add("TotemAnimal", StartTotemAnimal);
        deathEffectsDictionary.Add("TribalRitual", StartTribalRitual);

        usualEffectsDictionary = new Dictionary<string, GameEffectUsualFunction>();
        usualEffectsDictionary.Add("TreasureHunter", StartTreasureHunt);
        usualEffectsDictionary.Add("Collector", StartСollectorProcess);

        stopBuffFunctions = new Dictionary<string, StopBuffFunction>();
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
    public bool StartDialog(NPCController npc, Dialog dialog)
    {
        return dialogWindow.BeginDialog(npc, dialog);
    }

    /// <summary>
    /// Начать диалог
    /// </summary>
    public bool StartDialog(Dialog dialog)
    {
        return dialogWindow.BeginDialog(dialog);
    }

    /// <summary>
    /// Функция, вызывающаяся при нахождении секретного места
    /// </summary>
    public void FindSecretPlace()
    {
        secretsFoundNumber++;
    }

    /// <summary>
    /// Начать процесс сохранения игры
    /// </summary>
    public void StartSaveGameProcess(int checkpointNumb, bool generally, string levelName)
    {
        StartCoroutine(SaveGameProcess(checkpointNumb, generally, levelName));
    }

    /// <summary>
    /// Процесс сохранения игры. Используется, когда игру надо сохранить после некоторого промежутка времени.ё
    /// </summary>
    IEnumerator SaveGameProcess(int checkpointNumb, bool generally, string levelName)
    {
        yield return new WaitForSeconds(.5f);
        SaveGame(checkpointNumb, generally, levelName);
    }

    /// <summary>
    /// Функция сохранения игры. Поддерживается 2 режима сохранения: сохранения данных игры в целом и сохранение текущего уровня 
    /// </summary>
    public void SaveGame(int checkpointNumb, bool generally, string levelName)
    {
        SpecialFunctions.battleField.KillAllies();//Все союзники героя погибают (ввиду их временного характера)
        Hero.RestoreStats();//При сохранении герой восстанавливает свои характеристики (восстановление здоровья, сброс отрицательных боевых эффектов)
        Hero.ResetAdditionalWeapon();//При сохранении идёт сброс доп оружия
        Serializator.SaveXml(StoreGameData(generally, checkpointNumb), datapath + profileNumber.ToString()+".xml");
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

        if (gGData != null)
        {
            if (Time.fixedTime < gGData.gameAddTime - 1f)
                RefreshGameTime();
            vulnerableEnemiesNames = gGData.vulnerableEnemies;
            gameEffectsNames = gGData.gameEffectsCreated;
        }

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
                    SpecialFunctions.equipWindow.ClearWeaponCells();

                    equip.bag.Clear();
                    foreach (string itemName in eInfo.bagItems)
                        if (gStats.ItemDict.ContainsKey(itemName))
                            Hero.SetItem(gStats.ItemDict[itemName]);

                    equip.weapons.Clear();
                    foreach (string itemName in eInfo.weapons)
                        if (gStats.WeaponDict.ContainsKey(itemName))
                            Hero.SetItem(gStats.WeaponDict[itemName]);

                    foreach (string itemName in eInfo.activeTrinkets)
                        if (gStats.TrinketDict.ContainsKey(itemName))
                        {
                            TrinketClass _trinket = gStats.TrinketDict[itemName];
                            SpecialFunctions.equipWindow.SetActiveTrinket(_trinket);
                        }

                    equip.trinkets.Clear();
                    foreach (string itemName in eInfo.trinkets)
                        if (gStats.TrinketDict.ContainsKey(itemName))
                            Hero.SetItem(gStats.TrinketDict[itemName]);

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

                #endregion //heroEquipment

                Hero.SetBuffs(new BuffListData(new List<BuffClass>()));
                Hero.MaxHealth = gGData.maxHP;
                hero.Health = hero.MaxHealth;

                gStats.ResetStatistics();
                gStats.gameHistoryProgress.SetStoryProgressData(gGData.progressInfo);

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

            SpriteLightKitImageEffect lightManager = SpecialFunctions.CamController.GetComponent<SpriteLightKitImageEffect>();
            if (lightManager != null)
            {
                lightManager.intensity = lData.lightIntensity;
                lightManager.HDRRatio = lData.lightHDR;
            }

            Hero.MaxHealth = lData.maxHP;
            hero.Health = hero.MaxHealth;

            #region heroEquipment

            EquipmentInfo eInfo = lData.eInfo;
            if (eInfo != null && gStats != null)
            {
                EquipmentClass equip = Hero.Equipment;
                SpecialFunctions.equipWindow.ClearWeaponCells();

                equip.bag.Clear();
                foreach (string itemName in eInfo.bagItems)
                    if (gStats.ItemDict.ContainsKey(itemName))
                        Hero.SetItem(gStats.ItemDict[itemName]);

                equip.weapons.Clear();
                foreach (string itemName in eInfo.weapons)
                    if (gStats.WeaponDict.ContainsKey(itemName))
                        Hero.SetItem(gStats.WeaponDict[itemName]);

                foreach (string itemName in eInfo.activeTrinkets)
                    if (gStats.TrinketDict.ContainsKey(itemName))
                    {
                        TrinketClass _trinket = gStats.TrinketDict[itemName];
                        SpecialFunctions.equipWindow.SetActiveTrinket(_trinket);
                    }

                equip.trinkets.Clear();
                foreach (string itemName in eInfo.trinkets)
                    if (gStats.TrinketDict.ContainsKey(itemName))
                        Hero.SetItem(gStats.TrinketDict[itemName]);

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
                    {
                        iCollection.itemsFoundCount = cData.foundItemsCount;
                        for (int i = 0; i < cData.itemsFound.Count && i < iCollection.collection.Count; i++)
                            iCollection.collection[i].itemFound = cData.itemsFound[i];
                    }
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

            List<string> dropObjectNames = dropInfo.dropObjectNames;
            //Сначала надо уничтожить все объекты типа drop на уровне, которых нет в списке названий (это значит, что их уже подобрали)
            GameObject[] drops = GameObject.FindGameObjectsWithTag("drop");
            for (int i = drops.Length - 1; i >= 0; i--)
            {
                string objName = drops[i].gameObject.name;
                if (dropObjectNames.Contains(objName))
                {
                    dropInfo.drops.Remove(dropInfo.drops.Find(x => x.objectName == objName));
                    dropObjectNames.Remove(objName);
                }
                else
                    Destroy(drops[i]);
            }
            drops = GameObject.FindGameObjectsWithTag("heartDrop");
            for (int i = drops.Length - 1; i >= 0; i--)
            {
                string objName = drops[i].gameObject.name;
                if (dropObjectNames.Contains(objName))
                {
                    dropInfo.drops.Remove(dropInfo.drops.Find(x => x.objectName == objName));
                    dropObjectNames.Remove(objName);
                }
                else
                    Destroy(drops[i]);
            }

            //Создадим дропы, которые остались в списке дропов (значит они появились в процессе игры, но не были подобраны игроком)
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
                    else if (gStats.TrinketDict.ContainsKey(_dInfo.itemName))
                    {
                        GameObject newDrop = Instantiate(gStats.itemBase.customDrop, _dInfo.position, Quaternion.identity) as GameObject;
                        newDrop.GetComponent<DropClass>().item = gStats.TrinketDict[_dInfo.itemName];
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
                            DialogObject dObj = _obj.GetComponent<DialogObject>();
                            if (dObj != null)
                                if (SpecialFunctions.dialogWindow != null)
                                    SpecialFunctions.dialogWindow.AddDialogObjectInDictionary(dObj);
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
                                _obj = InstantiateWithId(newMonster, enData.position, Quaternion.identity);
                             
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
                            DialogObject dObj = _obj.GetComponent<DialogObject>();
                            if (dObj != null)
                                if (SpecialFunctions.dialogWindow != null)
                                    SpecialFunctions.dialogWindow.AddDialogObjectInDictionary(dObj);
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
                                _obj = InstantiateWithId(newObject, interData.position, Quaternion.identity);
                        }
                        if (_obj == null)
                            continue;
                        IHaveID _inter = _obj.GetComponent<IHaveID>();
                        if (_inter != null)
                        {
                            _inter.SetData(interData);

                        }
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
                            if (secretPlace.ConsiderByGameController)
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
            Dictionary<int, NPCController> npcDict = new Dictionary<int, NPCController>();
            Dictionary<string, GameObject> npcObjects = new Dictionary<string, GameObject>();

            if (npcInfo != null && NPCs != null)
            {
                foreach (NPCData npcData in npcInfo)
                {
                    int objId = npcData.objId;
                    if (objId < NPCs.Count ? NPCs[objId] != null : false)
                    {
                        npcDict.Add(objId, NPCs[objId]);
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
                            DialogObject dObj = _obj.GetComponent<DialogObject>();
                            if (dObj != null)
                                if (SpecialFunctions.dialogWindow != null)
                                    SpecialFunctions.dialogWindow.AddDialogObjectInDictionary(dObj);
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
                        npcDict.Add(_npc.GetID(), _npc);
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

            #region dialogs

            DialogWindowScript dWindow = SpecialFunctions.dialogWindow;
            DialogData dInfo = lData.dInfo;
            if (dWindow != null && dInfo!=null)
            {
                foreach (DialogInfo dialogInfo in dInfo.dialogs)
                    if (npcDict.ContainsKey(dialogInfo.npcID))
                    {
                        NPCController dialogNPC = npcDict[dialogInfo.npcID];
                        Dialog _dialog = dialogNPC.Dialogs.Find(x => x.dialogName == dialogInfo.dialogName);
                        if (_dialog != null)
                            dWindow.DialogQueue.Add(new DialogQueueElement(_dialog, dialogNPC));
                    }
            }

            #endregion //dialogs

        }

        hero.ResetAdditionalWeapon();

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
    GameData StoreGameData(bool general, int cNumber)
    {
        SavesInfo savesInfo = Serializator.DeXmlSavesInfo(savesInfoPath);
        bool haveData = true;
        GameData _gData = null;
        if (!Serializator.HasSavesInfo(savesInfoPath))
            haveData = false;
        if (savesInfo == null)
            haveData = false;
        if (!savesInfo.saves[profileNumber].hasData)
            haveData = false;
        if (haveData)
            _gData = Serializator.DeXml(datapath + profileNumber.ToString() + ".xml");
        else
            _gData= new GameData();
        if (_gData == null)
            _gData = new GameData();
        if (general)
        {
            _gData.ResetLevelData();
            _gData.SetGeneralGameData(cNumber, Hero, SpecialFunctions.statistics.ItemCollections,Time.fixedTime,vulnerableEnemiesNames,gameEffectsNames);
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
                                                                                                        GetComponent<GameStatistics>(),enInfo,intInfo, npcInfo, SpecialFunctions.dialogWindow);
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
            gData.SetGameStatistics(Time.fixedTime, vulnerableEnemiesNames, gameEffectsNames);
        }
        Serializator.SaveXml(gData, datapath + profileNumber.ToString() + ".xml");
    }

    /// <summary>
    /// Сменить информацию об используемом игроком оружии в данный момент
    /// </summary>
    /// <param name="general">Информация должна занестись в данные уровня (false) или в данные игры(true)</param>
    public void ChangeInformationAboutActiveWeapon(bool general, string weaponName)
    {
        Hero.ResetAdditionalWeapon();//При сохранении идёт сброс доп оружия
        GameData gData = Serializator.DeXml(datapath + profileNumber.ToString() + ".xml");
        if (gData != null)
        {
            if (general || gData.lData == null ? true : !gData.lData.active)
                if (gData.gGData.eInfo.weapons.Contains(weaponName))
                    gData.gGData.eInfo.weapon = weaponName;
            else if (gData.lData.eInfo.weapons.Contains(weaponName))
                gData.lData.eInfo.weapon = weaponName;
            Serializator.SaveXml(gData, datapath + profileNumber.ToString() + ".xml");
        }
    }

    /// <summary>
    /// Сменить информацию об инвентаре
    /// </summary>
    /// <param name="general">Информация должна занестись в данные уровня (false) или в данные игры(true)</param>
    public void ChangeInformationAboutEquipment(bool general)
    {
        Hero.ResetAdditionalWeapon();//При сохранении идёт сброс доп оружия
        GameData gData = Serializator.DeXml(datapath + profileNumber.ToString() + ".xml");
        if (gData != null)
        {
            if (general || gData.lData == null ? true : !gData.lData.active)
                gData.gGData.eInfo.SetActiveItems(hero.CurrentWeapon,hero.Equipment.activeTrinkets);
            else
                gData.lData.eInfo.SetActiveItems(hero.CurrentWeapon, hero.Equipment.activeTrinkets);
            Serializator.SaveXml(gData, datapath + profileNumber.ToString() + ".xml");
        }
    }

    /// <summary>
    /// Сделать переучёт игрового времени (доп время добавляется к основному времени, счётчик доп времени работает заново)
    /// </summary>
    public void RefreshGameTime()
    {
        GameData gData = Serializator.DeXml(datapath + profileNumber.ToString() + ".xml");
        if (gData != null)
        {
            gData.gGData.RefreshGameTime();
            Serializator.SaveXml(gData, datapath + profileNumber.ToString() + ".xml");
        }
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
        for (int i = monsters.Count - 1; i >= 0; i--)
            if (monsters[i].ID == -1)
                monsters.RemoveAt(i);

        intObjects = new List<GameObject>();
        ConsiderObjectsWithTag("lever", setID);

        ConsiderObjectsWithTag("door", setID);

        ConsiderObjectsWithTag("checkpoint", setID);

        ConsiderObjectsWithTag("mechanism", setID);

        ConsiderObjectsWithTag("box", setID);

        ConsiderObjectsWithTag("interObject", setID);

        ConsiderSaveMeObjects(setID);

        intObjects.Sort((x, y) => { return x.GetComponent<IHaveID>().GetID().CompareTo(y.GetComponent<IHaveID>().GetID()); });
        for (int i = intObjects.Count - 1; i >= 0; i--)
            if (intObjects[i].GetComponent<IHaveID>().GetID() == -1)
                intObjects.RemoveAt(i);

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
        for (int i = NPCs.Count - 1; i >= 0; i--)
            if (NPCs[i].GetID() == -1)
                NPCs.RemoveAt(i);

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
            if (inter != null? inter.GetID()!=-1:false)
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
    /// Учесть все объекты, имеющие компонент SaveMeScript и внести их в список объектв, задав им id при необходимости
    /// </summary>
    public void ConsiderSaveMeObjects(bool setID)
    {
        SaveMeScript[] saveMeScripts = FindObjectsOfType<SaveMeScript>();
        foreach (SaveMeScript saveMe in saveMeScripts)
        {
            IHaveID inter = saveMe.GetComponent<IHaveID>();
            GameObject obj = saveMe.gameObject;
            if (inter == null)
                continue;
            if (intObjects.Contains(obj))
                continue;
            if (setID)
            {
                inter.SetID(objectsIdCount);
                objectsIdCount++;
            }
            intObjects.Add(obj);
        }
    }

    /// <summary>
    /// Создать игровой объект и задать ему id
    /// </summary>
    public GameObject InstantiateWithId(GameObject _gameObject, Vector3 _position, Quaternion _rotation)
    {
        GameObject obj = Instantiate(_gameObject, _position, _rotation) as GameObject;
        SetIDToNewObject(obj);
        return obj;        
    }

    /// <summary>
    /// Задать id новому объекту
    /// </summary>
    public void SetIDToNewObject(GameObject obj)
    {
        AIController ai = obj.GetComponent<AIController>();
        IHaveID inter = obj.GetComponent<IHaveID>();
        NPCController npc = obj.GetComponent<NPCController>();
        DialogObject dObj = obj.GetComponent<DialogObject>();
        if (ai != null)
        {
            if (!monsters.Contains(ai))
            {
                ai.ID = monstersIdCount;
                monstersIdCount++;
                monsters.Add(ai);
            }
        }
        else if (npc != null)
        {
            if (!NPCs.Contains(npc))
            {
                npc.SetID(npcsIdCount);
                npcsIdCount++;
                NPCs.Add(npc);
            }
        }
        else if (inter != null)
        {
            if (!intObjects.Contains(obj))
            {
                inter.SetID(objectsIdCount);
                objectsIdCount++;
                intObjects.Add(obj);
            }
        }
        
        if (dObj!=null)
            if (SpecialFunctions.dialogWindow != null)
                SpecialFunctions.dialogWindow.AddDialogObjectInDictionary(dObj);
    }

    /// <summary>
    /// Добавить достижение
    /// </summary>
    public void GetAchievement(string _achievementID)
    {
        /*if (SteamManager.Initialized)
        {
            bool isAchivementAlreadyGet;
            if (SteamUserStats.GetAchievement(_achievementID, out isAchivementAlreadyGet) && !isAchivementAlreadyGet)
            {
                SteamUserStats.SetAchievement(_achievementID);
                SteamUserStats.StoreStats();
            }
        }*/
    }

    /// <summary>
    /// Добавить произошедший игровой эффект в список игровых эффектов
    /// </summary>
    public void AddGameEffectName(string _gEName)
    {
        if (!gameEffectsNames.Contains(_gEName))
        {
            gameEffectsNames.Add(_gEName);
            if (gameEffectsNames.Count >= gameEffectsCount)
                GetAchievement("LUCKY_ONE");
        }
    }

    /// <summary>
    /// Добавить убитого своей уязвимостью врага в список уязвимых врагов
    /// </summary>
    public void AddVulnerableKilledEnemy(string _eName)
    {
        if (!vulnerableEnemiesNames.Contains(_eName))
        {
            vulnerableEnemiesNames.Add(_eName);
            if (vulnerableEnemiesNames.Count >= achievementVulnerableEnemiesCount)
                GetAchievement("WISE_WARRIOR");
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
    /// Добавить в списки эффектов все эффекты данного предмета
    /// </summary>
    /// <param name="trinket">Предмет, который даёт возможность создавать пассивные эффекты</param>
    public void AddTrinketEffect(TrinketClass trinket)
    {
        foreach (TrinketEffectClass effect in trinket.trinketEffects)
            AddTrinketEffect(effect);

    }

    /// <summary>
    /// Добавить новый игровой эффект в список доступных эффектов
    /// </summary>
    public void AddTrinketEffect(TrinketEffectClass effect)
    {
        if (effect.effectType == TrinketEffectTypeEnum.monsterDeath)
            deathEffects.Add(effect);
        else if (effect.effectType == TrinketEffectTypeEnum.investigation)
            usualEffects.Add(effect);

    }

    /// <summary>
    /// Убрать все эффекты тринкета
    /// </summary>
    public void RemoveEffectsOfTrinket(TrinketClass trinket)
    {
        foreach (TrinketEffectClass effect in trinket.trinketEffects)
        {
            if (effect.effectType == TrinketEffectTypeEnum.monsterDeath)
            {
                TrinketEffectClass _effect = deathEffects.Find(x => x.effectName == effect.effectName);
                if (_effect!= null)
                    deathEffects.Remove(_effect);
            }
            else if (effect.effectType == TrinketEffectTypeEnum.investigation)
            {
                TrinketEffectClass _effect = usualEffects.Find(x => x.effectName == effect.effectName);
                if (_effect != null)
                    usualEffects.Remove(_effect);
            }

        }
    }

    /// <summary>
    /// Вызвать случайный игровой эффект (функция вызывет с некоторым шансом случайный положительный исследовательский объект при выполнении задания)
    /// </summary>
    public void AddRandomUsualGameEffect()
    {
        float normKoof = 0f;
        foreach (TrinketEffectClass effect in usualEffects)
        {
            normKoof += effect.effectProbability;
        }
        if (normKoof < 1f)
            normKoof = 1f;
        float rand = UnityEngine.Random.RandomRange(0f, 1f);
        foreach (TrinketEffectClass effect in usualEffects)
        {
            if (rand < effect.effectProbability / normKoof)
            {
                if (usualEffectsDictionary.ContainsKey(effect.effectName))
                    usualEffectsDictionary[effect.effectName]();
                break;
            }
            else
                rand -= effect.effectProbability;
        }
    }

    /// <summary>
    /// Вызвать случайный игровой эффект, связанный со смертью персонажа
    /// </summary>
    /// <param name="_char">Персонаж, который вызывает случайный эффект своей смертью</param>
    public void AddRandomDeathGameEffect(CharacterController _char)
    {
        float normKoof = 0f;
        foreach (TrinketEffectClass effect in deathEffects)
        {
            normKoof += effect.effectProbability;
        }
        if (normKoof < 1f)
            normKoof = 1f;
        float rand = UnityEngine.Random.RandomRange(0f, 1f);
        foreach (TrinketEffectClass effect in deathEffects)
        {
            if (rand < effect.effectProbability / normKoof)
            {
                if (deathEffectsDictionary.ContainsKey(effect.effectName))
                    deathEffectsDictionary[effect.effectName](_char);
                break;
            }
            else
                rand -= effect.effectProbability;
        }
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
            return underEffectMLText.GetText(SettingsScript.language) + buffNamesDict[_bName].GetText(SettingsScript.language)+"\"";
        return "";
    }

    /// <summary>
    /// Инициализировать действие баффа "Месть предков"
    /// </summary>
    /// <param name="_char">Смерть какого персонажа вызвал этот бафф (или на какого персонажа он должен подействовать</param>
    /// <param name="_time">Как долго он будет длит</param>
    void StartAncestorsRevenge(CharacterController _char)
    {
        AddGameEffectName("AncestorsRevenge");
        if (activeGameEffects.Contains("AncestorsRevenge"))
            return;
        WeaponClass _weapon = Hero.CurrentWeapon;
        DamageType newType = _weapon.attackType;
        string newTypeName = "";
        DamageType dType = ((AIController)_char).AttackParametres.damageType;
        if (!(_char is AIController))
            return;
        switch (dType)
        {
            case DamageType.Fire:
                {
                    newType = DamageType.Water;
                    newTypeName = "Water";
                    break;
                }
            case DamageType.Cold:
                {
                    newType = DamageType.Fire;
                    newTypeName = "Fire";
                    break;
                }
            case DamageType.Water:
                {
                    newType = DamageType.Fire;
                    newTypeName = "Fire";
                    break;
                }
            case DamageType.Poison:
                {
                    newType = DamageType.Crushing;
                    newTypeName = "Crushing";
                    break;
                }
            default:
                break;
        }
        if (newType != _weapon.attackType)
        {
            _weapon.attackType = newType;
            Hero.AddBuff(new BuffClass("AncestorsRevenge", Time.time, ancestorsRevengeTime));
            Hero.SpawnEffect("AncestorsRevenge" + newTypeName);
            activeGameEffects.Add("AncestorsRevenge");
            StartCoroutine("AncestorsRevengeProcess");
        }
    }

    /// <summary>
    /// Процесс действия баффа "Месть предков"
    /// </summary>
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
        AddGameEffectName("TribalLeader");
        if (!(_char is AIController) || activeGameEffects.Contains("TribalLeader"))
            return;
        ((AIController)_char).Loyalty = LoyaltyEnum.ally;
        _char.AddBuff(new BuffClass("TribalLeader", Time.time, tribalLeaderTime));
        _char.SpawnEffect("TribalLeader");
        Hero.AddBuff(new BuffClass("TribalLeader", Time.time, tribalLeaderTime));
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
            _char.TakeDamage(new HitParametres(10000f, DamageType.Physical));
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
    void StartBattleCry(CharacterController _char)
    {
        AddGameEffectName("BattleCry");
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
        AddGameEffectName("TreasureHunter");
        if (activeGameEffects.Contains("TreasureHunter"))
            return;
        else if (activeGameEffects.Contains("Collector"))
        {
            StopCollectorProcess(0,"");
        }
        Hero.AddBuff(new BuffClass("TreasureHunter", Time.time,treasureHunterTime));
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
        AddGameEffectName("Collector");
        if (activeGameEffects.Contains("Collector"))
            return;
        else if (activeGameEffects.Contains("TreasureHunter"))
        {
            StopTreasureHunt(0,"");
        }
        Hero.AddBuff(new BuffClass("Collector", Time.time, collectorTime));
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
    void StartAncientDarkness(CharacterController _char)
    {
        AddGameEffectName("AncientDarkness");
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
        SpriteLightKitImageEffect lightManager = SpecialFunctions.CamController.GetComponent<SpriteLightKitImageEffect>();
        int prevIntensity = Mathf.RoundToInt(lightManager.intensity * 100f);
        Hero.AddBuff(new BuffClass("AncientDarkness", Time.time, ancientDarknessTime, prevIntensity, (lightManager.HDRRatio > .1f ? "changed" : "")));
        activeGameEffects.Add("AncientDarkness");
        bool changedHDRRatio = lightManager.HDRRatio > 0.1f;
        SpecialFunctions.CamController.StartLightTransition(Mathf.Clamp(lightManager.intensity - 1f, 0f, 2f), changedHDRRatio ? lightManager.HDRRatio - .1f : lightManager.HDRRatio);
        yield return new WaitForSeconds(ancientDarknessTime);
        SpecialFunctions.CamController.StartLightTransition(prevIntensity / 100f, changedHDRRatio ? lightManager.HDRRatio + .1f : lightManager.HDRRatio);
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
        SpriteLightKitImageEffect lightManager = SpecialFunctions.CamController.GetComponent<SpriteLightKitImageEffect>();
        SpecialFunctions.CamController.StartLightTransition(argument / 100f, id == "changed" ? lightManager.HDRRatio + .1f : lightManager.HDRRatio);
        hero.RemoveBuff("AncientDarkness");
        activeGameEffects.Remove("AncientDarkness");
    }

    /// <summary>
    /// Вызвать эффект "Тотемное животное"
    /// </summary>
    void StartTotemAnimal(CharacterController _char)
    {
        AddGameEffectName("TotemAnimal");
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
    void StartTribalRitual(CharacterController _char)
    {
        AddGameEffectName("TribalRitual");
        Hero.StartTribalRitual();
    }

    #endregion //gameEffects

    /// <summary>
    /// Функция завершения данного уровня
    /// </summary>
    /// <param name="nextLevelName">Название следующего уровня</param>
    /// <param name="withCompleteLevelScreen">Отображать ли экран конца уровня?</param> 
    /// <param name="checkpointNumber">Номер чекпоинта, с которого начнётся следующий уровень</param>
    public void CompleteLevel(string nextLevelName, bool withCompleteLevelScreen, int checkpointNumber=0)
    {
        RemoveHeroDeathLevelEnd();
        PlayerPrefs.SetInt("Checkpoint Number", checkpointNumber);
        PlayerPrefs.SetFloat("Hero Health", SpecialFunctions.Player.GetComponent<HeroController>().MaxHealth);
        SpecialFunctions.SetFade(true);
        StartCoroutine(CompleteLevelProcess(nextLevelName, withCompleteLevelScreen));
        SaveGame(checkpointNumber, true, nextLevelName);
    }

    /// <summary>
    /// Процесс успешного завершения данного уровня
    /// </summary>
    /// <param name="nextLevelName">Название следующего уровня</param>
    /// <param name="withCompleteLevelScreen">Отображать ли экран конца уровня</param>
    IEnumerator CompleteLevelProcess(string nextLevelName, bool withCompleteLevelScreen)
    {
        SpecialFunctions.StartStoryEvent(this, EndGameEvent, new StoryEventArgs());
        yield return new WaitForSeconds(nextLevelTime);
        if (nextLevelName != string.Empty)
        {
            if (nextLevelName == "cave_lvl2")
                GetAchievement("ACH_COMPLETE_1LEVEL");
            SpecialFunctions.levelEnd = true;
            GameStatistics statistics = SpecialFunctions.statistics;
            List<ItemCollection> itemCollections = statistics != null ? statistics.ItemCollections : null;
            string sceneName = SceneManager.GetActiveScene().name;
            ItemCollection _collection = itemCollections != null ? itemCollections.Find(x => sceneName.Contains(x.settingName)) : null;

            //Если были найдены все секреты уровня, то счётчик зачищенных уровней пополняется
            if (secretsFoundNumber >= secretsTotalNumber && secretsTotalNumber > 0)
            {
                GameData gData = Serializator.DeXml(datapath + profileNumber.ToString() + ".xml");
                if (gData != null)
                {
                    gData.gGData.AddLevelWithRevealedSecrets();
                    if (gData.gGData.maxSecretsFoundLevelCount >= maxLevelWithSecretsCount)
                        GetAchievement("HAWK_EYE");
                    Serializator.SaveXml(gData, datapath + profileNumber.ToString() + ".xml");
                }
            }


            if (withCompleteLevelScreen)
                levelCompleteScreen.SetLevelCompleteScreen(nextLevelName, secretsFoundNumber, secretsTotalNumber, _collection);
            else
                LoadingScreenScript.instance.LoadScene(nextLevelName);
        }
        else
            Application.Quit();
    }

    /// <summary>
    /// Учесть смену главного героя
    /// </summary>
    /// <param name="_hero">Новый главный герой</param>
    public void ConsiderHero(HeroController _hero)
    {
        hero = _hero;
        SetHeroDeathLevelEnd();
    }

    /// <summary>
    /// Сделать так, чтобы при смерти героя уровень заканчивался
    /// </summary>
    public void SetHeroDeathLevelEnd()
    {
        Hero.CharacterDeathEvent += HandleTargetDeathEvent;
    }

    /// <summary>
    /// Сделать так, чтобы при смерти героя уровень не заканчивался
    /// </summary>
    public void RemoveHeroDeathLevelEnd()
    {
        Hero.CharacterDeathEvent -= HandleTargetDeathEvent;
    }

    #region musicAndSounds

    /// <summary>
    /// Поменять громкость музыки
    /// </summary>
    public void ChangeMusicVolume(float _volume)
    {
        if (musicSource != null)
            musicSource.volume = _volume;
    }

    /// <summary>
    /// Обработка события "Поменялась громкость звуков"
    /// </summary>
    private void HandleSoundVolumeChange(object sender, SoundChangesEventArgs e)
    {
        if (soundSource != null)
            soundSource.volume = e.SoundVolume;
        if (ambientSource != null)
            ambientSource.volume = e.SoundVolume;
    }

    /// <summary>
    /// Начать проигрывать звуки окружающего мира, которые имеют заданное название
    /// </summary>
    public void ChangeAmbientSound(string clipName)
    {
        if (ambientSource == null)
            return;
        ambientSource.Stop();
        if (clipName!="")
        {
            AudioClip _clip = ambientClips.Find(x => x.name.Contains(clipName));
            if (_clip != null)
            {
                ambientSource.clip = _clip;
                ambientSource.Play();
            }
        }
    }

    /// <summary>
    /// Начать проигрывать музыкальную тему с заданным названием
    /// </summary>
    public void ChangeMusicTheme(string clipName)
    {
        if (musicSource == null)
            return;
        musicSource.Stop();
        if (clipName != "")
        {
            AudioClip _clip = musicClips.Find(x => x.name.Contains(clipName));
            if (_clip != null)
            {
                musicSource.clip = _clip;
                musicSource.Play();
            }
        }
    }

    /// <summary>
    /// Проиграть игровой звук с заданным названием
    /// </summary>
    /// <param name="soundName"></param>
    public void PlaySound(string soundName)
    {
        if (soundSource == null)
            return;
        if (soundName != "")
        {
            AudioClip _clip = soundClips.Find(x => x.name.Contains(soundName));
            if (_clip != null)
            {
                soundSource.clip = _clip;
                soundSource.Play();
            }
        }
    }

    #endregion //musicAndSounds

    #region eventHandlers

    /// <summary>
    /// Узнать о смерти некого персонажа и завершить игру
    /// </summary>
    /// <param name="sender">Что вызвало событие</param>
    /// <param name="e">Данные о событии</param>
    protected virtual void HandleTargetDeathEvent(object sender, StoryEventArgs e)
    {
        EndLevel();
    }

    /// <summary>
    /// Завершения процесса игры и рестарт
    /// </summary>
    public void EndLevel()
    {
        StartCoroutine(DeathProcess());
    }

    /// <summary>
    /// Процесс обработки смерти героя
    /// </summary>
    IEnumerator DeathProcess()
    {
        //Пополним счётчик смертей
        GameData gData = Serializator.DeXml(datapath + profileNumber.ToString() + ".xml");
        if (gData != null)
        {
            gData.gGData.AddDeath();
            gData.SetGameStatistics(Time.fixedTime, vulnerableEnemiesNames, gameEffectsNames);
            Serializator.SaveXml(gData, datapath + profileNumber.ToString() + ".xml");
        }
        else
        {
            gData = new GameData();
            gData.gGData = new GameGeneralData();
            gData.gGData.AddDeath();
            gData.SetGameStatistics(Time.fixedTime, vulnerableEnemiesNames, gameEffectsNames);
            Serializator.SaveXml(gData, datapath + profileNumber.ToString() + ".xml");
        }

        SpecialFunctions.SetFade(true);
        PlayerPrefs.SetFloat("Hero Health", hero.MaxHealth);
        yield return new WaitForSeconds(deathTime);
        LoadingScreenScript.instance.LoadScene(SceneManager.GetActiveScene().name);
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

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
