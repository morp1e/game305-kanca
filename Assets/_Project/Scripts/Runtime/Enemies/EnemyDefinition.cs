using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Düşman davranışı ve başlangıç ayarları.</summary>
    [CreateAssetMenu(menuName = "Kanca/Dusman/Enemy Definition", fileName = "EnemyDef_")]
    public class EnemyDefinition : ScriptableObject
    {
        [Tooltip("Maksimum can. Ö: oynayarak ayarlanacak.")] public float maxHealth = 2f;
        [Tooltip("Devriye hızı, u/s. Ö: oynayarak ayarlanacak.")] public float moveSpeed = 2f;
        [Tooltip("Takip veya saldırı hızı, u/s. Ö: oynayarak ayarlanacak.")] public float chaseSpeed = 5f;
        [Tooltip("Devriye merkezinden uzaklık, u. Ö: oynayarak ayarlanacak.")] public float patrolRange = 2f;
        [Tooltip("Oyuncuyu fark etme uzaklığı, u. Ö: oynayarak ayarlanacak.")] public float detectionRadius = 6f;
        [Tooltip("Hedefi bırakma uzaklığı, u. Ö: oynayarak ayarlanacak.")] public float loseTargetRadius = 9f;
        [Tooltip("Saldırı telegraph süresi, s. Ö: oynayarak ayarlanacak.")] public float telegraphDuration = .4f;
        [Tooltip("Saldırı süresi, s. Ö: oynayarak ayarlanacak.")] public float attackDuration = .8f;
        [Tooltip("Toparlanma süresi, s. Ö: oynayarak ayarlanacak.")] public float recoverDuration = 1f;
        [Tooltip("Saldırılar arası bekleme, s. Ö: oynayarak ayarlanacak.")] public float attackCooldown = 1f;
        [Tooltip("Temas hasarı. Ö: oynayarak ayarlanacak.")] public float contactDamage = 1f;
        [Tooltip("Özel saldırı hasarı. Ö: oynayarak ayarlanacak.")] public float attackDamage = 1f;
        [Tooltip("Hasar sersemletmesi, s. Hollow Knight Tiktik 0,5 s. Ö: oynayarak ayarlanacak.")] public float hurtStunDuration = .5f;
        [Tooltip("Sersemken geri itme hızının sönme ivmesi, u/s². Ö: oynayarak ayarlanacak.")] public float hurtDeceleration = 40f;
        [Range(0f, 1f), Tooltip("Geri itme direnci. Ö: oynayarak ayarlanacak.")] public float knockbackResistance;
        [Tooltip("Yerçekimi ölçeği. Ö: oynayarak ayarlanacak.")] public float gravityScale = 3f;
        [Tooltip("Kalkan yay açısı. Sıfır kalkanı kapatır. Ö: oynayarak ayarlanacak.")] public float shieldArcDegrees;
        [Tooltip("Mermi tanımı. Ö: oynayarak ayarlanacak.")] public ProjectileDefinition projectile;
        [Tooltip("Hasar parlaması. Ö: oynayarak ayarlanacak.")] public Color hitFlashColor = Color.white;
        [Tooltip("Hasar parlaması süresi, s. Ö: oynayarak ayarlanacak.")] public float hitFlashDuration = .12f;
        [Tooltip("Telegraph rengi. Ö: oynayarak ayarlanacak.")] public Color telegraphColor = Color.yellow;
        [Tooltip("Oyuncu yeniden doğunca bu düşmanı sıfırla. Ö: oynayarak ayarlanacak.")] public bool resetOnPlayerRespawn = true;
        [Tooltip("Uçan düşmanın süzülme genliği, u. Ö: oynayarak ayarlanacak.")] public float hoverAmplitude = .35f;
        [Tooltip("Uçan düşmanın süzülme frekansı, Hz. Ö: oynayarak ayarlanacak.")] public float hoverFrequency = 1.5f;
        [Tooltip("Saldırı kutusu boyutu, u. Ö: oynayarak ayarlanacak.")] public Vector2 attackBoxSize = new Vector2(2f, 1.5f);
        [Tooltip("Saldırı kutusunun düşman merkezinden ileri uzaklığı, u. Ö: oynayarak ayarlanacak.")] public float attackBoxForward = 1f;
    }
}
