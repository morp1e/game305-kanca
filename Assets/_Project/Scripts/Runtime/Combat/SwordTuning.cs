using UnityEngine;

namespace Game.Combat
{
    [CreateAssetMenu(menuName = "Kanca/Tuning/Sword", fileName = "SwordTuning")]
    public class SwordTuning : ScriptableObject
    {
        [Tooltip("Savuruş süresi, s. Hollow Knight nail yaklaşık 0,25 s. Ö: oynayarak ayarlanacak.")] public float attackDuration = 0.25f;
        [Tooltip("Aktif vuruş başlangıcı, normalize süre. Ö: oynayarak ayarlanacak.")] [Range(0f, 1f)] public float activeStart = 0.25f;
        [Tooltip("Aktif vuruş bitişi, normalize süre. Ö: oynayarak ayarlanacak.")] [Range(0f, 1f)] public float activeEnd = 0.7f;
        [Tooltip("Saldırılar arası süre. Hollow Knight yaklaşık 0,4 s. Ö: oynayarak ayarlanacak.")] public float cooldown = 0.4f;
        [Tooltip("Savuruş yayı, derece. Ö: oynayarak ayarlanacak.")] public float swingArcDegrees = 100f;
        [Tooltip("Vuruş kutusu merkezi, nişan yönüne göre. Ö: oynayarak ayarlanacak.")] public Vector2 hitOffset = new Vector2(1f, 0f);
        [Tooltip("Vuruş kutusu boyutu. Ö: oynayarak ayarlanacak.")] public Vector2 hitSize = new Vector2(1.4f, 0.8f);
        [Tooltip("Vuruş hasarı. Ö: oynayarak ayarlanacak.")] public float damage = 1f;
        [Tooltip("Geri itme dürtüsü. Ö: oynayarak ayarlanacak.")] public float knockback = 8f;
        [Tooltip("İsabet anındaki yerel donma süresi, s. Ö: oynayarak ayarlanacak.")] public float hitstop = 0.05f;
        [Tooltip("İsabette kamera travması, 0-1. Ö: oynayarak ayarlanacak.")] [Range(0f, 1f)] public float traumaOnHit = 0.2f;
        [Tooltip("Saldırı girdi tamponu, s. Ö: oynayarak ayarlanacak.")] public float inputBuffer = 0.12f;
    }
}
