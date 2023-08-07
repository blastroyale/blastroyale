#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GSpawn
{
    public class ShortcutLogger : Singleton<ShortcutLogger>
    {
        class ShortcutInfo
        {
            public Shortcut     shortcut    = null;
            public double       logTime     = 0.0;

            public void copy(ShortcutInfo src)
            {
                shortcut        = src.shortcut;
                logTime         = src.logTime;
            }

            public void reset()
            {
                shortcut    = null;
                logTime     = 0.0;
            }
        }

        const double        _displayTime    = 3.0;
        const int           _numShortcuts   = 3;
        ShortcutInfo[]      _shortcuts      = new ShortcutInfo[_numShortcuts];

        public ShortcutLogger()
        {
            for (int i = 0; i < _numShortcuts; ++i) 
            {
                _shortcuts[i] = new ShortcutInfo();
            }
        }

        public void log(Shortcut shortcut)
        {
            if (!InputPrefs.instance.logShortcuts) return;

            _shortcuts[2].copy(_shortcuts[1]);
            _shortcuts[1].copy(_shortcuts[0]);

            _shortcuts[0].shortcut  = shortcut;
            _shortcuts[0].logTime   = EditorApplication.timeSinceStartup;
        }

        public void onSceneGUI()
        {           
            double editorTime = EditorApplication.timeSinceStartup;
            for (int i = _numShortcuts - 1; i >= 0; --i)
            {
                var s = _shortcuts[i];
                if (s.shortcut == null) continue;

                if ((editorTime - s.logTime) >= _displayTime) s.reset();
            }

            if (!InputPrefs.instance.logShortcuts) return;

            var labelStyle              = new GUIStyle("label");
            labelStyle.fontStyle        = FontStyle.Bold;
            labelStyle.fontSize         = InputPrefs.instance.shortcutLogFontSize;
            labelStyle.normal.textColor = InputPrefs.instance.shortcutLogTextColor;

            Handles.BeginGUI();
            float areaHeight    = _numShortcuts * (labelStyle.fontSize + 10.0f);
            Rect viewRect       = PluginCamera.camera.pixelRect;
            GUILayout.BeginArea(new Rect(0.0f, viewRect.yMax - areaHeight, viewRect.width, areaHeight));
            for (int i = 0; i < _numShortcuts; ++i)
            {
                var s = _shortcuts[i];
                if (s.shortcut != null)
                {
                    EditorGUILayout.LabelField("[" + s.shortcut.keyCombo.ToString() + "] " + s.shortcut.shortcutName, labelStyle, GUILayout.Height(labelStyle.fontSize + 8.0f));
                }
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}
#endif