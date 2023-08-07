#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public static class NiceKeyCodeStrings
    {
        private static Dictionary<KeyCode, string> _keyStrings = new Dictionary<KeyCode, string>();

        static NiceKeyCodeStrings()
        {
            var keyValues = Enum.GetValues(typeof(KeyCode));
            foreach (KeyCode keyVal in keyValues)
            {
                // Note: We need to check if the key already exists
                //       because there are keys that share the same
                //       integer value (e.g. RightApple & RightCommand).
                if (!_keyStrings.ContainsKey(keyVal))
                    _keyStrings.Add(keyVal, keyVal.ToString());
            }
            
            for (int i = 0; i < 10; ++i)
                _keyStrings[(KeyCode)((int)KeyCode.Alpha0 + i)] = i.ToString();

            _keyStrings[KeyCode.LeftBracket] = "[";
            _keyStrings[KeyCode.RightBracket] = "]";
            _keyStrings[KeyCode.Equals] = "=";
            _keyStrings[KeyCode.Comma] = ",";
            _keyStrings[KeyCode.Semicolon] = ";";
            _keyStrings[KeyCode.Period] = ".";
            _keyStrings[KeyCode.Backslash] = "\\";
            _keyStrings[KeyCode.Slash] = "/";
            _keyStrings[KeyCode.Minus] = "-";
            _keyStrings[KeyCode.BackQuote] = "`";
        }

        public static string get(KeyCode code)
        {
            return _keyStrings[code];
        }
    }
}
#endif