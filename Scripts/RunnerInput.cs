using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessRunner
{
    public enum RunnerAction { None, Left, Right, Jump, Slide, Tap, Pause, Mute }

    /// <summary>
    /// Bàn phím + vuốt (touch / chuột). Chạy được với cả Input System mới lẫn Input Manager cũ.
    /// </summary>
    public class RunnerInput
    {
        public float minSwipePixels = 50f;

        Vector2 pressStart;
        bool pressing;

        public RunnerAction Read()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) return RunnerAction.Left;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return RunnerAction.Right;
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) return RunnerAction.Jump;
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) return RunnerAction.Slide;
                if (kb.enterKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame) return RunnerAction.Tap;
                if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) return RunnerAction.Pause;
                if (kb.mKey.wasPressedThisFrame) return RunnerAction.Mute;
            }

            var ts = Touchscreen.current;
            if (ts != null)
            {
                var touch = ts.primaryTouch;
                var a = Pointer(touch.press.wasPressedThisFrame, touch.press.wasReleasedThisFrame, touch.position.ReadValue());
                if (a != RunnerAction.None) return a;
                if (touch.press.isPressed || touch.press.wasReleasedThisFrame) return RunnerAction.None;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                var m = Pointer(mouse.leftButton.wasPressedThisFrame, mouse.leftButton.wasReleasedThisFrame, mouse.position.ReadValue());
                if (m != RunnerAction.None) return m;
            }
#else
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return RunnerAction.Left;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return RunnerAction.Right;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) return RunnerAction.Jump;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return RunnerAction.Slide;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.R)) return RunnerAction.Tap;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) return RunnerAction.Pause;
            if (Input.GetKeyDown(KeyCode.M)) return RunnerAction.Mute;

            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                return Pointer(t.phase == TouchPhase.Began,
                    t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled, t.position);
            }
            else
            {
                var m = Pointer(Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0), Input.mousePosition);
                if (m != RunnerAction.None) return m;
            }
#endif
            return RunnerAction.None;
        }

        RunnerAction Pointer(bool down, bool up, Vector2 pos)
        {
            // Bấm trúng nút UI (pause, chơi lại...) thì không tính là thao tác chơi game
            if (down) { pressStart = pos; pressing = !OverUI(); return RunnerAction.None; }
            if (!up || !pressing) return RunnerAction.None;

            pressing = false;
            Vector2 d = pos - pressStart;
            if (d.magnitude < minSwipePixels) return RunnerAction.Tap;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x > 0 ? RunnerAction.Right : RunnerAction.Left;
            return d.y > 0 ? RunnerAction.Jump : RunnerAction.Slide;
        }

        static bool OverUI()
        {
            var es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }
    }
}
