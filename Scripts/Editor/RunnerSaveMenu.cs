using EndlessRunner;
using UnityEditor;
using UnityEngine;

/// <summary>Menu Tools > Endless Runner để xem / xoá dữ liệu lưu khi test.</summary>
public static class RunnerSaveMenu
{
    [MenuItem("Tools/Endless Runner/Show Save Data")]
    static void Show()
    {
        Debug.Log("[Runner] Save data (PlayerPrefs)" +
                  "\n  Best: " + RunnerSave.Best +
                  "\n  Total coins: " + RunnerSave.TotalCoins +
                  "\n  Muted: " + RunnerSave.Muted +
                  "\n  Company: " + PlayerSettings.companyName + " | Product: " + PlayerSettings.productName);
    }

    [MenuItem("Tools/Endless Runner/Reset Save Data")]
    static void Reset()
    {
        if (!EditorUtility.DisplayDialog("Reset Save Data",
                "Xoá điểm cao nhất, tổng coin và cài đặt âm thanh của Endless Runner?", "Xoá", "Huỷ"))
            return;
        RunnerSave.ResetAll();
        Debug.Log("[Runner] Đã xoá dữ liệu lưu.");
    }
}
