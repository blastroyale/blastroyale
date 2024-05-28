#if UNITY_EDITOR
using UnityEngine;
using System;

namespace GSpawn
{
    [Serializable]
    public class KeyCombo
    {
        public struct State
        {
            public bool     cmd;
            public bool     ctrl;
            public bool     shift;
            public KeyCode  key;
        }

        [SerializeField]
        private bool    _cmd        = false;
        [SerializeField]
        private bool    _ctrl       = false;
        [SerializeField]
        private bool    _shift      = false;
        [SerializeField]
        private KeyCode _key        = KeyCode.None;

        public bool     cmd         { get { return _cmd; } set { _cmd = value; } }
        public bool     ctrl        { get { return _ctrl; } set { _ctrl = value; } }
        public bool     shift       { get { return _shift; } set { _shift = value; } }
        public KeyCode  key         { get { return _key; } set { _key = value; } }
        public State    state 
        { 
            get { return new State { cmd = _cmd, ctrl = _ctrl, shift = _shift, key = _key }; } 
            set { _cmd = value.cmd; _ctrl = value.ctrl; _shift = value.shift; _key = value.key; } 
        }

        public bool isActive()
        {
            if (isEmpty()) return false;

            if (Event.current.alt) return false;

            bool modPressed = Event.current.command;
            if ((_cmd && !modPressed) || (!_cmd && modPressed)) return false;

            modPressed = Event.current.control;
            if ((_ctrl && !modPressed) || (!_ctrl && modPressed)) return false;
    
            modPressed = Event.current.shift;
            if ((_shift && !modPressed) || (!_shift && modPressed)) return false;

            return _key != KeyCode.None ? Keyboard.instance.isKeyDown(_key) : true;
        }

        public bool hasModifiersOnly()
        {
            return _key == KeyCode.None && (_cmd || _ctrl || _shift);
        }

        public void clear()
        {
            _key        = KeyCode.None;
            _cmd        = false;
            _ctrl       = false;
            _shift      = false;
        }

        public bool isEmpty()
        {
            return _key == KeyCode.None && !_cmd && !_ctrl && !_shift;
        }

        public bool conflictsWith(KeyCombo other)
        {
            if (this == other) return false;

            return _key == other.key && _ctrl == other.ctrl &&
                   _cmd == other.cmd && _shift == other.shift;
        }

        public override string ToString()
        {
            if (isEmpty()) return string.Empty;

            string str = string.Empty;
            if (_cmd)   str += "Cmd+";
            if (_ctrl)  str += "Ctrl+";
            if (_shift) str += "Shift+";
     
            if (_key != KeyCode.None) str += NiceKeyCodeStrings.get(_key);
            else str = str.Remove(str.Length - 1);

            return str;
        }
    }
}
#endif