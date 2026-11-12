using UnityEngine;

namespace EndlessRunner
{
    public class Spinner : MonoBehaviour
    {
        public float degreesPerSecond = 180f;

        void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
