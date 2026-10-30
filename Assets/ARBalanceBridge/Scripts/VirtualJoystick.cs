using UnityEngine;
using UnityEngine.EventSystems;

namespace BalanceBridge
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Handle;
        public Vector2 Value { get; private set; }
        int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData data)
        {
            if (pointer != int.MinValue) return;
            pointer = data.pointerId;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (pointer != data.pointerId) return;
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out var local);
            Value = Vector2.ClampMagnitude(local / (rect.rect.width * 0.34f), 1);
            Handle.anchoredPosition = Value * (rect.rect.width * 0.34f);
        }
        public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointer) Clear(); }
        public void Clear() { pointer = int.MinValue; Value = Vector2.zero; if (Handle != null) Handle.anchoredPosition = Vector2.zero; }
        void OnDisable() => Clear();
    }
}
