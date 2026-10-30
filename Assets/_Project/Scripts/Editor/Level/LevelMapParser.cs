using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Editor.Level
{
    public enum LevelCellKind { Empty, Ground, NonHookable, Decor, Prefab, HazardZone }

    public sealed class LevelMapData
    {
        public readonly List<string> Rows = new List<string>();
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Comments = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public int Width { get; internal set; }
        public int Height => Rows.Count;
        public int PlayerCount { get; internal set; }
        public float KillHeight => Headers.TryGetValue("killHeight", out var value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : float.NaN;
        public float CameraPadding => Headers.TryGetValue("cameraPadding", out var value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 2f;
    }

    public static class LevelMapParser
    {
        public static LevelMapData Parse(string text, IEnumerable<char> knownCharacters = null)
        {
            var known = knownCharacters == null ? new HashSet<char>("#X~^SCzwftbk dBE") : new HashSet<char>(knownCharacters);
            var map = new LevelMapData();
            if (text == null) throw new ArgumentNullException(nameof(text));
            var lines = text.Replace("\r", string.Empty).Split('\n');
            if (lines.Length > 0 && lines[lines.Length - 1].Length == 0) Array.Resize(ref lines, lines.Length - 1);
            bool contentStarted = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith(";", StringComparison.Ordinal)) { map.Comments.Add(line); continue; }
                if (!contentStarted && string.IsNullOrWhiteSpace(line)) continue;
                if (!contentStarted && line.StartsWith("@", StringComparison.Ordinal))
                {
                    int split = line.IndexOf(' ');
                    if (split < 2) throw new FormatException($"Başlık biçimi hatalı (satır {i + 1}).");
                    map.Headers[line.Substring(1, split - 1)] = line.Substring(split + 1).Trim();
                    continue;
                }
                contentStarted = true;
                map.Rows.Add(line);
                map.Width = Math.Max(map.Width, line.Length);
                for (int column = 0; column < line.Length; column++)
                {
                    char c = line[column];
                    if (c == 'S') map.PlayerCount++;
                    if (!known.Contains(c) && c != '.' && c != ' ') map.Warnings.Add($"Bilinmeyen harita karakteri '{c}' satır {map.Rows.Count}, sütun {column + 1}.");
                }
            }
            if (map.PlayerCount != 1) throw new FormatException($"Haritada tam bir S olmalı; bulunan: {map.PlayerCount}.");
            return map;
        }

        public static bool IsTopTile(LevelMapData map, int x, int y)
        {
            int row = map.Height - 2 - y;
            if (row < 0 || row >= map.Height || x < 0 || x >= map.Rows[row].Length) return true;
            char above = map.Rows[row][x];
            return above == ' ' || above == '.';
        }

        public static char GetCell(LevelMapData map, int x, int y)
        {
            int row = map.Height - 1 - y;
            return row < 0 || row >= map.Height || x < 0 || x >= map.Rows[row].Length ? ' ' : map.Rows[row][x];
        }
    }
}
