using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Компонент, который ответственен за осуществление события "Мир сна"
/// </summary>
public class DreamWorldScript : MonoBehaviour, IHaveStory
{

    #region delegates

    public delegate void storyActionDelegate(StoryAction _action);

    #endregion //delegates

    #region dictionaries

    protected Dictionary<string, storyActionDelegate> storyActionBase = new Dictionary<string, storyActionDelegate>(); //Словарь сюжетных действий

    #endregion //dictionaries

    #region consts

    private const float spawnRate = 1f;
    private const int wave1ToWave2MonsterCount = 2;
    private const int wave2ToWave3MonsterCount = 3;

    private const float spawnDistance = 3f;//Минимальное расстояние от героя до объекта, в котором может произойти спавн монстра

    #endregion //consts

    #region fields

    private HeroController hero;//Герой, который попадает в мир снов

    [SerializeField]
    private List<SpawnClass> wave1 = new List<SpawnClass>(),
                             wave2 = new List<SpawnClass>(),
                             wave3 = new List<SpawnClass>();//Описание волн монстров

    private GameObject batSpawn1, batSpawn2, spiderSpawn1, spiderSpawn2, minerSpawn1, minerSpawn2;//Места спавна монстров
    private List<AIController> spawnedMonsters = new List<AIController>();//Список созданных монстров
    private GameObject wallOnFire;//Окошко, через который виден мир снов (объект, обеспечивающий эффект дрожания и фиолетового оттенка)

    #endregion //fields

    #region parametres

    private int stage = 0;
    private bool activated = false;//Действует ли мир снов?
    private bool canSpawn = true;//Может ли объект создать нового монстра
    private bool noMonsters = false;//Монстров из текущей волны не осталось
    private int currentMaxMonsterCount;//Максимальное число монстров в первой, второй и третьей волнах
    private List<SpawnClass> currentSpawnList = new List<SpawnClass>();//Текущая волна монстров

    #endregion //parametres

    void Awake()
    {
        activated = false;
        spiderSpawn1 = transform.FindChild("SpiderSpawn1").gameObject;
        spiderSpawn2 = transform.FindChild("SpiderSpawn1").gameObject;
        batSpawn1 = transform.FindChild("BatSpawn1").gameObject;
        batSpawn2 = transform.FindChild("BatSpawn2").gameObject;
        minerSpawn1 = transform.FindChild("MinerSpawn1").gameObject;
        minerSpawn2 = transform.FindChild("MinerSpawn2").gameObject;
        currentSpawnList = new List<SpawnClass>();
        spawnedMonsters = new List<AIController>();
        wallOnFire = transform.FindChild("WallOnFire").gameObject;
        FormDictionaries();
    }

    void Update()
    {
        if (!activated)
            return;
        if (!noMonsters && spawnedMonsters.Count <= currentMaxMonsterCount && canSpawn)
            SpawnSomeMonster();
        else if (noMonsters && spawnedMonsters.Count <= (stage == 1 ? wave1ToWave2MonsterCount : wave2ToWave3MonsterCount))
            ChangeStage();
        foreach (AIController monster in spawnedMonsters)
            if (monster.Behavior==BehaviorEnum.calm)
                monster.StoryGoToThePoint(new StoryAction("goToThePoint", "hero", "",0));
    }

    /// <summary>
    /// Процесс, в течение которого монстры не будут создаваться
    /// </summary>
    /// <returns></returns>
    IEnumerator SpawnCooldownProcess()
    {
        canSpawn = false;
        yield return new WaitForSeconds(spawnRate);
        canSpawn = true;
    }

    /// <summary>
    ///Изменить фазу мира снов
    /// </summary>
    void ChangeStage()
    {
        stage++;
        List<SpawnClass> _spawnList;
        _spawnList = stage==1? wave1: stage==2? wave2 : wave3;
        currentSpawnList = new List<SpawnClass>();
        currentMaxMonsterCount = 0;
        foreach (SpawnClass _spawn in _spawnList)
        {
            currentSpawnList.Add(new SpawnClass(_spawn));
            currentMaxMonsterCount += _spawn.maxMonsterCount;
        }
        noMonsters = false;
    }

    /// <summary>
    /// Создать некоторого монстра из текущей волны
    /// </summary>
    void SpawnSomeMonster()
    {
        if (spawnedMonsters.Count >=currentMaxMonsterCount)
            return;
        List<int> monsterIndexes = new List<int>();
        for (int i = 0; i < currentSpawnList.Count; i++)
            if (currentSpawnList[i].monsterCount < currentSpawnList[i].maxMonsterCount)
                monsterIndexes.Add(i);
        if (monsterIndexes.Count == 0)
        {
            if (stage<3)
                noMonsters = true;
            return;
        }
        SpawnClass currentSpawn = currentSpawnList[Random.Range(0, currentSpawnList.Count)];
        GameObject _monster = currentSpawn.monster;
        GameObject newMonster = null;
        if (_monster.GetComponent<SpiderController>() != null)
        {
            GameObject spawnObject = Random.Range(0f, 1f) > 0.5f ? spiderSpawn1 : spiderSpawn2;
            newMonster = Instantiate(_monster, spawnObject.transform.position, Quaternion.identity) as GameObject;
            SpiderController spider = newMonster.GetComponent<SpiderController>();
            spider.Turn(spawnObject == spiderSpawn1 ? OrientationEnum.right : OrientationEnum.left);
            //spider.MoveOutAction(new StoryAction("moveOut", "", "", 0));
            spider.StoryGoToThePoint(new StoryAction("goToThePoint", "hero", "", 0));
            spider.CharacterDeathEvent += HandleDeathEvent;
            spawnedMonsters.Add(spider);
        }
        else
        {
            GameObject spawnObject = (_monster.GetComponent<BatController>() || _monster.GetComponent<GhostController>()) ?
                                     (Vector2.SqrMagnitude(hero.transform.position - batSpawn1.transform.position) >= spawnDistance * spawnDistance ? batSpawn1 : batSpawn2) :
                                     (Vector2.SqrMagnitude(hero.transform.position - minerSpawn1.transform.position) >= spawnDistance * spawnDistance ? minerSpawn1 : minerSpawn2);
            newMonster = Instantiate(_monster, spawnObject.transform.position, Quaternion.identity) as GameObject;
            AIController ai = newMonster.GetComponent<AIController>();
            ai.StoryGoToThePoint(new StoryAction("goToThePoint", "hero", "", 0));
            ai.CharacterDeathEvent += HandleDeathEvent;
            spawnedMonsters.Add(ai);
        }
        currentSpawn.monsterCount++;
        StartCoroutine(SpawnCooldownProcess());
     }

    #region eventHandlers

    /// <summary>
    /// Перехватить событие смерти одного из монстров
    /// </summary>
    void HandleDeathEvent(object sender, StoryEventArgs e)
    {
        if (!(sender is AIController))
            return;
        AIController ai = (AIController)sender;
        spawnedMonsters.Remove(ai);
        if (stage == 3)
        {
            SpawnClass spawn = currentSpawnList.Find(x => ai.gameObject.name.Contains(x.monster.name));
            if (spawn!=null? spawn.monsterCount>0:false)
                spawn.monsterCount--;
        }
    }

    #endregion //eventHandlers

    #region storyActions

    /// <summary>
    /// Активировать мир снов
    /// </summary>
    /// <param name="_action"></param>
    void Activate(StoryAction _action)
    {
        StartCoroutine(ActivationProcess());
    }

    /// <summary>
    /// Процесс активации мира снов
    /// </summary>
    /// <returns></returns>
    IEnumerator ActivationProcess()
    {
        GameObject.FindGameObjectWithTag("spirit").GetComponent<SpiritController>().Hero = null;//Дух ни за кем не следует в мире снов
        hero = SpecialFunctions.Player.GetComponent<HeroController>();
        hero.StartTribalRitual(false);
        SpriteLightKitImageEffect lightManager = SpecialFunctions.CamController.GetComponent<SpriteLightKitImageEffect>();
        SpriteLightKit lightKit = lightManager.GetComponentInChildren<SpriteLightKit>();
        lightKit.GetComponent<Camera>().backgroundColor = new Color(.65f, 0f, 1f, 1f);
        lightManager.intensity = .87f;
        yield return new WaitForSeconds(.5f);
        activated = true;
        stage = 0;
        ChangeStage();
        wallOnFire.transform.SetParent(SpecialFunctions.CamController.transform);
        wallOnFire.transform.localPosition = new Vector3(0f,0f,10f);
    }

    /// <summary>
    /// Прекратить работу мира снов
    /// </summary>
    void Deactivate(StoryAction _action)
    {
        stage = 0;
        GameObject.FindGameObjectWithTag("spirit").GetComponent<SpiritController>().Hero = hero.transform;//Дух снова следует за героем
        hero.StopTribalRitual();
        activated = false;
        SpecialFunctions.battleField.ResetBattlefield();
        foreach (AIController monster in spawnedMonsters)
        {
            Destroy(monster.gameObject);
        }
        spawnedMonsters.Clear();
        SpriteLightKitImageEffect lightManager = SpecialFunctions.CamController.GetComponent<SpriteLightKitImageEffect>();
        lightManager.intensity = .4f;
        SpriteLightKit lightKit = lightManager.GetComponentInChildren<SpriteLightKit>();
        lightKit.GetComponent<Camera>().backgroundColor = new Color(0f, 0f, 0f, 0f);
        wallOnFire.transform.SetParent(transform);
        wallOnFire.transform.localPosition = new Vector3(0f, 0f, 10f);
    }

    /// <summary>
    /// Сформировать словари сюжетных действий
    /// </summary>
    void FormDictionaries()
    {
        storyActionBase.Add("activate", Activate);
        storyActionBase.Add("deactivate", Deactivate);
    }

    #endregion //storyAction

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий
    /// </summary>
    /// <returns></returns>
    public virtual List<string> actionNames()
    {
        return new List<string>() { "activate", "deactivate" };
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs1()
    {
        return new Dictionary<string, List<string>>() { { "activate", new List<string>() { } },
                                                        { "deactivate", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public virtual Dictionary<string, List<string>> actionIDs2()
    {
        return new Dictionary<string, List<string>>() { { "activate", new List<string>() { } },
                                                        { "deactivate", new List<string>() { } } };
    }

    /// <summary>
    /// Вернуть словарь id-шников, связанных с конкретной функцией проверки условия сюжетного события
    /// </summary>
    public virtual Dictionary<string, List<string>> conditionIDs()
    {
        return new Dictionary<string, List<string>>() {};
    }

    /// <summary>
    /// Возвращает ссылку на сюжетное действие, соответствующее данному имени
    /// </summary>
    public StoryAction.StoryActionDelegate GetStoryAction(string s)
    {
        if (storyActionBase.ContainsKey(s))
            return storyActionBase[s].Invoke;
        else
            return null;
    }

    #endregion //IHaveStory

}

/// <summary>
/// Информация о спавне монстров
/// </summary>
[System.Serializable]
public class SpawnClass
{
    public GameObject monster;//Монстр, который спавнится
    public int monsterCount, maxMonsterCount;//максимальное кол-во монстров в спавне 

    public SpawnClass(GameObject _monster, int _monsterCount)
    {
        monster = _monster;
        maxMonsterCount = _monsterCount;
    }

    public SpawnClass(SpawnClass _spawn)
    {
        monster = _spawn.monster;
        maxMonsterCount = _spawn.maxMonsterCount;
        monsterCount = 0;
    }

}