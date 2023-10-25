#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class Keyboard : Singleton<Keyboard>
    {
        private class KeyInfo
        {
            public bool     isDown              = false;
            public double   keyDownTime         = 0.0f;
            public double   timeSinceKeyDown    = 0.0f;

            public void onKeyDown()
            {
                isDown              = true;
                keyDownTime         = EditorApplication.timeSinceStartup;
                timeSinceKeyDown    = 0.0f;
            }

            public void onKeyNoLongerDown()
            {
                isDown              = false;
                keyDownTime         = 0.0f;
                timeSinceKeyDown    = 0.0f;
            }

            public void updateTimeSinceDown()
            {
                timeSinceKeyDown = EditorApplication.timeSinceStartup - keyDownTime;
            }
        }

        private Dictionary<KeyCode, bool>   _keyStates              = new Dictionary<KeyCode, bool>();
        private KeyInfo                     _shiftInfo              = new KeyInfo();
        private KeyInfo                     _altInfo                = new KeyInfo();
        private KeyInfo                     _ctrlInfo               = new KeyInfo();
        private KeyInfo                     _cmdInfo                = new KeyInfo();

        public double                       shiftTimeSinceDown      { get { return _shiftInfo.timeSinceKeyDown; } }
        public double                       altTimeSinceDown        { get { return _altInfo.timeSinceKeyDown; } }
        public double                       ctrlTimeSinceDown       { get { return _ctrlInfo.timeSinceKeyDown; } }
        public double                       cmdTimeSinceDown        { get { return _cmdInfo.timeSinceKeyDown; } }

        public void updateModifierInfo()
        {
            Event e = Event.current;
            if (e.shift && !_shiftInfo.isDown) _shiftInfo.onKeyDown();
            else if (!e.shift && _shiftInfo.isDown) _shiftInfo.onKeyNoLongerDown();
            else if (e.shift && _shiftInfo.isDown) _shiftInfo.updateTimeSinceDown();

            if (e.alt && !_altInfo.isDown) _altInfo.onKeyDown();
            else if (!e.alt && _altInfo.isDown) _altInfo.onKeyNoLongerDown();
            else if (e.alt && _altInfo.isDown) _altInfo.updateTimeSinceDown();

            if (e.control && !_ctrlInfo.isDown) _ctrlInfo.onKeyDown();
            else if (!e.control && _ctrlInfo.isDown) _ctrlInfo.onKeyNoLongerDown();
            else if (e.control && _ctrlInfo.isDown) _ctrlInfo.updateTimeSinceDown();

            if (e.command && !_cmdInfo.isDown) _cmdInfo.onKeyDown();
            else if (!e.command && _cmdInfo.isDown) _cmdInfo.onKeyNoLongerDown();
            else if (e.command && _cmdInfo.isDown) _cmdInfo.updateTimeSinceDown();
        }

        public void clearButtonStates()
        {
            _keyStates.Clear();
            _shiftInfo.onKeyNoLongerDown();
            _altInfo.onKeyNoLongerDown();
            _ctrlInfo.onKeyNoLongerDown();
            _cmdInfo.onKeyNoLongerDown();
        }

        public void onKeyDown(KeyCode key)
        {
            if (key == KeyCode.None) return;

            if (_keyStates.ContainsKey(key)) _keyStates[key] = true;
            else _keyStates.Add(key, true);
        }

        public void onKeyUp(KeyCode key)
        {
            if (key == KeyCode.None) return;

            if (_keyStates.ContainsKey(key)) _keyStates[key] = false;
            else _keyStates.Add(key, false);
        }

        public bool isKeyDown(KeyCode key)
        {
            if (key == KeyCode.None) return false;

            if (key == KeyCode.LeftControl || key == KeyCode.RightControl)  return Event.current.control;
            if (key == KeyCode.LeftCommand || key == KeyCode.RightCommand)  return Event.current.command;
            if (key == KeyCode.LeftShift || key == KeyCode.RightShift)      return Event.current.shift;

            if (!_keyStates.ContainsKey(key)) return false;
            return _keyStates[key];
        }
    }
}
#endif