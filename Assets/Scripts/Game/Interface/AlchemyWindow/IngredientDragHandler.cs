using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Скрипт, реализующий перетаскивание изображения ингредиента (например, в окно смешивания)
/// </summary>
public class IngredientDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    #region fields

    public static ItemClass currentIngredient;//Какой ингредиент в данный момент перетаскивают в общий котёл
    protected ItemClass ingredient;//Ингредиент, который соответствует рассматриваемому объекту
    public ItemClass Ingredient { get { return ingredient; } set { ingredient = value; } }

    private Camera cam;

    #endregion //fields

    #region parametres

    protected Vector3 startPosition;//Где находилось изображение до начало перетаскивания

    #endregion //parametres

    /// <summary>
    /// Действия при начале захвата объекта
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        currentIngredient = ingredient;
        startPosition = transform.position;
        cam = SpecialFunctions.CamController.GetComponent<Camera>();
    }

    /// <summary>
    /// Функция, вызываемая при удержании объекта
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        //transform.position = eventData.position;
        Vector2 mousePos = Input.mousePosition;
        Vector3 mouseWorldPos = cam.ScreenPointToRay(mousePos).origin;
        mouseWorldPos.z = startPosition.z;
        transform.position = mouseWorldPos;
    }

    /// <summary>
    /// Окончание удержания объекта
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        currentIngredient = null;
        transform.position = startPosition;
    }

}
