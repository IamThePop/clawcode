using UnityEngine;
using UnityEngine.EventSystems; 
public class CursorHoverChanger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cursor Settings")]
    [Tooltip("Drag your custom cursor image here")]
    public Texture2D hoverCursorTexture;

    [Tooltip("The exact pixel on the image that clicks (0,0 is top-left)")]
    public Vector2 hotSpot = Vector2.zero;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverCursorTexture != null)
        {
            Cursor.SetCursor(hoverCursorTexture, hotSpot, CursorMode.Auto);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}