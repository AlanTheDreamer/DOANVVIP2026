using UnityEngine;

namespace EndlessRunner
{
    /// <summary>Gắn model bất kỳ vào một điểm neo và tự scale cho vừa hitbox.</summary>
    public static class ModelUtil
    {
        public enum Align { Bottom, Center, Top }

        /// <param name="fitSize">Kích thước mục tiêu (mét). Trục = 0 nghĩa là bỏ qua trục đó.</param>
        /// <param name="stretchXZ">true = kéo giãn X/Z độc lập (dùng cho mặt đường).</param>
        public static GameObject Attach(ModelSlot slot, Transform anchor, Vector3 fitSize, Align align, bool stretchXZ = false)
        {
            var go = Object.Instantiate(slot.prefab, anchor);
            var t = go.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.Euler(slot.rotationOffset);

            StripPhysics(go);
            foreach (var a in go.GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;

            if (!slot.keepOriginalSize) Fit(t, anchor, fitSize, align, stretchXZ);
            t.localPosition += slot.positionOffset;
            return go;
        }

        static void StripPhysics(GameObject go)
        {
            foreach (var j in go.GetComponentsInChildren<Joint>(true)) Object.Destroy(j);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Object.Destroy(c); }
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) { rb.isKinematic = true; Object.Destroy(rb); }
        }

        static void Fit(Transform model, Transform anchor, Vector3 target, Align align, bool stretchXZ)
        {
            if (!TryGetBounds(model, out var b)) return;
            const float eps = 0.0001f;
            Vector3 s = b.size;

            if (stretchXZ)
            {
                float sx = target.x > 0 && s.x > eps ? target.x / s.x : 1f;
                float sz = target.z > 0 && s.z > eps ? target.z / s.z : 1f;
                model.localScale = Vector3.Scale(model.localScale, new Vector3(sx, 1f, sz));
            }
            else
            {
                float k = float.MaxValue;
                if (target.x > 0 && s.x > eps) k = Mathf.Min(k, target.x / s.x);
                if (target.y > 0 && s.y > eps) k = Mathf.Min(k, target.y / s.y);
                if (target.z > 0 && s.z > eps) k = Mathf.Min(k, target.z / s.z);
                if (k < float.MaxValue) model.localScale *= k;
            }

            if (!TryGetBounds(model, out b)) return;
            Vector3 point;
            switch (align)
            {
                case Align.Bottom: point = new Vector3(b.center.x, b.min.y, b.center.z); break;
                case Align.Top: point = new Vector3(b.center.x, b.max.y, b.center.z); break;
                default: point = b.center; break;
            }
            model.position += anchor.position - point;
        }

        static bool TryGetBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (!found) { bounds = r.bounds; found = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return found;
        }
    }
}
