#if UNITY_EDITOR
namespace GSpawn
{
    public static class StringEx
    {
        public static string axisIndexToAxisName(int axis)
        {
            switch (axis)
            {
                case 0:

                    return "X";

                case 1:

                    return "Y";

                case 2:

                    return "Z";

                default:

                    return "N/A";
            }
        }

        public static string removeTrailingSlashes(this string str)
        {
            string finalString = str;
            while (finalString.getLastChar() == '\\' || finalString.getLastChar() == '/')
                finalString = finalString.Substring(0, finalString.Length - 1);

            return finalString;
        }

        public static char getLastChar(this string str)
        {
            return str[str.Length - 1];
        }

        public static bool isWhiteSpace(this string str)
        {
            for (int charIndex = 0; charIndex < str.Length; ++charIndex)
            {
                if (!char.IsWhiteSpace(str[charIndex])) return false;
            }

            return true;
        }

        public static bool isDigit(this string str)
        {
            return str.Length == 1 && char.IsDigit(str[0]);
        }

        public static bool isLetter(this string str)
        {
            return str.Length == 1 && char.IsLetter(str[0]);
        }

        public static bool isChar(this string str, char character)
        {
            return str.Length == 1 && str[0] == character;
        }

        public static string replaceAt(this string s, int index, char newChar)
        {
            char[] chars = s.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }

        public static bool beingsWith(this string s, string str)
        {
            if (str.Length > s.Length) return false;

            for (int i = str.Length - 1; i >= 0; --i) 
            {
                if (s[i] != str[i]) return false;
            }

            return true;
        }
    }
}
#endif