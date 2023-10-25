#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public static class StringMatch
    {
        public enum Case
        {
            Sensitive = 0,
            Insensitive,
        }

        public static List<string> match(List<string> strings, string stringToMatch, Case matchCase)
        {
            if (strings.Count == 0) return new List<string>();

            var matches = strings.FindAll(item => item == stringToMatch);
            if (matchCase == Case.Insensitive)
            {
                string lowerToMatch = stringToMatch.ToLower();
                foreach(var str in strings)
                {
                    string lowerCase = str.ToLower();
                    if (lowerCase.Contains(lowerToMatch)) matches.Add(str);
                }
            }
            else
            {
                foreach (var str in strings)
                {
                    if (str.Contains(stringToMatch)) matches.Add(str);
                }
            }

            return matches;
        }
    }
}
#endif