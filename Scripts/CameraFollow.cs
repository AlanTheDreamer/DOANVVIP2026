using UnityEngine;

namespace EndlessRunner
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 4.5f, -7.5f);
        public float lookAhead = 6f;
        [Range(0f, 1f)] public float followX = 0.6f;
        public float smooth = 8f;

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = Desired();
            Vector3 pos = transform.position;
            float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);
            pos.x = Mathf.Lerp(pos.x, desired.x, k);
            pos.y = Mathf.Lerp(pos.y, desired.y, k);
            pos.z = desired.z; // bám cứng trục Z để không bị tụt lại khi chạy nhanh
            transform.position = pos;
            LookAtTarget();
        }

        public void Snap()
        {
            if (target == null) return;
            transform.position = Desired();
            LookAtTarget();
        }

        Vector3 Desired()
        {
            Vector3 t = target.position;
            return new Vector3(t.x * followX + offset.x, offset.y + t.y * 0.5f, t.z + offset.z);
        }

        void LookAtTarget()
        {
            Vector3 t = target.position;
            transform.LookAt(new Vector3(t.x * followX, 1f, t.z + lookAhead));
        }
    }
}
