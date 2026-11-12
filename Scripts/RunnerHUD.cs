using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace EndlessRunner
{
    /// <summary>
    /// HUD dựng hoàn toàn bằng code (UGUI): điểm, coin, kỷ lục, nút pause,
    /// màn hình bắt đầu, bảng tạm dừng, bảng Game Over.
    /// Font / màu nhấn / icon coin đổi trong RunnerTheme > HUD.
    /// </summary>
    public class RunnerHUD : MonoBehaviour
    {
        static readonly Color PillColor = new Color(0.06f, 0.08f, 0.14f, 0.55f);
        static readonly Color CardColor = new Color(0.09f, 0.11f, 0.18f, 0.96f);
        static readonly Color Danger = new Color(1f, 0.33f, 0.4f);
        static readonly Color Muted = new Color(1f, 1f, 1f, 0.6f);
        static readonly Color DarkText = new Color(0.1f, 0.12f, 0.18f);
        const float SpriteRadius = 28f;

        class Panel
        {
            public CanvasGroup group;
            public RectTransform card;
            public float t;
            public bool visible;
        }

        RunnerGame game;
        RunnerTheme theme;
        Font font;
        Color accent;
        string accentHex;
        Sprite rounded, circle;

        RectTransform canvasRoot, safeRoot;
        Rect lastSafeArea;

        // HUD khi đang chơi
        CanvasGroup hudGroup;
        Text scoreText, distanceText, coinText, bestText;
        RectTransform coinIcon;
        Button pauseButton;
        Text toastText;
        float toastTimer;
        const float ToastDuration = 1.2f;

        // Các bảng
        Panel startPanel, pausePanel, overPanel;
        Text startBest, startHint, overScore, overCoins, overDistance, overBest, overHint;
        GameObject newBestBadge;
        Text soundLabel;

        float coinPunch, shownScore;

        // Chữ "+5" bay lên khi ăn coin
        class Popup { public Text text; public float age = 99f; public float x; }
        readonly Popup[] popups = new Popup[8];
        int nextPopup;
        const float PopupLife = 0.8f;
        int speedStep;

        public void Setup(RunnerGame game, RunnerTheme theme)
        {
            this.game = game;
            this.theme = theme;
            font = theme.hudFont != null ? theme.hudFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            accent = theme.hudAccent;
            accentHex = ColorUtility.ToHtmlStringRGB(accent);
            rounded = MakeRoundedSprite(64, (int)SpriteRadius);
            circle = MakeRoundedSprite(128, 64);

            EnsureEventSystem();
            BuildCanvas();

            game.CoinCollected += OnCoin;
            game.StateChanged += OnStateChanged;
            OnStateChanged(game.State);
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.StateChanged -= OnStateChanged;
            game.CoinCollected -= OnCoin;
        }

        // =====================================================================
        //  Cập nhật mỗi frame
        // =====================================================================

        void Update()
        {
            if (game == null) return;
            float dt = Time.unscaledDeltaTime; // vẫn chạy khi pause (timeScale = 0)
            var state = game.State;
            ApplySafeArea();

            // HUD trên cùng
            bool hudOn = state == RunnerGame.GameState.Running || state == RunnerGame.GameState.Paused;
            hudGroup.alpha = Mathf.MoveTowards(hudGroup.alpha, hudOn ? 1f : 0f, dt * 5f);
            pauseButton.interactable = state == RunnerGame.GameState.Running;
            if (pausePanel.visible) soundLabel.text = game.Sound.Muted ? "SOUND: OFF" : "SOUND: ON";

            scoreText.text = game.Score.ToString();
            distanceText.text = Mathf.FloorToInt(game.Distance) + " m";
            coinText.text = game.Coins.ToString();
            bestText.text = "<size=26><color=#FFFFFF99>BEST</color></size>  " + Mathf.Max(game.Best, game.Score);

            // Coin "nảy" khi ăn
            coinPunch = Mathf.MoveTowards(coinPunch, 0f, dt * 5f);
            coinIcon.localScale = Vector3.one * (1f + 0.45f * coinPunch * coinPunch);
            UpdatePopups(dt);

            // Thông báo tăng tốc
            if (state == RunnerGame.GameState.Running)
            {
                int step = Mathf.FloorToInt(game.Speed / 5f);
                if (step > speedStep) { speedStep = step; ShowToast("SPEED UP!"); }
            }
            UpdateToast(dt);

            // Các bảng
            Animate(startPanel, dt);
            Animate(pausePanel, dt);
            Animate(overPanel, dt);

            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
            if (startPanel.visible) startHint.color = new Color(1f, 1f, 1f, pulse);
            if (overPanel.visible)
            {
                // Điểm đếm tăng dần
                float rate = Mathf.Max(60f, game.Score * 1.4f);
                shownScore = Mathf.MoveTowards(shownScore, game.Score, rate * dt);
                overScore.text = Mathf.RoundToInt(shownScore).ToString();
                overHint.color = new Color(1f, 1f, 1f, 0.35f + 0.25f * pulse);
            }
        }

        void OnCoin()
        {
            coinPunch = 1f;
            var p = popups[nextPopup];
            nextPopup = (nextPopup + 1) % popups.Length;
            p.age = 0f;
            p.x = Random.Range(-40f, 40f);
            p.text.text = "+" + game.coinPoints;
        }

        void UpdatePopups(float dt)
        {
            foreach (var p in popups)
            {
                p.age += dt;
                float k = p.age / PopupLife;
                var c = p.text.color;
                c.a = k >= 1f ? 0f : Mathf.Clamp01(k * 10f) * (1f - k * k);
                p.text.color = c;
                if (c.a <= 0f) continue;
                // bật lên từ bên phải điểm số rồi bay lên
                p.text.rectTransform.anchoredPosition = new Vector2(230f + p.x, -60f + k * 90f);
                p.text.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.5f, 1f, EaseOutBack(Mathf.Clamp01(k * 4f)));
            }
        }

        void OnStateChanged(RunnerGame.GameState s)
        {
            startPanel.visible = s == RunnerGame.GameState.Ready;
            pausePanel.visible = s == RunnerGame.GameState.Paused;
            overPanel.visible = s == RunnerGame.GameState.Dead;

            switch (s)
            {
                case RunnerGame.GameState.Ready:
                    startBest.text = (game.Best > 0 ? "BEST  " + game.Best + "      " : "") +
                                     "<color=#" + accentHex + ">●</color> " + game.TotalCoins;
                    speedStep = Mathf.FloorToInt(game.startSpeed / 5f);
                    toastTimer = 0f;
                    break;
                case RunnerGame.GameState.Dead:
                    shownScore = 0f;
                    overCoins.text = game.Coins.ToString();
                    overDistance.text = Mathf.FloorToInt(game.Distance) + " m";
                    overBest.text = game.Best.ToString();
                    newBestBadge.SetActive(game.IsNewBest);
                    break;
            }
        }

        static void Animate(Panel p, float dt)
        {
            p.t = Mathf.MoveTowards(p.t, p.visible ? 1f : 0f, dt * (p.visible ? 3.5f : 6f));
            p.group.alpha = p.visible ? Mathf.Clamp01(p.t * 2f) : p.t;
            bool on = p.visible && p.t > 0.5f;
            p.group.interactable = on;
            p.group.blocksRaycasts = on;
            if (p.card != null)
                p.card.localScale = Vector3.one * (p.visible ? Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(p.t)) : 1f);
        }

        void ShowToast(string text)
        {
            toastText.text = text;
            toastTimer = ToastDuration;
        }

        void UpdateToast(float dt)
        {
            toastTimer = Mathf.Max(0f, toastTimer - dt);
            float age = ToastDuration - toastTimer;
            var c = toastText.color;
            c.a = toastTimer <= 0f ? 0f : Mathf.Clamp01(age * 8f) * Mathf.Clamp01(toastTimer / 0.35f);
            toastText.color = c;
            toastText.rectTransform.anchoredPosition = new Vector2(0f, 220f + age * 50f);
            toastText.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(Mathf.Clamp01(age * 5f)));
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float a = x - 1f;
            return 1f + c3 * a * a * a + c1 * a * a;
        }

        void ApplySafeArea()
        {
            Rect sa = Screen.safeArea;
            if (sa == lastSafeArea || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafeArea = sa;
            safeRoot.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        // =====================================================================
        //  Dựng giao diện
        // =====================================================================

        void BuildCanvas()
        {
            var go = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Tham chiếu 1440x1440 + match 0.5: ổn cho cả màn ngang (PC) lẫn dọc (điện thoại)
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 1440f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot = (RectTransform)go.transform;
            safeRoot = Stretch(Node("SafeArea", canvasRoot));

            BuildTopBar();
            BuildToast();
            startPanel = BuildStartPanel();
            pausePanel = BuildPausePanel();
            overPanel = BuildGameOverPanel();
            ApplySafeArea();
        }

        void BuildTopBar()
        {
            var bar = Stretch(Node("TopBar", safeRoot));
            hudGroup = bar.gameObject.AddComponent<CanvasGroup>();
            hudGroup.alpha = 0f;

            // Coin (trái)
            var coinPill = Box("Coins", bar, PillColor, 42f).rectTransform;
            Place(coinPill, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(230f, 84f));
            coinIcon = CoinIcon(coinPill, 58f);
            Place(coinIcon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(44f, 0f), new Vector2(58f, 58f));
            coinText = Label("Value", coinPill, "0", 46, Color.white, TextAnchor.MiddleLeft);
            Place(coinText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(130f, 60f));

            // Điểm (giữa)
            scoreText = Label("Score", bar, "0", 110, Color.white, TextAnchor.MiddleCenter, true, true);
            Place(scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(600f, 130f));
            distanceText = Label("Distance", bar, "0 m", 34, Muted, TextAnchor.MiddleCenter, false, true);
            Place(distanceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(400f, 48f));

            // Pause + Best (phải)
            pauseButton = MakeButton("Pause", bar, "II", 38, PillColor, Color.white, 42f, game.TogglePause);
            Place((RectTransform)pauseButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(84f, 84f));

            var bestPill = Box("Best", bar, PillColor, 42f).rectTransform;
            Place(bestPill, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-136f, -36f), new Vector2(250f, 84f));
            bestText = Label("Value", bestPill, "", 38, accent, TextAnchor.MiddleCenter);
            Stretch(bestText.rectTransform);

            for (int i = 0; i < popups.Length; i++)
            {
                var t = Label("Popup", bar, "", 56, accent, TextAnchor.MiddleCenter, true, true);
                Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 70f));
                t.color = new Color(accent.r, accent.g, accent.b, 0f);
                popups[i] = new Popup { text = t };
            }
        }

        void BuildToast()
        {
            toastText = Label("Toast", safeRoot, "", 72, accent, TextAnchor.MiddleCenter, true, true);
            Place(toastText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(900f, 100f));
            toastText.color = new Color(accent.r, accent.g, accent.b, 0f);
        }

        Panel BuildStartPanel()
        {
            var p = NewPanel("StartPanel", 0.35f);
            var c = Place(Node("Content", p.group.transform), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1000f));
            p.card = c;

            At(Label("Title1", c, "ENDLESS", 150, Color.white, TextAnchor.MiddleCenter, true, true), 0f, 330f, 900f, 170f);
            At(Label("Title2", c, "RUNNER", 150, accent, TextAnchor.MiddleCenter, true, true), 0f, 180f, 900f, 170f);

            var box = Box("Controls", c, PillColor, 36f).rectTransform;
            At(box, 0f, -60f, 640f, 280f);
            Row(box, K("A / D") + "   change lane", 90f);
            Row(box, K("W / Space") + "   jump", 30f);
            Row(box, K("S") + "   slide", -30f);
            At(Label("Mobile", box, "Swipe on mobile  •  Esc pause  •  M mute", 28, Muted, TextAnchor.MiddleCenter, false), 0f, -95f, 600f, 40f);

            startBest = Label("Best", c, "", 38, Muted, TextAnchor.MiddleCenter, true);
            At(startBest, 0f, -255f, 600f, 50f);
            startHint = Label("Hint", c, "TAP OR PRESS ANY KEY", 46, Color.white, TextAnchor.MiddleCenter, true, true);
            At(startHint, 0f, -370f, 900f, 60f);
            return p;
        }

        Panel BuildPausePanel()
        {
            var p = NewPanel("PausePanel", 0.55f);
            var card = Box("Card", p.group.transform, CardColor, 44f).rectTransform;
            At(card, 0f, 0f, 560f, 640f);
            p.card = card;

            At(Label("Title", card, "PAUSED", 84, Color.white, TextAnchor.MiddleCenter, true, true), 0f, 220f, 500f, 100f);
            var resume = MakeButton("Resume", card, "RESUME", 46, accent, DarkText, 55f, game.Resume);
            At((RectTransform)resume.transform, 0f, 80f, 400f, 110f);
            var restart = MakeButton("Restart", card, "RESTART", 40, new Color(1f, 1f, 1f, 0.1f), Color.white, 50f, game.Restart);
            At((RectTransform)restart.transform, 0f, -45f, 400f, 100f);
            var sound = MakeButton("Sound", card, "", 40, new Color(1f, 1f, 1f, 0.1f), Color.white, 50f, game.Sound.ToggleMute);
            At((RectTransform)sound.transform, 0f, -160f, 400f, 100f);
            soundLabel = sound.GetComponentInChildren<Text>();
            At(Label("Hint", card, "Esc / P to resume  •  M to mute", 28, Muted, TextAnchor.MiddleCenter, false), 0f, -255f, 520f, 40f);
            return p;
        }

        Panel BuildGameOverPanel()
        {
            var p = NewPanel("GameOverPanel", 0.6f);
            var card = Box("Card", p.group.transform, CardColor, 48f).rectTransform;
            At(card, 0f, 0f, 640f, 780f);
            p.card = card;

            At(Label("Title", card, "GAME OVER", 76, Danger, TextAnchor.MiddleCenter, true, true), 0f, 310f, 600f, 90f);
            At(Label("ScoreLabel", card, "SCORE", 30, Muted, TextAnchor.MiddleCenter, true), 0f, 225f, 400f, 40f);
            overScore = Label("Score", card, "0", 130, Color.white, TextAnchor.MiddleCenter, true, true);
            At(overScore, 0f, 135f, 600f, 140f);

            var badge = Box("NewBest", card, accent, 30f).rectTransform;
            At(badge, 0f, 35f, 280f, 60f);
            Stretch(Label("Text", badge, "NEW BEST!", 34, DarkText, TextAnchor.MiddleCenter).rectTransform);
            newBestBadge = badge.gameObject;

            At(Box("Divider", card, new Color(1f, 1f, 1f, 0.08f), 0f).rectTransform, 0f, -25f, 540f, 2f);
            overCoins = Stat(card, -190f, "COINS");
            overDistance = Stat(card, 0f, "DISTANCE");
            overBest = Stat(card, 190f, "BEST");

            var retry = MakeButton("Retry", card, "PLAY AGAIN", 50, accent, DarkText, 60f, game.Restart);
            At((RectTransform)retry.transform, 0f, -250f, 460f, 120f);
            overHint = Label("Hint", card, "or press any key", 28, Muted, TextAnchor.MiddleCenter, false);
            At(overHint, 0f, -345f, 500f, 40f);
            return p;
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        Panel NewPanel(string name, float dim)
        {
            var root = Stretch(Node(name, canvasRoot));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            Stretch(Box("Dim", root, new Color(0.02f, 0.03f, 0.08f, dim), 0f).rectTransform);
            return new Panel { group = group };
        }

        Text Stat(RectTransform parent, float x, string label)
        {
            var value = Label(label + "Value", parent, "0", 52, Color.white, TextAnchor.MiddleCenter);
            At(value, x, -90f, 190f, 64f);
            At(Label(label + "Label", parent, label, 24, Muted, TextAnchor.MiddleCenter), x, -140f, 190f, 34f);
            return value;
        }

        void Row(RectTransform parent, string text, float y)
        {
            At(Label("Row", parent, text, 38, Color.white, TextAnchor.MiddleCenter, false), 0f, y, 600f, 50f);
        }

        string K(string key) => "<b><color=#" + accentHex + ">" + key + "</color></b>";

        RectTransform CoinIcon(Transform parent, float size)
        {
            var rt = Node("CoinIcon", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            if (theme.hudCoinIcon != null)
            {
                img.sprite = theme.hudCoinIcon;
                img.preserveAspect = true;
                return rt;
            }
            img.sprite = circle;
            img.color = accent;
            var inner = Node("Inner", rt);
            var innerImg = inner.gameObject.AddComponent<Image>();
            innerImg.sprite = circle;
            innerImg.color = Color.Lerp(accent, Color.white, 0.35f);
            innerImg.raycastTarget = false;
            Place(inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 0.6f);
            return rt;
        }

        Button MakeButton(string name, Transform parent, string label, int fontSize, Color bg, Color fg, float radius, UnityAction onClick)
        {
            var img = Box(name, parent, bg, radius);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() =>
            {
                onClick();
                // Bỏ chọn nút, để phím Space/Enter không "bấm lại" nút này
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            });
            Stretch(Label("Label", img.transform, label, fontSize, fg, TextAnchor.MiddleCenter).rectTransform);
            return b;
        }

        Image Box(string name, Transform parent, Color color, float radius)
        {
            var img = Node(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (radius > 0f)
            {
                img.sprite = rounded;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = SpriteRadius / radius;
            }
            return img;
        }

        Text Label(string name, Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, bool bold = true, bool shadow = false)
        {
            var t = Node(name, parent).gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            if (shadow)
            {
                var s = t.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.45f);
                s.effectDistance = new Vector2(0f, -4f);
            }
            return t;
        }

        static RectTransform Node(string name, Transform parent)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Đặt theo toạ độ tính từ tâm của parent.</summary>
        static void At(Graphic g, float x, float y, float w, float h) => At(g.rectTransform, x, y, w, h);

        static void At(RectTransform rt, float x, float y, float w, float h)
        {
            var mid = new Vector2(0.5f, 0.5f);
            Place(rt, mid, mid, new Vector2(x, y), new Vector2(w, h));
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>Tạo sprite bo góc (9-slice) bằng code, không cần file ảnh.</summary>
        static Sprite MakeRoundedSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "HUD_Rounded"
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float cx = Mathf.Clamp(fx, radius, size - radius);
                float cy = Mathf.Clamp(fy, radius, size - radius);
                float d = Vector2.Distance(new Vector2(fx, fy), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
