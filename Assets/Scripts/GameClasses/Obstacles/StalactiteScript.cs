using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Скрипт, управляющий сталактитом
/// </summary>
public class StalactiteScript : MonoBehaviour, IMechanism
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

    #endregion //fields

    #region parametres

    [SerializeField]
    protected float gravityScale = 1f;//Насколько сильно гравитация воздействует на падение
    private bool fall = false;//падает ли сталактит

    #endregion //parametres

    void Awake()
    {
        hitBox = GetComponent<HitBoxController>();
        hitBox.Immobile = true;
        hitBox.SetEnemies(enemies);

        rigid = GetComponent<Rigidbody2D>();
        rigid.isKinematic = true;
        rigid.gravityScale = gravityScale;
        stalactiteBase = transform.FindChild("StalactiteBase");
        groundCheck = transform.FindChild("GroundCheck");
    }

    void Update()
    {
        if (!fall)
            return;
        if (Physics2D.OverlapCircle(groundCheck.position, grRadius, LayerMask.GetMask(grLayerName)))
            DestroyStalactite();
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

    public void ActivateMechanism()
    {
        if (fall)
            return;
        stalactiteBase.SetParent(null);

        rigid.isKinematic = false;
        hitBox.SetHitBox(hitData);
        fall = true;
    }

    #endregion //IHaveID

}
