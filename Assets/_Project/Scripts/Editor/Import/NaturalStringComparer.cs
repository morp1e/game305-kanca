using System;
using System.Collections.Generic;

namespace Game.Editor.Import
{
    public sealed class NaturalStringComparer : IComparer<string>
    {
        public static readonly NaturalStringComparer Instance = new NaturalStringComparer();

        public int Compare(string left, string right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            int a = 0, b = 0;
            while (a < left.Length && b < right.Length)
            {
                if (char.IsDigit(left[a]) && char.IsDigit(right[b]))
                {
                    int aEnd = a, bEnd = b;
                    while (aEnd < left.Length && char.IsDigit(left[aEnd])) aEnd++;
                    while (bEnd < right.Length && char.IsDigit(right[bEnd])) bEnd++;
                    string aDigits = left.Substring(a, aEnd - a).TrimStart('0');
                    string bDigits = right.Substring(b, bEnd - b).TrimStart('0');
                    int lengthCompare = aDigits.Length.CompareTo(bDigits.Length);
                    if (lengthCompare != 0) return lengthCompare;
                    int numberCompare = string.CompareOrdinal(aDigits, bDigits);
                    if (numberCompare != 0) return numberCompare;
                    a = aEnd;
                    b = bEnd;
                    continue;
                }
                int charCompare = char.ToLowerInvariant(left[a]).CompareTo(char.ToLowerInvariant(right[b]));
                if (charCompare != 0) return charCompare;
                a++;
                b++;
            }
            return (left.Length - a).CompareTo(right.Length - b);
        }
    }
}
