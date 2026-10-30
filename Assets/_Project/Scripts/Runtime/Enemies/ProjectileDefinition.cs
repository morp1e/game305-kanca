using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Düşman mermisinin ayarları.</summary>
    [CreateAssetMenu(menuName = "Kanca/Dusman/Projectile Definition", fileName = "ProjectileDef_")]
    public class ProjectileDefinition : ScriptableObject
    {
        [Tooltip("Uçuş hızı, u/s. Ö: oynayarak ayarlanacak.")] public float speed = 6f;
        [Tooltip("Ömür, s. Ö: oynayarak ayarlanacak.")] public float lifetime = 4f;
        [Tooltip("İsabet hasarı. Ö: oynayarak ayarlanacak.")] public float damage = 1f;
        [Tooltip("Kılıçla sektirilebilir. Ö: oynayarak ayarlanacak.")] public bool deflectable = true;
        [Tooltip("Sektirme hız çarpanı. Ö: oynayarak ayarlanacak.")] public float reflectSpeedMult = 1.5f;
        [Tooltip("Çarpışma yarıçapı, u. Ö: oynayarak ayarlanacak.")] public float radius = .25f;
    }
}
