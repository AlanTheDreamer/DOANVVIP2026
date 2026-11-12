using UnityEngine;

namespace EndlessRunner
{
    /// <summary>Sinh đường, chướng ngại, coin, cảnh 2 bên phía trước người chơi và xoá phía sau.</summary>
    public class LevelSpawner : MonoBehaviour
    {
        RunnerGame game;
        RunnerTheme theme;
        Transform player;

        float nextGroundZ, nextRowZ, nextSceneryZ;
        int chunkIndex;
        readonly int[] lanes = { -1, 0, 1 };

        float RoadWidth => game.laneWidth * 3f + 1.5f;

        public void Setup(RunnerGame game, RunnerTheme theme, Transform player)
        {
            this.game = game;
            this.theme = theme;
            this.player = player;
        }

        public void ResetLevel()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            chunkIndex = 0;
            nextGroundZ = -game.chunkLength;
            nextSceneryZ = -game.despawnBehind;
            nextRowZ = game.safeStartDistance;
            Tick(game.startSpeed);
        }

        public void Tick(float speed)
        {
            float pz = player.position.z;
            float ahead = pz + game.spawnAhead;

            while (nextGroundZ < ahead)
            {
                SpawnGround(nextGroundZ);
                nextGroundZ += game.chunkLength;
            }

            while (nextRowZ < ahead)
            {
                // Chạy càng nhanh thì khoảng cách giữa các hàng càng xa
                float gap = Random.Range(game.minRowGap, game.maxRowGap) + speed * game.gapPerSpeed;
                SpawnRow(nextRowZ, gap);
                nextRowZ += gap;
            }

            while (nextSceneryZ < ahead)
            {
                SpawnScenery(nextSceneryZ);
                nextSceneryZ += Random.Range(4f, 9f);
            }

            float limit = pz - game.despawnBehind;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.position.z < limit) Destroy(c.gameObject);
            }
        }

        // ---------- Rows ----------

        void SpawnRow(float z, float gap)
        {
            // Cả hàng cùng loại nhảy/trượt
            if (Random.value < game.fullRowChance)
            {
                var t = Random.value < 0.5f ? HazardType.Jump : HazardType.Slide;
                foreach (int l in lanes) SpawnHazard(t, l, z);
                if (Random.value < game.coinChance) SpawnCoinLine(lanes[Random.Range(0, 3)], z, gap);
                return;
            }

            Shuffle(lanes);
            int count = Random.value < game.doubleObstacleChance ? 2 : 1; // luôn chừa ít nhất 1 làn trống
            for (int i = 0; i < count; i++)
                SpawnHazard((HazardType)Random.Range(0, 3), lanes[i], z);

            if (Random.value < game.coinChance) SpawnCoinLine(lanes[count], z, gap);
        }

        void SpawnHazard(HazardType type, int lane, float z)
        {
            float w = game.laneWidth;
            var root = new GameObject("Hazard_" + type);
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(lane * w, 0f, z);

            // HITBOX cố định theo loại — model chỉ là "vỏ"
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Vector3 visualSize;
            switch (type)
            {
                case HazardType.Jump:
                    box.size = new Vector3(w * 0.7f, 0.8f, 0.6f);
                    box.center = new Vector3(0f, 0.4f, 0f);
                    visualSize = new Vector3(w * 0.8f, 0.8f, 0f);
                    break;
                case HazardType.Slide:
                    box.size = new Vector3(w * 0.7f, 2.6f, 0.6f);   // chặn từ 0.9m → 3.5m
                    box.center = new Vector3(0f, 2.2f, 0f);
                    visualSize = new Vector3(w * 0.9f, 3.4f, 0f);
                    break;
                default:
                    box.size = new Vector3(w * 0.7f, 3f, 1.2f);
                    box.center = new Vector3(0f, 1.5f, 0f);
                    visualSize = new Vector3(w * 0.85f, 3f, 0f);
                    break;
            }
            root.AddComponent<Hazard>().type = type;

            var slot = ModelSlot.Pick(theme.GetObstacles(type));
            if (slot != null) ModelUtil.Attach(slot, root.transform, visualSize, ModelUtil.Align.Bottom);
            else Placeholders.Hazard(type, root.transform, w);
        }

        void SpawnCoinLine(int lane, float rowZ, float gap)
        {
            const float spacing = 2.5f;
            int n = Mathf.Clamp(Mathf.FloorToInt((gap - 6f) / spacing), 3, 8);
            for (int i = 0; i < n; i++) SpawnCoin(lane, rowZ + 3f + i * spacing);
        }

        void SpawnCoin(int lane, float z)
        {
            var root = new GameObject("Coin");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(lane * game.laneWidth, 1f, z);

            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;
            root.AddComponent<Pickup>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.gameObject.AddComponent<Spinner>();

            if (theme.coin.IsSet) ModelUtil.Attach(theme.coin, visual, new Vector3(0.8f, 0.8f, 0f), ModelUtil.Align.Center);
            else Placeholders.Coin(visual);
        }

        // ---------- Environment ----------

        void SpawnGround(float z)
        {
            var root = new GameObject("Ground");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(0f, 0f, z);

            var anchor = new GameObject("Anchor").transform;
            anchor.SetParent(root.transform, false);
            anchor.localPosition = new Vector3(0f, 0f, game.chunkLength * 0.5f);

            var slot = ModelSlot.Pick(theme.groundChunks);
            if (slot != null)
                ModelUtil.Attach(slot, anchor, new Vector3(RoadWidth, 0f, game.chunkLength), ModelUtil.Align.Top, true);
            else
                Placeholders.Ground(anchor, RoadWidth, game.chunkLength, game.laneWidth, chunkIndex);
            chunkIndex++;
        }

        void SpawnScenery(float z)
        {
            float half = RoadWidth * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                if (Random.value > 0.8f) continue;
                var root = new GameObject("Scenery");
                root.transform.SetParent(transform, false);
                root.transform.position = new Vector3(side * (half + Random.Range(2.5f, 8f)), 0f, z + Random.Range(-1f, 1f));
                root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                var slot = ModelSlot.Pick(theme.scenery);
                if (slot != null) ModelUtil.Attach(slot, root.transform, new Vector3(3f, 0f, 3f), ModelUtil.Align.Bottom);
                else Placeholders.Scenery(root.transform);
            }
        }

        static void Shuffle(int[] a)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (a[i], a[j]) = (a[j], a[i]);
            }
        }
    }
}
