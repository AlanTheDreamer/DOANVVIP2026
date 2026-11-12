using UnityEngine;

namespace EndlessRunner
{
    /// <summary>
    /// Toàn bộ MODEL của game nằm ở đây. Ô nào để trống thì game tự dùng khối placeholder.
    /// Tạo bằng: chuột phải trong Project > Create > Endless Runner > Theme,
    /// rồi kéo asset này vào ô "Theme" của RunnerGame.
    /// </summary>
    [CreateAssetMenu(fileName = "RunnerTheme", menuName = "Endless Runner/Theme")]
    public class RunnerTheme : ScriptableObject
    {
        [Header("Nhân vật (tự scale cao 1.8m, chân chạm đất, mặt hướng +Z)")]
        public ModelSlot player = new ModelSlot();

        [Header("Tên tham số Animator (bỏ qua nếu model không có Animator)")]
        [Tooltip("Float - tốc độ chạy")] public string speedParam = "Speed";
        [Tooltip("Bool - đang chạm đất")] public string groundedParam = "Grounded";
        [Tooltip("Bool - đang trượt")] public string slidingParam = "Sliding";
        [Tooltip("Trigger")] public string jumpTrigger = "Jump";
        [Tooltip("Trigger")] public string slideTrigger = "Slide";
        [Tooltip("Trigger")] public string deadTrigger = "Dead";

        [Header("Chướng ngại vật (mỗi loại có thể nhiều model, random)")]
        [Tooltip("Vật thấp - phải NHẢY qua. Tự fit cao 0.8m")]
        public ModelSlot[] jumpObstacles;
        [Tooltip("Vật treo cao - phải TRƯỢT dưới. Tự fit cao 3.4m (phần chặn từ 0.9m trở lên)")]
        public ModelSlot[] slideObstacles;
        [Tooltip("Tường chắn - phải ĐỔI LÀN. Tự fit cao 3m")]
        public ModelSlot[] blockObstacles;

        [Header("Vật phẩm")]
        [Tooltip("Coin - tự fit 0.8m, tự xoay")]
        public ModelSlot coin = new ModelSlot();

        [Header("Môi trường")]
        [Tooltip("Mảnh đường - tự kéo giãn cho vừa bề ngang và chiều dài chunk, mặt trên ở y=0")]
        public ModelSlot[] groundChunks;
        [Tooltip("Cây, nhà, đá... 2 bên đường - tự fit đáy 3x3m, xoay ngẫu nhiên")]
        public ModelSlot[] scenery;

        [Header("Bầu trời")]
        public Color skyColor = new Color(0.55f, 0.75f, 0.95f);
        public bool solidColorSky = true;
        public bool fog = true;

        [Header("Âm thanh — để trống ô nào thì bỏ qua ô đó")]
        [Tooltip("Nhạc nền, tự lặp lại")] public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.6f;
        [Tooltip("Âm lượng nhạc ở màn chờ = musicVolume x hệ số này")]
        [Range(0f, 1f)] public float menuVolumeScale = 0.5f;
        [Tooltip("Nhạc chơi nhanh dần theo tốc độ chạy")] public bool musicSpeedsUp = true;
        [Range(1f, 1.5f)] public float maxMusicPitch = 1.12f;
        [Space]
        public AudioClip jumpSfx;
        public AudioClip slideSfx;
        public AudioClip laneSfx;
        public AudioClip coinSfx;
        public AudioClip deathSfx;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        [Header("HUD")]
        [Tooltip("Font cho giao diện. Trống = font mặc định của Unity")]
        public Font hudFont;
        [Tooltip("Màu nhấn: tiêu đề, nút bấm, coin")]
        public Color hudAccent = new Color(1f, 0.78f, 0.24f);
        [Tooltip("Icon coin trên HUD. Trống = hình tròn vàng")]
        public Sprite hudCoinIcon;

        public ModelSlot[] GetObstacles(HazardType type)
        {
            switch (type)
            {
                case HazardType.Jump: return jumpObstacles;
                case HazardType.Slide: return slideObstacles;
                default: return blockObstacles;
            }
        }
    }

    [System.Serializable]
    public class ModelSlot
    {
        [Tooltip("Prefab / FBX model. Collider & Rigidbody trong model sẽ bị gỡ, hitbox do code quản lý.")]
        public GameObject prefab;
        [Tooltip("Xoay thêm nếu model quay sai hướng (vd: 0,180,0)")]
        public Vector3 rotationOffset;
        [Tooltip("Dịch thêm sau khi fit (mét)")]
        public Vector3 positionOffset;
        [Tooltip("Tick để giữ nguyên kích thước gốc, không auto-fit")]
        public bool keepOriginalSize;

        public bool IsSet => prefab != null;

        public static ModelSlot Pick(ModelSlot[] slots)
        {
            if (slots == null) return null;
            int valid = 0;
            foreach (var s in slots) if (s != null && s.IsSet) valid++;
            if (valid == 0) return null;
            int k = Random.Range(0, valid);
            foreach (var s in slots)
            {
                if (s == null || !s.IsSet) continue;
                if (k == 0) return s;
                k--;
            }
            return null;
        }
    }
}
