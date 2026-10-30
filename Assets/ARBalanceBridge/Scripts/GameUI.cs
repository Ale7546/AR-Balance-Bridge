using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace BalanceBridge
{
    public class GameUI : MonoBehaviour
    {
        public VirtualJoystick Joystick { get; private set; }
        public Text Status, Timer, Progress, Hint, CardTitle, CardBody;
        public Button Primary, Retry, Reposition;
        public Text PrimaryLabel;
        public GameObject Card;
        public Image ProgressFill;
        Font font;
        RectTransform safe;
        Rect lastSafe;
        static readonly Color Ink = new Color(0.035f, 0.075f, 0.10f, 0.94f);
        static readonly Color Mint = new Color(0.24f, 0.94f, 0.77f);

        public void Build(GameController game)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(600, 1100);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var events = new GameObject("Touch Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            safe = Rect("Safe area", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var header = Panel("Header", safe, new Vector2(0, 1), Vector2.one, new Vector2(26, -108), new Vector2(-26, -22), Ink);
            Label(header, "BALANCE / AR BRIDGE", 25, new Vector2(24, -14), new Vector2(420, 32), TextAnchor.MiddleLeft, Color.white);
            Status = Label(header, "SCAN A SURFACE", 14, new Vector2(24, -51), new Vector2(470, 22), TextAnchor.MiddleLeft, Mint);
            Timer = Label(header, "00.0s", 30, new Vector2(530, -17), new Vector2(160, 40), TextAnchor.MiddleCenter, Color.white);
            Progress = Label(header, "ONE BRIDGE. STAY BALANCED.", 12, new Vector2(490, -58), new Vector2(260, 18), TextAnchor.MiddleCenter, new Color(0.7f, 0.8f, 0.83f));
            Retry = ButtonAt(header, "RETRY", new Vector2(820, -20), new Vector2(118, 46), () => game.Retry());
            Reposition = ButtonAt(header, "NEW SPOT", new Vector2(950, -20), new Vector2(148, 46), () => game.Reposition());
            var track = Panel("Progress track", safe, new Vector2(0, 1), Vector2.one, new Vector2(26, -115), new Vector2(-26, -111), new Color(0.1f, 0.2f, 0.23f, 0.7f));
            ProgressFill = Panel("Progress", track, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero, Mint).GetComponent<Image>();

            var pad = Panel("Joystick", safe, Vector2.zero, Vector2.zero, new Vector2(44, 38), new Vector2(216, 210), new Color(0.035f, 0.075f, 0.10f, 0.72f));
            pad.GetComponent<Image>().sprite = Circle();
            pad.GetComponent<Image>().raycastTarget = true;
            Joystick = pad.gameObject.AddComponent<VirtualJoystick>();
            var knob = Panel("Handle", pad, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-32, -32), new Vector2(32, 32), Mint);
            knob.GetComponent<Image>().sprite = Circle();
            Joystick.Handle = knob;
            Hint = Label(safe, "Drag the joystick gently • push up to move forward", 17, new Vector2(255, -653), new Vector2(760, 26), TextAnchor.MiddleLeft, Color.white);
            var footer = Panel("Hint backdrop", safe, Vector2.zero, new Vector2(1, 0), new Vector2(244, 28), new Vector2(-35, 83), new Color(0.035f, 0.075f, 0.10f, 0.8f));
            Hint.transform.SetParent(footer, false);
            Stretch(Hint.rectTransform, new Vector2(20, 0), new Vector2(-16, 0));

            var card = Panel("Instruction card", safe, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-555, -145), new Vector2(-25, 123), Ink);
            Card = card.gameObject;
            Label(card, "A SMALL CROSSING. A STEADY HAND.", 13, new Vector2(30, -20), new Vector2(470, 20), TextAnchor.MiddleLeft, Mint);
            CardTitle = Label(card, "Find your starting point", 30, new Vector2(30, -55), new Vector2(470, 44), TextAnchor.MiddleLeft, Color.white);
            CardBody = Label(card, "Move your phone slowly over a well-lit floor or table. Aim at a clear, textured surface.", 18, new Vector2(30, -109), new Vector2(470, 64), TextAnchor.UpperLeft, new Color(0.78f, 0.85f, 0.87f));
            Primary = ButtonAt(card, "LOOKING FOR A SURFACE…", new Vector2(30, -194), new Vector2(470, 48), () => game.PrimaryAction(), true);
            PrimaryLabel = Primary.GetComponentInChildren<Text>();
            header.offsetMin = new Vector2(16, -170); header.offsetMax = new Vector2(-16, -16);
            var title = header.GetComponentInChildren<Text>(); title.fontSize = 21;
            Timer.rectTransform.anchoredPosition = new Vector2(470, -60);
            Timer.fontSize = 22;
            Progress.gameObject.SetActive(false);
            Status.fontSize = 12;
            Retry.GetComponent<RectTransform>().anchoredPosition = new Vector2(90, -118);
            Reposition.GetComponent<RectTransform>().anchoredPosition = new Vector2(250, -118);
            track.offsetMin = new Vector2(16, -178); track.offsetMax = new Vector2(-16, -174);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.offsetMin = new Vector2(-265, -134); card.offsetMax = new Vector2(265, 134);
            pad.anchorMin = pad.anchorMax = new Vector2(0.5f, 0);
            pad.offsetMin = new Vector2(-100, 38); pad.offsetMax = new Vector2(100, 238);
            footer.offsetMin = new Vector2(16, 250); footer.offsetMax = new Vector2(-16, 310);
            Hint.fontSize = 15;
            var recovery = ButtonAt(header, "RESET AR", new Vector2(354, -95), new Vector2(180, 46), () => game.RecoverCamera());
            var buttons = new[] { Retry, Reposition, recovery };
            for (int i = 0; i < buttons.Length; i++)
            {
                var rect = buttons[i].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(i / 3f, 1); rect.anchorMax = new Vector2((i + 1) / 3f, 1);
                rect.offsetMin = new Vector2(10, -142); rect.offsetMax = new Vector2(-10, -96);
            }
            Label(pad, "MOVE", 15, new Vector2(0, -80), new Vector2(200, 30), TextAnchor.MiddleCenter, Ink);
            RefreshSafeArea();
        }
        public void SetControls(bool visible, bool enabled)
        {
            Joystick.Clear();
            Joystick.gameObject.SetActive(visible);
            Joystick.enabled = enabled;
            var group = Joystick.GetComponent<CanvasGroup>();
            if (group == null) group = Joystick.gameObject.AddComponent<CanvasGroup>();
            group.alpha = enabled ? 1 : 0.4f;
            group.blocksRaycasts = enabled;
        }
        public void SetProgress(float value)
        {
            var rt = (RectTransform)ProgressFill.transform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(value), 1);
        }
        void Update() { if (Screen.safeArea != lastSafe) RefreshSafeArea(); }
        void RefreshSafeArea()
        {
            if (safe == null || Screen.width == 0 || Screen.height == 0) return;
            lastSafe = Screen.safeArea;
            safe.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
            safe.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
        }
        RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var rect = Rect(name, parent, min, max, offsetMin, offsetMax);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return rect;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            return rect;
        }
        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max; }
        Text Label(Transform parent, string value, int size, Vector2 topLeft, Vector2 dimensions, TextAnchor alignment, Color color)
        {
            var rect = Rect(value, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(topLeft.x, topLeft.y - dimensions.y), topLeft + new Vector2(dimensions.x, 0));
            var text = rect.gameObject.AddComponent<Text>();
            text.text = value; text.font = font; text.fontSize = size; text.alignment = alignment; text.color = color;
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }
        Button ButtonAt(Transform parent, string title, Vector2 topLeft, Vector2 size, UnityEngine.Events.UnityAction action, bool primary = false)
        {
            var rect = Panel(title, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(topLeft.x, topLeft.y - size.y), topLeft + new Vector2(size.x, 0), primary ? Mint : new Color(0.14f, 0.23f, 0.27f));
            rect.GetComponent<Image>().raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.onClick.AddListener(action);
            var label = Label(rect, title, 16, Vector2.zero, size, TextAnchor.MiddleCenter, primary ? Ink : Color.white);
            Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
            return button;
        }
        static Sprite Circle()
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                pixels[y * 128 + x] = new Color(1, 1, 1, Mathf.Clamp01(63f - Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f))));
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }
    }
}

