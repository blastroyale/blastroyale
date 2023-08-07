#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public static class UniqueNameGen
    {
        public static string generate(string desiredName, List<string> names)
        {
            if (names.Count == 0) return desiredName;

            string baseName     = desiredName;
            string finalName    = desiredName;
            int suffixIndex     = 0;

            int lastOpenBracket = finalName.LastIndexOf('(');
            if (lastOpenBracket >= 0)
            {
                int charCursor = lastOpenBracket + 1;
                while (charCursor < finalName.Length && finalName[charCursor] == '0')
                    ++charCursor;

                string suffixString = string.Empty;
                while (charCursor < finalName.Length)
                {
                    if (char.IsDigit(finalName[charCursor])) suffixString += finalName[charCursor];
                    else break;

                    ++charCursor;
                }

                if (charCursor < finalName.Length && finalName[charCursor] == ')')
                {
                    baseName    = finalName.Substring(0, lastOpenBracket).Trim();
                    suffixIndex = suffixString.Length != 0 ? int.Parse(suffixString) : 0;
                }
            }

            while (names.Contains(finalName))
            {
                finalName = baseName + " (" +  suffixIndex + ")";
                ++suffixIndex;
            }

            return finalName;
        }
    }
}
#endif