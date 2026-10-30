using System;
using UnityEngine;

namespace Game.Hook
{
    public enum HookWeightClass { Light, Heavy }

    /// <summary>
    /// Kancanın çekebildiği hedef. Hafif hedef oyuncuya çekilir, ağır hedef oyuncuyu kendine çeker.
    /// Kanca takılınca Hooked, bırakınca Released yayınlar; düşman ve boss bu sırada kendi hızını yazmaz.
    /// Bileşen kapalıysa kanca hedefi çekmez, yalnız yüzey kuralları uygulanır.
    /// </summary>
    public class HookPullable : MonoBehaviour
    {
        [SerializeField] HookWeightClass weightClass;
        [SerializeField] float pullSpeedOverride;
        [SerializeField] Rigidbody2D body;
        public HookWeightClass WeightClass => weightClass;
        public float PullSpeedOverride => pullSpeedOverride;
        public Rigidbody2D Body => body != null ? body : GetComponentInParent<Rigidbody2D>();

        /// <summary>Kanca şu an bu hedefe takılı (hafifte sarılıyor, ağırda oyuncu çekiliyor).</summary>
        public bool IsHooked { get; private set; }
        public event Action Hooked;
        public event Action Released;

        /// <summary>GrappleHook çağırır.</summary>
        public void NotifyHooked()
        {
            if (IsHooked) return;
            IsHooked = true;
            Hooked?.Invoke();
        }

        /// <summary>GrappleHook çağırır; kanca hedefi bıraktığında, geri döndüğünde veya ele alındığında.</summary>
        public void NotifyReleased()
        {
            if (!IsHooked) return;
            IsHooked = false;
            Released?.Invoke();
        }

        void OnDisable() => NotifyReleased();
    }
}
