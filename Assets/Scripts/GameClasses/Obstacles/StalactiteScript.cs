using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт, управляющий сталактитом
/// </summary>
public class StalactiteScript : MonoBehaviour, IMechanism, IInteractive
{

    #region consts

    private const float grRadius = .005f;
    private string grLayerName = "ground";

    #endregion //consts

    #region fields

    private HitBoxController hitBox;
    [SerializeField]
    private List<string> enemies = new List<string>();
    public List<string> Enemies { set { enemies = value; } }

    [SerializeField]
    private HitParametres hitData;
    public HitParametres HitData { get { return hitData; } set { hitData = value; } }

    private Rigidbody2D rigid;
    private Transform stalactiteBase;
    private Transform groundCheck;

    private SpriteRenderer sRenderer;

    #endregion //fields

    #region parametres

    [SerializeField]
    protected float gravityScale = 1f;//Насколько сильно гравитация воздействует на падение
    [SerializeField]
    protected bool allyHitBox = false;//Атакует союзников или врагов?
    private bool fall = false;//падает ли сталактит

    protected Color outlineColor = Color.green;

    #endregion //parametres

    void Awake()
    {
        hitBox = GetComponent<HitBoxController>();
        hitBox.Immobile = true;
        hitBox.SetEnemies(enemies);
        hitBox.allyHitBox = allyHitBox;

        rigid = GetComponent<Rigidbody2D>();
        rigid.isKinematic = true;
        rigid.gravityScale = gravityScale;
        stalactiteBase = transform.FindChild("StalactiteBase");
        groundCheck = transform.FindChild("GroundCheck");

        sRenderer = GetComponent<SpriteRenderer>();

    }

    void Update()
    {
        if (!fall)
            return;
        if (Physics2D.OverlapCircle(groundCheck.position, grRadius, LayerMask.GetMask(grLayerName)))
            DestroyStalactite();
    }

    void ActivateStalactite()
    {

        if (fall)
            return;
        stalactiteBase.SetParent(null);

        rigid.isKinematic = false;
        hitBox.SetHitBox(hitData);
        fall = true;
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col)
            Destroy(col);
    }

    /// <summary>
    /// Прекращение работы сталактита
    /// </summary>
    public void DestroyStalactite()
    {
        hitBox.ResetHitBox();
        rigid.velocity = Vector2.zero;
        fall = false;
        Destroy(hitBox);
        Destroy(GetComponent<HitBoxCollider>());
        Destroy(rigid);
        Destroy(groundCheck.gameObject);
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

    #region IMechanism

    public void ActivateMechanism()
    {
        ActivateStalactite();
    }

    #endregion //IMechanism

    #region IInteractive

    /// <summary>
    /// Провести взаимодействие со сталактитом
    /// </summary>
    public virtual void Interact()
    {
        ActivateStalactite();
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
