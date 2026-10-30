using UnityEngine;

namespace Game.Flow
{
    /// <summary>Oyunun sahne sırası: başlık, sıralı bölümler, bitiş. Sahne adları Build Settings'teki adlardır.</summary>
    [CreateAssetMenu(menuName = "Kanca/Flow/Scene Catalog", fileName = "SceneCatalog")]
    public sealed class SceneCatalog : ScriptableObject
    {
        public string titleScene = "Title";
        public string[] levels = { "Level_01" };
        public string endScene = "End";

        public string FirstLevel => levels != null && levels.Length > 0 ? levels[0] : string.Empty;

        public bool IsLevel(string sceneName) => System.Array.IndexOf(levels ?? System.Array.Empty<string>(), sceneName) >= 0;

        /// <summary>Bölümden sonraki sahne; son bölümden sonra bitiş sahnesi; katalogda olmayan sahne için boş.</summary>
        public string NextAfter(string current)
        {
            if (levels == null) return string.Empty;
            for (int i = 0; i < levels.Length; i++)
                if (levels[i] == current) return i + 1 < levels.Length ? levels[i + 1] : endScene;
            return string.Empty;
        }
    }
}
