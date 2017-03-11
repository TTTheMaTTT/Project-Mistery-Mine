using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт паука вора
/// </summary>
public class SpiderThiefScript : CharacterController
{

    #region fields

    private Animator animator;
    private Transform hero;
    private Transform trans;

    #endregion //fields

    #region parametres

    [SerializeField]
    private float distanceToHero;//На каком расстоянии от героя паук начинает убегать от него

    [SerializeField][HideInInspector]protected int id;

    private bool waiting = true;
    private bool activated = false;
    private int stage = 0;

    #endregion //parametres

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        hero = SpecialFunctions.Player.transform;
        trans = transform;
    }

    public void Update()
    {
        if (!activated || !waiting)
            return;
        if (Vector2.SqrMagnitude(trans.position - hero.position) < distanceToHero * distanceToHero)
            StartCoroutine("EscapeProcess");
    }

    /// <summary>
    /// Сформировать словари сюжетных действий
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        storyActionBase.Add("activate", StoryActivate);
    }

    /// <summary>
    /// Процесс побега
    /// </summary>
    /// <returns></returns>
    IEnumerator EscapeProcess()
    {
        waiting = false;
        stage++;
        AnimationClip[] animClips = animator.runtimeAnimatorController.animationClips;
        AnimationClip animClip = null;
        foreach (AnimationClip _animClip in animClips)
            if (_animClip.name == "Escape" + stage.ToString())
                animClip = _animClip;
        if (animClip != null)
        {
            animator.Play(animClip.name);
            yield return new WaitForSeconds(animClip.averageDuration);
        }
        waiting = true;
        if (stage == 4)
            Death();
    }

    protected override void Death()
    {
        base.Death();
        Destroy(gameObject);
    }

    #region storyActions

    /// <summary>
    /// Активирование персонажа
    /// </summary>
    public void StoryActivate(StoryAction _action)
    {
        activated = true;
    }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public override List<string> actionNames()
    {
        List<string> _actions = base.actionNames();
        _actions.Add("activate");
        return _actions;
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs1()
    {
        Dictionary<string, List<string>> _actionIDs1 = base.actionIDs1();
        _actionIDs1.Add( "activate",new List<string>() { });
        return _actionIDs1;
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs2()
    {
        Dictionary<string, List<string>> _actionIDs2 = base.actionIDs2();
        _actionIDs2.Add("activate", new List<string>() { });
        return _actionIDs2;
    }

    #endregion //IHaveStory

    #region IHaveID

    /// <summary>
    /// Вернуть id персонажа
    /// </summary>
    public override int GetID()
    {
        return id;
    }

    /// <summary>
    /// Установить заданное id
    /// </summary>
    public override void SetID(int _id)
    {
        id = _id;
    }

    /// <summary>
    /// Настроить персонажа в соответствии с сохранёнными данными
    /// </summary>
    public override void SetData(InterObjData _intObjData)
    {
        if (!(_intObjData is SpiderSpyData))
            return;
        SpiderSpyData spData = (SpiderSpyData)_intObjData;
        activated = spData.activated;
    }

    /// <summary>
    /// Вернуть сохраняемые данные персонажа
    /// </summary>
    public override InterObjData GetData()
    {
        return new SpiderSpyData(id,gameObject.name, activated);
    }

    #endregion //IHaveID

}
