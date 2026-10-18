using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Oyuncu gövdesinin kanca gibi dış sistemlere açtığı yüz. Uygulayan: Game.Player.PlayerMotor.
    /// </summary>
    public interface IPlayerBody
    {
        Vector2 Position { get; }

        /// <summary>u/s. Dış sistem yazabilir; motor bir sonraki FixedUpdate'te bu hızla hareket eder.</summary>
        Vector2 Velocity { get; set; }

        bool IsGrounded { get; }

        /// <summary>Gövdenin çarpışma şekli. Kanca, çekiş öncesi önden Cast atmak için kullanır.</summary>
        Collider2D BodyCollider { get; }

        /// <summary>
        /// True iken motor yerçekimini ve oyuncu girdisini uygulamaz, yalnız Velocity ile çarpışmalı hareket eder.
        /// Kanca çekişi boyunca açıktır.
        /// </summary>
        bool ExternalControl { get; set; }

        /// <summary>
        /// Fırlamadan sonra kısa pencere: bu sürede zıplamaya basılırsa zıplama mevcut ivmeye eklenir.
        /// </summary>
        void OpenLaunchJumpWindow(float seconds);
    }
}
