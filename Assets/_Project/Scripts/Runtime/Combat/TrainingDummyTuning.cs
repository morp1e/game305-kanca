using UnityEngine;

namespace Game.Combat
{
    [CreateAssetMenu(menuName = "Kanca/Tuning/Training Dummy", fileName = "TrainingDummyTuning")]
    public class TrainingDummyTuning : ScriptableObject
    {
        [Tooltip("Hedefin canı. Ö: oynayarak ayarlanacak.")] public float maxHealth = 3f;
        [Tooltip("Canı tükenince yeniden doğma süresi, s. Ö: oynayarak ayarlanacak.")] public float respawnDelay = 2f;
        [Tooltip("İsabet parlaması süresi, s. Ö: oynayarak ayarlanacak.")] public float flashDuration = 0.12f;
        [Tooltip("İsabet parlaması rengi. Ö: oynayarak ayarlanacak.")] public Color hitColor = Color.white;
    }
}
