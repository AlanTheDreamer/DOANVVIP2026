using EndlessRunner;
using UnityEditor;
using UnityEngine;

public static class EndlessRunnerMenu
{
    // Chuột phải trong Hierarchy > Endless Runner > Runner Game
    [MenuItem("GameObject/Endless Runner/Runner Game", false, 10)]
    static void CreateRunnerGame(MenuCommand command)
    {
        var go = new GameObject("RunnerGame", typeof(RunnerGame));
        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create Runner Game");
        Selection.activeObject = go;
    }
}
