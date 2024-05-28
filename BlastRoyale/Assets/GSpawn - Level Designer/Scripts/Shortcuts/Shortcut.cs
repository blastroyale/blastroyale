#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Serializable]
    public class Shortcut : IUIItemStateProvider
    {
        [SerializeField]
        private PluginGuid              _guid               = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private bool                    _canChangeFromUI    = true;
        [SerializeField]
        private KeyCombo                _keyCombo           = new KeyCombo();
        [SerializeField]
        private string                  _name               = string.Empty;
        [SerializeField]
        private List<ShortcutConflict> _conflicts           = new List<ShortcutConflict>();
        [NonSerialized]
        private HashSet<string>         _potentialConflicts = new HashSet<string>(); 
        [SerializeField]
        private bool                    _uiSelected         = false;
        [NonSerialized]
        private CopyPasteMode           _uiCopyPasteMode    = CopyPasteMode.None;

        public PluginGuid               guid                { get { return _guid; } }
        public ShortcutContext          context             { get; set; }
        public PluginCommand            command             { get; set; }
        public string                   shortcutName        { get { return _name; } set { if (!string.IsNullOrEmpty(value)) { _name = value; } } }
        public KeyCombo                 keyCombo            { get { return _keyCombo; } }
        public int                      numConflicts        { get { return _conflicts.Count; } }
        public bool                     hasConflicts        { get { return _conflicts.Count != 0; } }
        public bool                     canChangeFromUI     { get { return _canChangeFromUI; } set { _canChangeFromUI = value; } }
        public bool                     uiSelected          { get { return _uiSelected; } set { _uiSelected = value; } }
        public CopyPasteMode            uiCopyPasteMode     { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }

        public bool executeCommand()
        {
            if ((context != null && !context.active) || !keyCombo.isActive()) return false;

            ShortcutLogger.instance.log(this);
            command.enter();
            return true;
        }

        public bool executeOrDisableModifierCommand()
        {
            if (!keyCombo.hasModifiersOnly()) return false;

            if ((context != null && !context.active) || !keyCombo.isActive())
            {
                command.exit();
                return false;
            }

            ShortcutLogger.instance.log(this);
            command.enter();
            return true;
        }

        public bool disableCommand()
        {
            if ((context != null && !context.active) || !keyCombo.isActive())
            {
                command.exit();
                return true;
            }

            return false;
        }

        public bool conflictsWith(Shortcut other)
        {
            if (this == other) return false;        

            Type globalContextType  = GlobalShortcutContext.instance.GetType();
            Type otherContextType   = other.context.GetType();
            Type contextType        = context.GetType();

            if (otherContextType != contextType && 
                otherContextType != globalContextType && 
                contextType != globalContextType && 
                !_potentialConflicts.Contains(other.shortcutName)) return false;

            return keyCombo.conflictsWith(other.keyCombo);
        }

        public void clearConflicts()
        {
            _conflicts.Clear();
        }

        public void addConflict(ShortcutConflict conflict)
        {
            _conflicts.Add(conflict);
        }

        public void addPotentialConflict(Shortcut shortcut)
        {
            _potentialConflicts.Add(shortcut.shortcutName);
            shortcut._potentialConflicts.Add(shortcutName);
        }

        public string getConflictsTooltip()
        {
            string tooltip = string.Empty;
            if (hasConflicts)
            {
                tooltip = "The following conflicts have been detected:\r\n";
                foreach(var c in _conflicts)
                {
                    tooltip += "-" + c.shortcutName + "[Category: " + c.categoryName + "]\r\n";
                }
            }

            return tooltip;
        }
    }
}
#endif