using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Editor.Level
{
    public enum LevelLegendKind { TileGround, TileNonHookable, TileDecor, Prefab, HazardZone, Exit }

    [Serializable]
    public sealed class LevelLegendEntry
    {
        public string character;
        public LevelLegendKind kind;
        public string prefabPath;
        public Vector2 offset;
    }

    [CreateAssetMenu(menuName = "Kanca/Level Legend")]
    public sealed class LevelLegend : ScriptableObject
    {
        public List<LevelLegendEntry> entries = new List<LevelLegendEntry>();
        public LevelLegendEntry Find(char character) => entries.Find(e => !string.IsNullOrEmpty(e.character) && e.character[0] == character);
    }
}
