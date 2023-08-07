#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.ShortcutManagement;

namespace GSpawn
{
    public static class ShortcutManagerEx
    {
        private static string[] _camNavShortcutNames = 
        { 
            "3D Viewport/Fly Mode Forward", "3D Viewport/Fly Mode Backward", 
            "3D Viewport/Fly Mode Left", "3D Viewport/Fly Mode Right", 
            "3D Viewport/Fly Mode Up", "3D Viewport/Fly Mode Down"
        };

        public static bool isKeyUsedForCameraNavigation(KeyCode key)
        { 
            foreach(var name in _camNavShortcutNames)
            {
                var binding = ShortcutManager.instance.GetShortcutBinding(name);
                foreach (var keyCombo in binding.keyCombinationSequence)
                    if (keyCombo.keyCode == key) return true;
            }

            return false;
        }

        public static bool isUndoRedo(Event e)
        {
            if (e.type == EventType.KeyDown)
            {
                var binding = ShortcutManager.instance.GetShortcutBinding("Main Menu/Edit/Undo");
                if (isBindingActive(binding, e)) return true;

                binding     = ShortcutManager.instance.GetShortcutBinding("Main Menu/Edit/Redo");
                if (isBindingActive(binding, e)) return true;
            }

            return false;
        }

        public static bool isFileSave(Event e)
        {
            if (e.type == EventType.KeyDown)
            {
                var binding = ShortcutManager.instance.GetShortcutBinding("Main Menu/File/Save");
                if (isBindingActive(binding, e)) return true;
            }

            return false;
        }

        private static bool isBindingActive(ShortcutBinding binding, Event e)
        {
            foreach (var keyCombo in binding.keyCombinationSequence)
            {
                if (keyCombo.keyCode == e.keyCode)
                {
                    #if UNITY_EDITOR_OSX
                    bool modPressed = Event.current.command;
                    #else
                    bool modPressed = Event.current.control;
                    #endif
                    if ((keyCombo.action && !modPressed) || (!keyCombo.action && modPressed)) return false;

                    modPressed = Event.current.shift;
                    if ((keyCombo.shift && !modPressed) || (!keyCombo.shift && modPressed)) return false;

                    modPressed = Event.current.alt;
                    if ((keyCombo.alt && !modPressed) || (!keyCombo.alt && modPressed)) return false;

                    return keyCombo.keyCode != KeyCode.None ? Keyboard.instance.isKeyDown(keyCombo.keyCode) : true;
                }
            }

            return false;
        }
    }
}
#endif