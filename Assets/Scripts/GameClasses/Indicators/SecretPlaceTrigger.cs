using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Триггер, при входе в который выдаётся сообщение, что было найдено секретное место
/// </summary>
public class SecretPlaceTrigger : StoryTrigger, IHaveID
{

    #region fields

    [SerializeField]protected List<GameObject> ghostWalls = new List<GameObject>();//Призрачные стены, которые исчезают при открытии секретного места
    public List<GameObject> GhostWalls { get { return ghostWalls; } }

    #endregion //fields

    #region parametres

    [SerializeField][HideInInspector]public int id;

    #endregion //parametres

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("player"))
        {
            SpecialFunctions.StartStoryEvent(this, TriggerEvent, new StoryEventArgs());
            if (!triggered)
            {
                SpecialFunctions.FindSecretPlace(1.5f);
                RevealTruth();
                triggered = true;
                SpecialFunctions.statistics.ConsiderStatistics(this);
            }
        }
    }

    /// <summary>
    /// Раскрыть скрытое
    /// </summary>
    public void RevealTruth()
    {
        //Destroy(gameObject);
        foreach (GameObject ghostWall in ghostWalls)
            ghostWall.GetComponent<FadeScript>().Activate();
        if (gameObject.layer == LayerMask.NameToLayer("hidden"))
            gameObject.layer = LayerMask.NameToLayer("Default");
        Destroy(this);
    }

    #region IHaveID

    /// <summary>
    /// Вернуть id
    /// </summary>
    public int GetID()
    {
        return id;
    }

    /// <summary>
    /// Выставить id объекту
    /// </summary>
    public void SetID(int _id)
    {
        id = _id;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif //UNITY_EDITOR
    }

    /// <summary>
    /// Загрузить данные о секретном месте
    /// </summary>
    public virtual void SetData(InterObjData _intObjData)
    {
    }

    /// <summary>
    /// Запомнить секретное место с таким id
    /// </summary>
    public virtual InterObjData GetData()
    {
        return new InterObjData(id, gameObject.name, transform.position);
    }

    #endregion //IHaveID

}

#if UNITY_EDITOR
[CustomEditor(typeof(SecretPlaceTrigger))]
public class SecretPlaceTriggerEditor : Editor
{
    int newCount;

    public void OnEnable()
    {
        SecretPlaceTrigger sTrigger = (SecretPlaceTrigger)target;
        foreach (GameObject ghostWall in sTrigger.GhostWalls)
            if (ghostWall.GetComponent<FadeScript>() == null)
                ghostWall.AddComponent<FadeScript>();
    }

    public override void OnInspectorGUI()
    {
        SecretPlaceTrigger sTrigger = (SecretPlaceTrigger)target;
        base.OnInspectorGUI();
        if (sTrigger.GhostWalls.Count != newCount)
            foreach (GameObject ghostWall in sTrigger.GhostWalls)
                if (ghostWall.GetComponent<FadeScript>() == null)
                    ghostWall.AddComponent<FadeScript>();
        newCount = sTrigger.GhostWalls.Count;
    }

}
#endif //UNITY_EDITOR