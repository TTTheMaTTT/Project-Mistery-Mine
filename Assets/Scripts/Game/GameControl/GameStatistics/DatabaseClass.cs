using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс базы данных. В нём хранится информация о всех важных игровых сущностях
/// </summary>
public class DatabaseClass : ScriptableObject
{

    #region fields

    public List<Quest> quests;//Список квестов, используемых в игре

    #endregion //fields

}
