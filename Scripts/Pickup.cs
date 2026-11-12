using UnityEngine;

namespace EndlessRunner
{
    /// <summary>Coin: chạm vào thì cộng điểm và biến mất.</summary>
    public class Pickup : MonoBehaviour
    {
        public int value = 1;

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out PlayerRunner player) || player.IsDead) return;
            if (RunnerGame.Instance != null) RunnerGame.Instance.AddCoins(value);
            Destroy(gameObject);
        }
    }
}
