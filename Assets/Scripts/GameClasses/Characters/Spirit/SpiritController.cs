using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Контроллер, урправляющий духом - спутником игрок
/// </summary>
public class SpiritController : CharacterController
{

    #region consts

    protected const float findHiddenTime = 1f;//Время через которое дух скажет, что нашёл что-то необычное, когда это необычное окажется рядом с ним

    #endregion //consts

    #region fields

    protected Transform hero;//Персонаж, за которым следует дух
    public Transform Hero { get { return hero; } set { hero = value; } }

    protected List<FindHiddenRoutine> hiddenObjects = new List<FindHiddenRoutine>();
    [SerializeField]protected List<Dialog> findHiddenDialogs;//Диалоги, ознаменующий нахождение секретного предмета или места

    #endregion //fields

    #region parametres

    [SerializeField]
    protected float xOffset = 1f, yOffset = 0f;//Смещение

    #endregion //parametres

    protected override void Initialize()
    {
        base.Initialize();
        hero = SpecialFunctions.Player.transform;
        hiddenObjects = new List<FindHiddenRoutine>();
    }

    protected virtual void FixedUpdate()
    {
        if (hero == null)
            return;
        Vector2 pivot = hero.position + new Vector3(xOffset * Mathf.Sign(hero.lossyScale.x), yOffset);
        transform.position = Vector2.Lerp(transform.position, pivot, Time.fixedDeltaTime * speed);
    }

    protected void OnTriggerEnter2D(Collider2D other)
    {
        FindHiddenRoutine _routine = hiddenObjects.Find(x => x.hiddenObject == other.gameObject);
        if (_routine == null)
            StartFindHidden(other.gameObject);
    }

    protected void OnTriggerExit2D(Collider2D other)
    {
        FindHiddenRoutine _routine = hiddenObjects.Find(x => x.hiddenObject == other.gameObject);
        if (_routine != null)
            StopFindHidden(other.gameObject);
    }

    /// <summary>
    /// Воспроизвести вспышку света
    /// </summary>
    /// <param name="flashType"></param>
    protected void MakeFlash(string flashType)
    {
        Animate(new AnimationEventArgs("flash", flashType, 0));
    }

    /// <summary>
    /// Начать поиск секретного предмета неподалёку
    /// </summary>
    void StartFindHidden(GameObject hiddenObject)
    {
        FindHiddenRoutine _routine = new FindHiddenRoutine(hiddenObject, FindHiddenProcess(hiddenObject));
        hiddenObjects.Add(_routine);
        StartCoroutine(_routine.findProcess);
    }

    /// <summary>
    /// Прекратить поиск секретного предмета (например, из-за того, что предмета больше нет или он теперь далеко)
    /// </summary>
    void StopFindHidden(GameObject hiddenObject)
    {
        FindHiddenRoutine _routine = hiddenObjects.Find(x => x.hiddenObject == hiddenObject);
        if (_routine == null)
            return;
        StopCoroutine(_routine.findProcess);
        hiddenObjects.Remove(_routine);
    }

    /// <summary>
    /// Процесс поиска секретного предмета
    /// </summary>
    /// <param name="hiddenObject">Объект, который ожидается быть найденным</param>
    protected virtual IEnumerator FindHiddenProcess(GameObject hiddenObject)
    {
        yield return new WaitForSeconds(findHiddenTime);
        if (hiddenObject!=null)
            goto end;
        FindHiddenRoutine _routine = hiddenObjects.Find(x => x.hiddenObject == hiddenObject);
        if (_routine == null)
            goto end;
        if (hiddenObject.layer == LayerMask.NameToLayer("hidden")) 
            SpecialFunctions.dialogWindow.BeginDialog(findHiddenDialogs[Random.Range(0,findHiddenDialogs.Count)]);
        hiddenObjects.Remove(_routine);
    end:
        bool k = true;
    }
}

/// <summary>
/// Специальный класс, который является оболочкой над процессом поиска скрытого предмета
/// </summary>
public class FindHiddenRoutine
{
    public GameObject hiddenObject;
    public IEnumerator findProcess;

    public FindHiddenRoutine(GameObject _hiddenObject, IEnumerator _findProcess)
    {
        hiddenObject = _hiddenObject;
        findProcess = _findProcess;
    }
}