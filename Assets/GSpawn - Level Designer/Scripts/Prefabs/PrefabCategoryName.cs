#if UNITY_EDITOR
using System;

namespace GSpawn
{
    public class PrefabCategoryName
    {
        private string  _name   = string.Empty;

        public string   name    { get { return _name; } }

        public void extract(string prefabName)
        {
            int nameLength      = prefabName.Length;
            int currentLength   = 0;
            for (int i = 0; i < nameLength; ++i)
            {
                // Check for '_' followed by digit
                if (i < nameLength - 1)
                {
                    if (prefabName[i] == '_' && Char.IsDigit(prefabName[i + 1]))
                        break;
                }

                if (Char.IsDigit(prefabName[i]))
                    break;

                ++currentLength;
            }

            _name = prefabName.Substring(0, currentLength);
        }

        public bool matchPrefabName(string prefabName) 
        {
            if (prefabName.beingsWith(_name))
            {
                // We need 2 more characters to make a decision, but if they are not available, match it
                int i = _name.Length;
                if (i == prefabName.Length || (i + 1) == prefabName.Length) return true;

                // Note: We don't want to match coffin_01 to coffin_lid_01 for example.
                if (prefabName[i] == '_')
                {
                    // Don't match if a digit is not present
                    return char.IsDigit(prefabName[i + 1]);
                }
                else return char.IsDigit(prefabName[i]);
            }
            else return false;
        }
    }
}
#endif