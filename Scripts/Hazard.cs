using UnityEngine;

namespace EndlessRunner
{
    public enum HazardType { Jump, Slide, Block }

    /// <summary>Chạm vào là chết.</summary>
    public class Hazard : MonoBehaviour
    {
        public HazardType type;

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerRunner player)) player.Die();
        }
    }
}
