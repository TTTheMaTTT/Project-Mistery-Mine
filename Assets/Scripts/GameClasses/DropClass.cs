using UnityEngine;
using System.Collections;

/// <summary>
/// Класс, характеризующий подбираемые предметы
/// </summary>
public class DropClass : MonoBehaviour, IInteractive
{

    #region consts

    private const float groundRadius = .001f;

    #endregion //consts

    #region fields

    public ItemClass item;

    private Rigidbody2D rigid;
    private Transform groundCheck;


    #endregion //fields

    void Awake()
    {
        rigid = GetComponentInChildren<Rigidbody2D>();
        groundCheck = transform.FindChild("GroundCheck");
    }

    void FixedUpdate()
    {
        if (!rigid.isKinematic)
        {
            if (Physics2D.OverlapCircle(groundCheck.position, groundRadius, LayerMask.GetMask("ground")))
            {
                rigid.isKinematic = true;
            }
        }
    }

    /// <summary>
    /// Провзаимодействовать с дропом
    /// </summary>
    public void Interact()
    {
        SpecialFunctions.player.GetComponent<HeroController>().SetItem(item);
        Destroy(gameObject);
    }
}
