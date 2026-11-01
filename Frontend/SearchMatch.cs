using System;
using System.Collections.Generic;
using System.Linq;

namespace SKILL_UP.Frontend
{
    /// <summary>
    /// How every search box in the site matches: typing the start of a word finds it ("A" finds "AI Fundamentals" and
    /// "Advanced SQL", "fund" finds "AI Fundamentals"). Each word typed must begin some word of the text, in any order.
    /// Short descriptions are only searched once a word of 3 or more letters is typed, so "a" does not match every course
    /// through "a", "an" and "and".
    /// </summary>
    public static class SearchMatch
    {
        /// <summary>The lower-case words of a text: runs of letters and digits.</summary>
        public static List<string> Words(string text)
        {
            var words = new List<string>();
            var start = -1;
            text = text ?? "";
            for (int i = 0; i <= text.Length; i++)
            {
                var inWord = i < text.Length && char.IsLetterOrDigit(text[i]);
                if (inWord && start < 0) start = i;
                else if (!inWord && start >= 0)
                {
                    words.Add(text.Substring(start, i - start).ToLowerInvariant());
                    start = -1;
                }
            }
            return words;
        }

        static bool Covers(List<string> wanted, List<string> words) =>
            wanted.All(w => words.Any(x => x.StartsWith(w, StringComparison.Ordinal)));

        /// <summary>
        /// True when every word of the term begins a word of main, or (for a term with a word of 3+ letters) of main or detail.
        /// An empty term matches everything. With titleStart, a single typed letter matches only what starts with it, so "A" shows
        /// the courses whose name starts with A, not every course with the word "a" in it. main must then begin with the title.
        /// </summary>
        public static bool Matches(string term, string main, string detail = null, bool titleStart = false)
        {
            var wanted = Words(term);
            if (wanted.Count == 0) return true;
            var words = Words(main);
            if (titleStart && wanted.Count == 1 && wanted[0].Length == 1) return words.Count > 0 && words[0].StartsWith(wanted[0], StringComparison.Ordinal);
            if (Covers(wanted, words)) return true;
            return detail != null && wanted.Max(w => w.Length) >= 3 && Covers(wanted, words.Concat(Words(detail)).ToList());
        }

        /// <summary>For ordering results: 0 when the title starts with what was typed, 1 when one of its words does, 2 for the rest.</summary>
        public static int Rank(string term, string title)
        {
            var wanted = Words(term);
            if (wanted.Count == 0) return 0;
            var words = Words(title);
            if (string.Join(" ", words).StartsWith(string.Join(" ", wanted), StringComparison.Ordinal)) return 0;
            return words.Any(x => x.StartsWith(wanted[0], StringComparison.Ordinal)) ? 1 : 2;
        }
    }
}
