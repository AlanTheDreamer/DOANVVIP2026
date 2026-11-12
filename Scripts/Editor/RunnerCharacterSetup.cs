using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Chuột phải vào file FBX nhân vật > Endless Runner > Setup Runner Character
/// (chọn kèm Animator Controller nếu muốn gắn luôn).
/// Tự làm: Rig Humanoid, đánh dấu normal map (_N), ẩn vũ khí/đồ thừa, tắt Root Motion,
/// tạo prefab "<tên>_Runner" sẵn sàng kéo vào Theme > Player.
/// </summary>
public static class RunnerCharacterSetup
{
    // Mesh có tên chứa các từ này sẽ bị ẩn trong prefab (vũ khí, đạo cụ, LOD phụ...)
    static readonly string[] HideKeywords =
        { "Gun", "Shotgun", "Machinegun", "Drink", "Weapon", "Sword", "Shield", "LOD1", "LOD2", "LOD3" };

    const string MenuPath = "Assets/Endless Runner/Setup Runner Character";

    [MenuItem(MenuPath, true)]
    static bool Validate() => GetSelectedModelPath() != null;

    [MenuItem(MenuPath, false, 2000)]
    static void Setup()
    {
        string path = GetSelectedModelPath();
        var controller = Selection.objects.OfType<RuntimeAnimatorController>().FirstOrDefault();
        string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');

        // 1. Rig Humanoid
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();

        var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            Debug.LogWarning("[Runner] Unity không tự map được xương Humanoid cho '" + path +
                             "'. Chọn FBX > Rig > Configure... để map tay. Prefab vẫn được tạo.");
        else
            Debug.Log("[Runner] Humanoid OK: " + path);

        // 2. Normal map: texture tên kết thúc bằng _N (cùng thư mục FBX và thư mục con)
        int normals = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string tp = AssetDatabase.GUIDToAssetPath(guid);
            string n = Path.GetFileNameWithoutExtension(tp);
            if (!n.EndsWith("_N") && !n.EndsWith("_Normal") && !n.EndsWith("_normal")) continue;
            var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
            if (ti == null || ti.textureType == TextureImporterType.NormalMap) continue;
            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
            normals++;
        }
        if (normals > 0) Debug.Log("[Runner] Đã đánh dấu " + normals + " normal map.");

        // 3. Tạo prefab
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);

        int hidden = 0;
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            if (!HideKeywords.Any(k => r.name.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)) continue;
            r.gameObject.SetActive(false);
            hidden++;
        }

        var animator = inst.GetComponent<Animator>();
        if (animator == null) animator = inst.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (avatar != null) animator.avatar = avatar;
        if (controller != null) animator.runtimeAnimatorController = controller;

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + model.name + "_Runner.prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
        Object.DestroyImmediate(inst);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[Runner] Đã tạo " + prefabPath + " (ẩn " + hidden + " mesh phụ" +
                  (controller != null ? ", gắn controller " + controller.name : ", CHƯA gắn Animator Controller") +
                  "). Kéo prefab này vào Theme > Player > Prefab.", prefab);
    }

    static string GetSelectedModelPath()
    {
        foreach (var o in Selection.objects)
        {
            string p = AssetDatabase.GetAssetPath(o);
            if (!string.IsNullOrEmpty(p) && AssetImporter.GetAtPath(p) is ModelImporter) return p;
        }
        return null;
    }
}
