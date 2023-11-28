#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    [Serializable]
    public class ShortcutCategory : IUIItemStateProvider
    {
        [SerializeField]
        private PluginGuid          _guid               = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private string              _name               = string.Empty;
        [SerializeField]
        private List<Shortcut>      _shortcuts          = new List<Shortcut>();
        [SerializeField]
        private bool                _uiSelected         = false;
        [NonSerialized]
        private CopyPasteMode       _uiCopyPasteMode    = CopyPasteMode.None;

        public PluginGuid           guid                { get { return _guid; } }
        public string               categoryName        { get { return _name; } set { if (!string.IsNullOrEmpty(value)) { _name = value; } } }
        public int                  numShortcuts        { get { return _shortcuts.Count; } }
        public bool                 uiSelected          { get { return _uiSelected; } set { _uiSelected = value; } }
        public CopyPasteMode        uiCopyPasteMode     { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }

        public void clearShortcutConflicts()
        {
            foreach (var sh in _shortcuts)
                sh.clearConflicts();
        }

        public bool executeCommands()
        {
            bool foundActive = false;
            foreach (var s in _shortcuts)
                foundActive |= s.executeCommand();

            return foundActive;
        }

        public bool executeOrDisableModifierCommands()
        {
            bool foundActive = false;
            foreach (var s in _shortcuts)
                foundActive |= s.executeOrDisableModifierCommand();

            return foundActive;
        }

        public bool disableCommands()
        {
            bool disabledCommand = false;
            foreach (var s in _shortcuts)
                disabledCommand |= s.disableCommand();

            return disabledCommand;
        }

        public Shortcut getOrCreateShortcut(string shortcutName, ShortcutContext context, PluginCommand command)
        {
            var sh = findShortcut(shortcutName);
            if (sh == null)
            {
                sh = new Shortcut();
                sh.shortcutName = shortcutName;
                _shortcuts.Add(sh);
            }

            sh.command = command;
            sh.context = context;

            return sh;
        }

        public void getShortcuts(List<Shortcut> shortcuts)
        {
            shortcuts.Clear();
            foreach (var s in _shortcuts)
                shortcuts.Add(s);
        }

        public Shortcut findShortcut(string name)
        {
            return _shortcuts.Find(item => item.shortcutName == name);
        }

        public Shortcut getShortcut(int index)
        {
            return _shortcuts[index];
        }

        public void getShortcutNames(List<string> names)
        {
            names.Clear();
            foreach (var s in _shortcuts)
                names.Add(s.shortcutName);
        }
    }
}
#endif