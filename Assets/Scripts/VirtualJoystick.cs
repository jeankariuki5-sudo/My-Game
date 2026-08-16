using UnityEngine;
using UnityEngine.EventSystems;

// Attach to the joystick's background/base UI Image. Works with touch (mobile) and
// mouse (desktop/WebGL) automatically - Unity's EventSystem routes both through the
// same pointer-event interfaces, so no platform-specific code is needed here.
public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background; // the joystick's outer base - defaults to this object
    [SerializeField] private RectTransform handle;      // the draggable knob, child of background
    [SerializeField] private float handleRange = 100f;  // max distance the handle can travel from center, in UI pixels

    // Normalized direction, magnitude 0-1. (0,0) whenever the joystick isn't being held.
    public Vector2 InputDirection { get; private set; } = Vector2.zero;

    private void Awake()
    {
        if (background == null) background = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Register the touch/click position immediately on press, not just once dragging starts
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out position);

        Vector2 clamped = Vector2.ClampMagnitude(position, handleRange);
        InputDirection = clamped / handleRange;

        if (handle != null)
        {
            handle.anchoredPosition = clamped;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputDirection = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
