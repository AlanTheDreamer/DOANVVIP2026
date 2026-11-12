using UnityEngine;

namespace EndlessRunner
{
    /// <summary>
    /// Toàn bộ dữ liệu lưu của game, dùng PlayerPrefs (lưu trên máy người chơi, không có server).
    /// Muốn thêm dữ liệu mới (skin đã mua, nhiệm vụ...) thì thêm ở đây.
    /// </summary>
    public static class RunnerSave
    {
        public const string BestKey = "EndlessRunner_Best";
        public const string TotalCoinsKey = "EndlessRunner_TotalCoins";
        public const string MutedKey = "EndlessRunner_Muted";

        /// <summary>Điểm cao nhất.</summary>
        public static int Best
        {
            get => PlayerPrefs.GetInt(BestKey, 0);
            set { PlayerPrefs.SetInt(BestKey, value); PlayerPrefs.Save(); }
        }

        /// <summary>Tổng coin tích luỹ qua mọi lượt chơi (dùng cho shop sau này).</summary>
        public static int TotalCoins
        {
            get => PlayerPrefs.GetInt(TotalCoinsKey, 0);
            set { PlayerPrefs.SetInt(TotalCoinsKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set { PlayerPrefs.SetInt(MutedKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Xoá toàn bộ dữ liệu của game (không đụng tới dữ liệu khác).</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(BestKey);
            PlayerPrefs.DeleteKey(TotalCoinsKey);
            PlayerPrefs.DeleteKey(MutedKey);
            PlayerPrefs.Save();
        }
    }
}
