using UnityEngine;

namespace EndlessRunner
{
    /// <summary>
    /// Gắn script này vào 1 GameObject trống trong scene rồi bấm Play là chạy.
    /// Mọi thứ (player, đường, camera, đèn, HUD) được dựng bằng code.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class RunnerGame : MonoBehaviour
    {
        public enum GameState { Ready, Running, Paused, Dead }

        public static RunnerGame Instance { get; private set; }

        [Header("Models — để trống = dùng khối placeholder")]
        public RunnerTheme theme;

        [Header("Tốc độ")]
        public float startSpeed = 12f;
        public float maxSpeed = 30f;
        [Tooltip("Tăng bao nhiêu m/s mỗi giây")] public float acceleration = 0.3f;

        [Header("Nhân vật")]
        public float laneWidth = 2.5f;
        public float laneChangeSpeed = 14f;
        public float jumpHeight = 2.2f;
        public float gravity = -35f;
        public float slideDuration = 0.75f;

        [Header("Sinh màn chơi")]
        public float chunkLength = 20f;
        public float spawnAhead = 120f;
        public float despawnBehind = 25f;
        public float safeStartDistance = 35f;
        public float minRowGap = 12f;
        public float maxRowGap = 20f;
        [Tooltip("Mỗi 1 m/s tốc độ cộng thêm bấy nhiêu mét khoảng cách giữa các hàng")]
        public float gapPerSpeed = 0.4f;
        [Range(0f, 1f)] public float doubleObstacleChance = 0.35f;
        [Range(0f, 1f)] public float fullRowChance = 0.1f;
        [Range(0f, 1f)] public float coinChance = 0.6f;

        [Header("Điểm & HUD")]
        [Tooltip("Số điểm mỗi coin cộng vào Score")]
        public int coinPoints = 5;
        [Tooltip("Bỏ tick nếu bạn tự làm UI riêng (đọc Score, Coins, State...)")]
        public bool useBuiltInHud = true;

        // ---- Dữ liệu cho UI đọc ----
        public GameState State { get; private set; }
        public PlayerRunner Player => player;
        public float Speed => speed;
        public float Distance => distance;
        public int Coins => coins;
        public int Best => best;
        public int TotalCoins { get; private set; }
        public bool IsNewBest { get; private set; }
        public int Score => Mathf.FloorToInt(distance) + coins * coinPoints;

        public RunnerAudio Sound { get; private set; }
        public event System.Action CoinCollected; // mỗi lần ăn coin
        public event System.Action<GameState> StateChanged;

        PlayerRunner player;
        LevelSpawner level;
        CameraFollow cam;
        readonly RunnerInput input = new RunnerInput();
        float speed, distance, stateTime;
        int coins, best;


        void Awake()
        {
            Instance = this;
            LogThemeReport();
            if (theme == null) theme = ScriptableObject.CreateInstance<RunnerTheme>();
            appliedTheme = theme;
            best = RunnerSave.Best;
            TotalCoins = RunnerSave.TotalCoins;
            BuildWorld();
            Sound = new GameObject("Audio").AddComponent<RunnerAudio>();
            Sound.Setup(this, theme);
            if (useBuiltInHud) new GameObject("HUD").AddComponent<RunnerHUD>().Setup(this, theme);
            ResetGame();
        }

        RunnerTheme appliedTheme;

        // In ra Console: Theme nào đang dùng, ô nào đã gán model
        void LogThemeReport()
        {
            if (FindObjectsByType<RunnerGame>(FindObjectsSortMode.None).Length > 1)
                Debug.LogWarning("[Runner] Scene có nhiều hơn 1 RunnerGame! Hãy xoá bớt, chỉ giữ 1.", this);

            if (theme == null)
            {
                Debug.LogWarning("[Runner] Ô Theme trên '" + name + "' đang TRỐNG → dùng khối placeholder.", this);
                return;
            }

            string Slot(ModelSlot s) => s != null && s.IsSet ? s.prefab.name : "—";
            string List(ModelSlot[] a)
            {
                if (a == null || a.Length == 0) return "—";
                int n = 0;
                foreach (var s in a) if (s != null && s.IsSet) n++;
                return n + "/" + a.Length + " có prefab";
            }

            Debug.Log("[Runner] Theme: " + theme.name +
                      "\n  player: " + Slot(theme.player) +
                      " | coin: " + Slot(theme.coin) +
                      "\n  jump: " + List(theme.jumpObstacles) +
                      " | slide: " + List(theme.slideObstacles) +
                      " | block: " + List(theme.blockObstacles) +
                      "\n  ground: " + List(theme.groundChunks) +
                      " | scenery: " + List(theme.scenery), theme);

            if (theme.player != null && theme.player.IsSet && theme.player.prefab.GetComponentInChildren<Renderer>(true) == null)
                Debug.LogWarning("[Runner] Prefab nhân vật '" + theme.player.prefab.name +
                                 "' không có MeshRenderer/SkinnedMeshRenderer nào → sẽ không thấy gì.", theme.player.prefab);
        }

        void OnValidate()
        {
            if (Application.isPlaying && appliedTheme != null && theme != appliedTheme)
                Debug.LogWarning("[Runner] Theme chỉ được áp dụng lúc bắt đầu Play. Hãy Stop rồi gán Theme, sau đó Play lại.", this);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Instance == this) Instance = null;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Pause(); // tự pause khi thoát app trên điện thoại
        }

        void BuildWorld()
        {
            if (FindAnyObjectByType<Light>() == null)
            {
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                sun.shadows = LightShadows.Soft;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                camera = go.GetComponent<Camera>();
            }
            camera.farClipPlane = spawnAhead + 60f;
            if (theme.solidColorSky)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = theme.skyColor;
            }

            RenderSettings.fog = theme.fog;
            if (theme.fog)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = theme.skyColor;
                RenderSettings.fogStartDistance = spawnAhead * 0.4f;
                RenderSettings.fogEndDistance = spawnAhead * 0.95f;
            }

            player = new GameObject("Player").AddComponent<PlayerRunner>();
            player.Setup(this, theme);
            player.Died += OnPlayerDied;

            level = new GameObject("Level").AddComponent<LevelSpawner>();
            level.Setup(this, theme, player.transform);

            cam = camera.GetComponent<CameraFollow>();
            if (cam == null) cam = camera.gameObject.AddComponent<CameraFollow>();
            cam.target = player.transform;
        }

        void ResetGame()
        {
            Time.timeScale = 1f;
            speed = startSpeed;
            distance = 0f;
            coins = 0;
            IsNewBest = false;
            player.ResetRunner();
            level.ResetLevel();
            cam.Snap();
            SetState(GameState.Ready);
        }

        void SetState(GameState s)
        {
            State = s;
            stateTime = Time.unscaledTime;
            StateChanged?.Invoke(s);
        }

        float TimeInState => Time.unscaledTime - stateTime;

        void Update()
        {
            var action = input.Read();
            if (action == RunnerAction.Mute) { Sound.ToggleMute(); return; }
            switch (State)
            {
                case GameState.Ready:
                    if (action != RunnerAction.None && action != RunnerAction.Pause && TimeInState > 0.25f)
                        SetState(GameState.Running);
                    break;

                case GameState.Running:
                    if (action == RunnerAction.Pause) { Pause(); break; }
                    speed = Mathf.Min(maxSpeed, speed + acceleration * Time.deltaTime);
                    player.ForwardSpeed = speed;
                    player.HandleAction(action);
                    distance = player.transform.position.z;
                    level.Tick(speed);
                    break;

                case GameState.Paused:
                    if (action == RunnerAction.Pause) Resume();
                    break;

                case GameState.Dead:
                    if (action != RunnerAction.None && action != RunnerAction.Pause && TimeInState > 0.6f) ResetGame();
                    break;
            }
        }

        // ---- Gọi từ nút UI ----
        public void Pause()
        {
            if (State != GameState.Running) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Running);
        }

        public void TogglePause()
        {
            if (State == GameState.Running) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void Restart()
        {
            if (State == GameState.Dead || State == GameState.Paused) ResetGame();
        }

        void OnPlayerDied()
        {
            if (Score > best)
            {
                IsNewBest = best > 0; // lần chơi đầu tiên không tính "NEW BEST"
                best = Score;
                RunnerSave.Best = best;
            }
            TotalCoins += coins;
            RunnerSave.TotalCoins = TotalCoins;
            SetState(GameState.Dead);
        }

        public void AddCoins(int amount)
        {
            coins += amount;
            CoinCollected?.Invoke();
        }
    }
}
