using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

/// <summary>
/// Контроллер главного героя в обличии летучей мыши
/// </summary>
public class BatHeroController : HeroController
{

    #region fields

    protected HeroController originalController;

    #endregion //fields

    #region parametres

    [SerializeField]protected float acceleration = 20f;//Ускорение персонажа
    public float Acceleration { get { return acceleration; } set { acceleration = value; } }
    [SerializeField][HideInInspector]protected int id;

    #endregion //parametres

    protected override void Update()
    {
        if (immobile || employment <= 6)
            goto analyseSection;

        if (Input.GetButton("Horizontal") || Input.GetButton("Vertical"))
            Move(Input.GetAxis("Horizontal") > 0f ? OrientationEnum.right : OrientationEnum.left);
        else
            StopMoving();

        if (employment > 7)
        {
            if (Input.GetButtonDown("Attack"))
                if (interactor.ReadyForInteraction())
                    interactor.Interact();
            if (Input.GetButtonDown("ChangeInteraction"))
                interactor.ChangeInteraction();
        }

        analyseSection:

        Analyse();

        Animate(new AnimationEventArgs("fly"));
    }

    protected override void FixedUpdate()
    {
    }

    protected override void Initialize()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;

        if (transform.FindChild("HitBox") != null)
            hitBox = transform.FindChild("HitBox").GetComponent<HitBoxController>();

        if (hitBox != null)
        {
            hitBox.AttackerInfo = new AttackerClass(gameObject,AttackTypeEnum.melee);
            hitBox.SetEnemies(enemies);
        }

        orientation = (OrientationEnum)Mathf.RoundToInt(Mathf.Sign(transform.localScale.x));

        anim = GetComponentInChildren<CharacterVisual>();
        if (anim != null)
        {
            AnimationEventHandler += anim.AnimateIt;
        }

        employment = maxEmployment;
        speedCoof = 1f;
        FormDictionaries();

        buffs = new List<BuffClass>();

        indicators = transform.FindChild("Indicators");
        waterCheck = indicators.FindChild("WaterCheck");
        interactor = indicators.FindChild("Interactor").GetComponent<Interactor>();

        immobile = false;
        jumping = false;
        onLadder = false;

    }

    #region movement

    /// <summary>
    /// Перемещение
    /// </summary>
    protected override void Move(OrientationEnum _orientation)
    {
        Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical")).normalized * speed * speedCoof;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);

        if (orientation != _orientation)
        {
            Turn(_orientation);
        }
    }

    /// <summary>
    /// Остановить перемещение
    /// </summary>
    protected override void StopMoving()
    {
        Vector2 targetVelocity = Vector2.zero;
        rigid.velocity = Vector2.Lerp(rigid.velocity, targetVelocity, Time.fixedDeltaTime * acceleration);
    }

    /// <summary>
    /// Повернуться в выбранном направлении
    /// </summary>
    public override void Turn(OrientationEnum _orientation)
    {
        if (employment < 8)
            return;
        if (orientation != _orientation)
        {
            Vector3 vect = transform.localScale;
            orientation = _orientation;
            transform.localScale = new Vector3(-1 * vect.x, vect.y, vect.z);
        }
    }

    /// <summary>
    /// Повернуться
    /// </summary>
    protected override void Turn()
    {
        Vector3 vect = transform.localScale;
        orientation = (OrientationEnum)(-1 * (int)orientation);
        transform.localScale = new Vector3(-1 * vect.x, vect.y, vect.z);
    }

    #endregion //movement

    //Смерть персонажа
    protected override void Death()
    {
        base.Death();
        anim.gameObject.SetActive(false);
    }

    /// <summary>
    /// Анализ окружающей персонажа обстановки
    /// </summary>
    protected override void Analyse()
    {
        groundState = GroundStateEnum.inAir; 

        bool _underWater = Physics2D.OverlapCircle(waterCheck.position, groundRadius, LayerMask.GetMask("Water"));
        if (underWater != _underWater)
        {
            Underwater = _underWater;
        }
    }

    #region passiveAbilities

    /// <summary>
    /// Призвать тотемное животное
    /// </summary>
    public override void SummonTotemAnimal()
    {
    }

    /// <summary>
    /// Вызвать эффект "Племенной ритуал"
    /// </summary>
    public override void StartTribalRitual(bool shortTime)
    {
    }

    #endregion //passiveAbilities

    /// <summary>
    /// Функция, из-за которой управление переходит от обычного контроллера к контроллеру летучей мыши
    /// </summary>
    protected virtual void BecomeBat()
    {
        originalController = SpecialFunctions.player.GetComponent<HeroController>();
        transform.position = originalController.transform.position;
        SpecialFunctions.SwitchPlayer(this);
        originalController.gameObject.SetActive(false);
        Animate(new AnimationEventArgs("spawnEffect", "AppearDust", 0));
        //AddBuff(new BuffClass("BatTransformation", Time.fixedTime,batTime));
        //StartCoroutine("BecomeHumanProcess");
        GameObject.FindGameObjectWithTag("spirit").GetComponent<SpiritController>().Hero = transform;
    }

    /// <summary>
    /// Вернуться к гуманоидному оригинальному контроллеру
    /// </summary>
    protected virtual void BecomeHuman()
    {
        if (!originalController)
            return;
        originalController.gameObject.SetActive(true);
        SpecialFunctions.SwitchPlayer(originalController);
        originalController.transform.position = transform.position;
        Animate(new AnimationEventArgs("spawnEffect", "AppearDust", 0));
        gameObject.SetActive(false);
        GameObject.FindGameObjectWithTag("spirit").GetComponent<SpiritController>().Hero = originalController.transform;
    }

    /*protected virtual IEnumerator BecomeHumanProcess()
    {
        yield return new WaitForSeconds(batTime);
        BecomeHuman();
    }*/

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
        BecomeBat();
    }

    /// <summary>
    /// Настроить персонажа в соответствии с сохранёнными данными
    /// </summary>
    public override void SetData(InterObjData _intObjData)
    {
        BecomeBat();//Считаем, что если этот паук вообще загружается, то он и становится основным персонажем
    }

    /// <summary>
    /// Вернуть сохраняемые данные персонажа
    /// </summary>
    public override InterObjData GetData()
    {
        return new InterObjData(id, gameObject.name, transform.position);
    }

    #endregion //IHaveID

    #region storyActions

    /// <summary>
    /// Уничтожение объекта в результате скриптового события
    /// </summary>
    public void StoryBecomeHuman(StoryAction _action)
    {
        BecomeHuman();
    }

    /// <summary>
    /// Сформировать словари стори-действий
    /// </summary>
    protected override void FormDictionaries()
    {
        base.FormDictionaries();
        storyActionBase.Add("becomeHuman", StoryBecomeHuman);
    }

    #endregion //storyActions

    #region IHaveStory

    /// <summary>
    /// Вернуть список сюжетных действий, которые может воспроизводить персонаж
    /// </summary>
    /// <returns></returns>
    public override List<string> actionNames()
    {
        List<string> _actionNames = base.actionNames();
        _actionNames.Add("becomeHuman");
        return _actionNames;
    }

    /// <summary>
    /// Вернуть словарь первых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs1()
    {
        Dictionary<string, List<string>> _actionIDs1 = base.actionIDs1();
        _actionIDs1.Add("becomeHuman", new List<string>());
        return _actionIDs1;
    }

    /// <summary>
    /// Вернуть словарь вторых id-шников, связанных с конкретным сюжетным действием
    /// </summary>
    /// <returns></returns>
    public override Dictionary<string, List<string>> actionIDs2()
    {
        Dictionary<string, List<string>> _actionIDs2 = base.actionIDs2();
        _actionIDs2.Add("becomeHuman", new List<string>());
        return _actionIDs2;
    }

    #endregion //IHaveStory

}

/// <summary>
/// Редактор героя в обличии летучей мыши
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(BatHeroController))]
public class BatHeroControllerEditor : Editor
{

    SerializedObject serBatHero;

    public void OnEnable()
    {
        serBatHero = new SerializedObject((BatHeroController)target);
    }

    public override void OnInspectorGUI()
    {
        BatHeroController batHero = (BatHeroController)target;
        batHero.MaxHealth = EditorGUILayout.FloatField("Max Health", batHero.MaxHealth);
        batHero.Health = EditorGUILayout.FloatField("Health", batHero.Health);
        batHero.Balance = EditorGUILayout.IntField("Balance", batHero.Balance);
        batHero.Speed = EditorGUILayout.FloatField("Speed", batHero.Speed);
        batHero.Acceleration = EditorGUILayout.FloatField("Acceleration", batHero.Acceleration);

        serBatHero.ApplyModifiedProperties();
    }

}
#endif //UNITY_EDITOR