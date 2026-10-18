using System;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>Kılıcın yazılabilir olay replikleri.</summary>
    [CreateAssetMenu(menuName = "Kanca/Dialogue/Sword Line Set", fileName = "SwordLines")]
    public class SwordLineSet : ScriptableObject
    {
        [Serializable]
        public class CueLines
        {
            [Tooltip("Repliklerin ait olduğu olay.")]
            public SwordCue cue;
            [Tooltip("Bu olayda seçilebilecek replikler.")]
            public string[] lines = Array.Empty<string>();
            [Tooltip("Seçilme olasılığı, 0-1. Kaynak: Ö.")]
            [Range(0f, 1f)] public float chance = 1f;
            [Tooltip("Aynı olayın yeniden konuşması için bekleme süresi (s). Kaynak: Ö.")]
            [Min(0f)] public float cooldown = 0f;
        }

        [Tooltip("Olaylar ve replikleri.")]
        [SerializeField] CueLines[] cues = CreateDefaults();
        [Tooltip("İki replik arasındaki en kısa süre (s). Kaynak: Ö.")]
        [Min(0f)] public float globalCooldown = 1.5f;
        [Tooltip("Balonun sabit görünme süresi (s). Kaynak: Ö.")]
        [Min(0f)] public float displayBaseSeconds = 0.8f;
        [Tooltip("Her karakter için gösterim süresi (s). Kaynak: Ö.")]
        [Min(0f)] public float displaySecondsPerCharacter = 0.035f;
        [Tooltip("Daktilo efektinin hızı (karakter/s). Kaynak: Ö.")]
        [Min(1f)] public float charactersPerSecond = 28f;
        [Tooltip("Oyuncu bu süre hareketsiz kalınca Idle bir kez söylenir (s). Kaynak: Ö.")]
        [Min(0f)] public float idleSeconds = 8f;
        [Tooltip("Son fırlamadan sonra bu sürede saplanırsa zincir sayılır (s). Kaynak: Ö.")]
        [Min(0f)] public float chainWindow = 1.2f;
        [Tooltip("Oyuncu hareket sayılma eşiği (u). Kaynak: Ö.")]
        [Min(0f)] public float movementThreshold = 0.01f;

        public CueLines[] Cues => cues;

        public CueLines GetCue(SwordCue cue)
        {
            if (cues == null) return null;
            for (var i = 0; i < cues.Length; i++)
                if (cues[i] != null && cues[i].cue == cue) return cues[i];
            return null;
        }

        static CueLines[] CreateDefaults() => new[]
        {
            Make(SwordCue.FirstThrow, "Hadi bakalım, kolunu savur.", "İlk atış bedava. Sanırım.", "Beni fırlat, kendini değil."),
            Make(SwordCue.Stick, "Tam isabet. Benden beklendiği gibi.", "Tuttum! Şimdi sıra sende.", "Duvara değil, tam yerine."),
            Make(SwordCue.Miss, "Iskaladık. Daha çok ben ıskaladım.", "Geri geliyorum, suratını asma.", "Bir daha deneriz. Ben hazırım."),
            Make(SwordCue.Launch, "Uç bakalım, ben tutarım.", "İşte şimdi havalısın.", "Güzel kalkış. İnişi konuşuruz."),
            Make(SwordCue.ChainLaunch, "Bir daha mı? Kanat takalım bari.", "Seri gidiyorsun. Ben de buradayım.", "Arka arkaya! Bunu sevdim."),
            Make(SwordCue.Death, "Ben iyiyim. Sen biraz dağılmışsın.", "Düşmek de bir tür ilerleme.", "Bir dahakine daha az ölüm, olur mu?"),
            Make(SwordCue.Respawn, "Yeniden buradayız. Bu kez dikkat.", "Döndün. Özlemiş gibi yapayım.", "Tamam, planı bu kez uygula."),
            Make(SwordCue.Idle, "Mola mı? Benim de keskinliğim dinlensin.", "Hareketsizliğin etkileyici.", "Bir yere mi gidiyorduk?")
        };

        static CueLines Make(SwordCue cue, params string[] lines) => new CueLines { cue = cue, lines = lines, chance = 1f, cooldown = 0f };
    }
}
