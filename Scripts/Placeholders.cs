using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    /// <summary>Model tạm bằng khối cơ bản, dùng khi Theme chưa gán prefab.</summary>
    public static class Placeholders
    {
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

        static readonly Color PlayerColor = new Color(0.2f, 0.7f, 1f);
        static readonly Color Dark = new Color(0.1f, 0.1f, 0.15f);
        static readonly Color JumpColor = new Color(1f, 0.6f, 0.1f);
        static readonly Color SlideColor = new Color(0.6f, 0.3f, 0.9f);
        static readonly Color BlockColor = new Color(0.9f, 0.2f, 0.25f);
        static readonly Color PostColor = new Color(0.5f, 0.5f, 0.55f);
        static readonly Color CoinColor = new Color(1f, 0.85f, 0.1f);
        static readonly Color GroundA = new Color(0.32f, 0.34f, 0.38f);
        static readonly Color GroundB = new Color(0.28f, 0.30f, 0.34f);
        static readonly Color LaneLine = new Color(0.85f, 0.85f, 0.85f);

        static readonly Color[] SceneryPalette =
        {
            new Color(0.95f, 0.75f, 0.6f), new Color(0.6f, 0.8f, 0.7f), new Color(0.75f, 0.7f, 0.9f),
            new Color(0.9f, 0.9f, 0.75f), new Color(0.65f, 0.75f, 0.9f)
        };
        static readonly Color TreeGreen = new Color(0.3f, 0.65f, 0.35f);
        static readonly Color Trunk = new Color(0.45f, 0.3f, 0.2f);

        public static GameObject Shape(Transform parent, Vector3 localPos, Vector3 size, Color color,
            PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = type.ToString();
            var col = go.GetComponent<Collider>();
            col.enabled = false;
            Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = GetMaterial(color, r.sharedMaterial);
            return go;
        }

        static Material GetMaterial(Color c, Material baseMat)
        {
            if (!materials.TryGetValue(c, out var m) || m == null)
            {
                m = new Material(baseMat) { color = c };
                materials[c] = m;
            }
            return m;
        }

        public static void Player(Transform root)
        {
            Shape(root, new Vector3(0, 0.9f, 0), new Vector3(0.7f, 0.9f, 0.7f), PlayerColor, PrimitiveType.Capsule);
            Shape(root, new Vector3(0, 1.45f, 0.3f), new Vector3(0.45f, 0.15f, 0.15f), Dark); // "kính" để thấy hướng mặt
        }

        public static void Hazard(HazardType type, Transform root, float laneWidth)
        {
            switch (type)
            {
                case HazardType.Jump:
                    Shape(root, new Vector3(0, 0.4f, 0), new Vector3(laneWidth * 0.8f, 0.8f, 0.5f), JumpColor);
                    break;
                case HazardType.Slide:
                    Shape(root, new Vector3(-laneWidth * 0.42f, 1.7f, 0), new Vector3(0.15f, 3.4f, 0.15f), PostColor);
                    Shape(root, new Vector3(laneWidth * 0.42f, 1.7f, 0), new Vector3(0.15f, 3.4f, 0.15f), PostColor);
                    Shape(root, new Vector3(0, 2.2f, 0), new Vector3(laneWidth * 0.85f, 2.6f, 0.3f), SlideColor);
                    break;
                default:
                    Shape(root, new Vector3(0, 1.5f, 0), new Vector3(laneWidth * 0.85f, 3f, 1.2f), BlockColor);
                    break;
            }
        }

        public static void Coin(Transform root)
        {
            var c = Shape(root, Vector3.zero, new Vector3(0.8f, 0.05f, 0.8f), CoinColor, PrimitiveType.Cylinder);
            c.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        public static void Ground(Transform root, float width, float length, float laneWidth, int index)
        {
            Shape(root, new Vector3(0, -0.25f, 0), new Vector3(width, 0.5f, length), index % 2 == 0 ? GroundA : GroundB);
            Shape(root, new Vector3(-laneWidth * 0.5f, 0.005f, 0), new Vector3(0.08f, 0.01f, length), LaneLine);
            Shape(root, new Vector3(laneWidth * 0.5f, 0.005f, 0), new Vector3(0.08f, 0.01f, length), LaneLine);
            Shape(root, new Vector3(-width * 0.5f, 0.1f, 0), new Vector3(0.3f, 0.2f, length), Dark);
            Shape(root, new Vector3(width * 0.5f, 0.1f, 0), new Vector3(0.3f, 0.2f, length), Dark);
        }

        public static void Scenery(Transform root)
        {
            if (Random.value < 0.4f)
            {
                float h = Random.Range(2f, 4f);
                Shape(root, new Vector3(0, h * 0.5f, 0), new Vector3(0.3f, h * 0.5f, 0.3f), Trunk, PrimitiveType.Cylinder);
                float crown = Random.Range(1.5f, 2.5f);
                Shape(root, new Vector3(0, h + crown * 0.3f, 0), Vector3.one * crown, TreeGreen, PrimitiveType.Sphere);
            }
            else
            {
                float h = Random.Range(2f, 9f), w = Random.Range(1.5f, 3.5f);
                Shape(root, new Vector3(0, h * 0.5f, 0), new Vector3(w, h, w),
                    SceneryPalette[Random.Range(0, SceneryPalette.Length)]);
            }
        }
    }
}
