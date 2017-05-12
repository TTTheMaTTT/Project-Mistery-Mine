using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Данные, что дают общую информацию о всём ходе игры
/// </summary>
[XmlType("General Game Data")]
[XmlInclude(typeof(EquipmentInfo))]
[XmlInclude(typeof(CollectionInfo))]
[XmlInclude(typeof(GameProgressData))]
public class GameGeneralData
{

    [XmlElement("First Checkpoint")]
    public int firstCheckpointNumber = 0;//Если игрок решит переиграть уровень, именно с этого чекпоинта он начнёт игру

    [XmlElement("Max HP")]
    public float maxHP = 12f;//Максимальное здоровье персонажа

    [XmlElement("Equipment Info")]
    public EquipmentInfo eInfo;//Данные об инвентаре персонажа

    [XmlArray("Collections Info")]
    [XmlArrayItem("Information About Collection")]
    public List<CollectionInfo> cInfo = new List<CollectionInfo>();//Данные о собранных коллекциях

    #region informationForGameStatistics

    [XmlElement("Death Count")]
    public int deathCount = 0;//Количество смертей

    [XmlElement("Game Time")]
    public float gameTime = 0f;//Сколько времени уже идёт игра

    [XmlElement("Game Additional Time")]
    public float gameAddTime=0f;//Добавка к игровому времени. Нужна для учёта выходов из игры и вырубаний игровой машины. Игровой контроллер контролирует эту величину. 
                                //Если игра перезапустилась, то это время просто добавится к основному времени. В общем, эта переменная нужна для удобства работы

    [XmlElement("Vulnerable Enemies Names")]
    public List<string> vulnerableEnemies = new List<string>();//Список имён противников, которые были убиты противоположной им стихией

    [XmlElement("Secret Places Count")]
    public int maxSecretsFoundLevelCount = 0;//Сколько было пройдено уровней с максимальным числом найденных секретных мест

    [XmlElement("Game Effects Created")]
    public List<string> gameEffectsCreated = new List<string>();//Какие игровые эффекты были произведены игроком

    #endregion //informationForGameStatistics

    public GameProgressData progressInfo;//Данные о прогрессе игры

    public GameGeneralData()
    {
    }

    public GameGeneralData(int cNumb, HeroController player, List<ItemCollection> _collections, float _gameAddTime, List<string> _vEnemies, List<string> _gECreated)
    {
        firstCheckpointNumber = cNumb;
        eInfo = new EquipmentInfo(player.CurrentWeapon, player.Equipment);

        cInfo = new List<CollectionInfo>();
        for (int i = 0; i < _collections.Count; i++)
            cInfo.Add(new CollectionInfo(_collections[i]));
        GameStatistics gStats = SpecialFunctions.statistics;
        if (gStats!=null)
            progressInfo = new GameProgressData(gStats.gameHistoryProgress);
        maxHP = player.MaxHealth;
        gameAddTime = _gameAddTime;
        vulnerableEnemies = _vEnemies;
        gameEffectsCreated = _gECreated;
    }

    public void SetGameGeneralData(int cNumb, HeroController player, List<ItemCollection> _collections, float _gameAddTime, List<string> _vEnemies, List<string> _gECreated)
    {
        firstCheckpointNumber = cNumb;
        eInfo = new EquipmentInfo(player.CurrentWeapon, player.Equipment);

        cInfo = new List<CollectionInfo>();
        for (int i = 0; i < _collections.Count; i++)
            cInfo.Add(new CollectionInfo(_collections[i]));
        GameStatistics gStats = SpecialFunctions.statistics;
        if (gStats != null)
            progressInfo = new GameProgressData(gStats.gameHistoryProgress);
        maxHP = player.MaxHealth;
        gameAddTime = _gameAddTime;
        vulnerableEnemies = _vEnemies;
        gameEffectsCreated = _gECreated;
    }

    /// <summary>
    /// Увеличить счётчик смертей
    /// </summary>
    public void AddDeath()
    {
        deathCount++;
    }

    /// <summary>
    /// Добавить доп время к основному игровому времени
    /// </summary>
    public void RefreshGameTime()
    {
        gameTime += gameAddTime;
        gameAddTime = 0f;
    }

    /// <summary>
    /// Добавить счётчик уровней, в которых были найдены все секретные места
    /// </summary>
    public void AddLevelWithRevealedSecrets()
    {
        maxSecretsFoundLevelCount++;
    }

}
