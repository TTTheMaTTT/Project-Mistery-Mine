using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт, управляющий камнем, который при падении наносит урон
/// </summary>
public class RockScript : MonoBehaviour, IInteractive
{

    #region fields

    private HitBoxController hitBox;
    private Rigidbody2D rigid;
    private SpriteRenderer sRenderer;

    [SerializeField]
    private List<string> enemies = new List<string>();
    public List<string> Enemies { set { enemies = value; } }

    [SerializeField]
    private HitParametres hitData;
    public HitParametres HitData { get { return hitData; } set { hitData = value; } }

    #endregion //fields

    #region parametres

    [SerializeField]private float fallTime = 4f;//Сколько времени "Работает камень"
    private bool fall = false;//падает ли камень
    [SerializeField]private float minDamageableFallSpeed = 5f;//Минимальная скорость камня, при которой он наносит урон
    [SerializeField]
    private Vector2 forceVector;//Какая сила инициирует движение камня
    private Color outlineColor = Color.yellow;//Цвет контура взаимодействия

    #endregion //parametres

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.isKinematic = true;
        hitBox = transform.FindChild("HitBox").GetComponent<HitBox>();
        hitBox.SetEnemies(enemies);
        sRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!fall)
            return;

        if (rigid.velocity.sqrMagnitude > minDamageableFallSpeed * minDamageableFallSpeed)
            hitBox.SetHitBox(hitData);
        else
            hitBox.ResetHitBox();
    }

    /// <summary>
    /// Процесс падения и "работы" камня
    /// </summary>
    IEnumerator FallProcess()
    {
        fall = true;
        yield return new WaitForSeconds(fallTime);
        fall = false;
        Destroy(hitBox.gameObject);
        Destroy(this);
    }

    #region IHaveID

    public void SetID(int _id)
    {
    }

    public int GetID()
    {
        return -1;
    }

    public InterObjData GetData()
    {
        return null;
    }

    public void SetData(InterObjData _intObjData)
    { }

    #endregion //IHaveID

    #region IInteractive

    /// <summary>
    /// Провести взаимодействие с камнем
    /// </summary>
    public virtual void Interact()
    {
        rigid.isKinematic = false;
        rigid.AddForce(forceVector);
        StartCoroutine("FallProcess");
    }

    /// <summary>
    /// Отрисовать контур, если происзодит взаимодействие (или убрать этот контур)
    /// </summary>
    public virtual void SetOutline(bool _outline)
    {
        if (sRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            sRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat("_Outline", _outline ? 1f : 0);
            mpb.SetColor("_OutlineColor", outlineColor);
            mpb.SetFloat("_OutlineWidth", .08f / ((Vector2)transform.lossyScale).magnitude);
            sRenderer.SetPropertyBlock(mpb);
        }
    }

    /// <summary>
    /// Можно ли провзаимодействовать с объектом в данный момент?
    /// </summary>
    public virtual bool IsInteractive()
    {
        return !fall;
    }

    #endregion //IInteractive

}
