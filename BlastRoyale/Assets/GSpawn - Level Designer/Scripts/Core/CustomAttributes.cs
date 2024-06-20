#if UNITY_EDITOR
using System;

namespace GSpawn
{
    [AttributeUsage(AttributeTargets.Field)]
    public class UIFieldConfig : Attribute
    {
        private string  _label          = string.Empty;
        private string  _tooltip        = string.Empty;
        private string  _sectionLabel   = string.Empty;
        private bool    _rowSeparator   = false;

        public string   label           { get { return _label; } }
        public string   tooltip         { get { return _tooltip; } }
        public string   sectionLabel    { get { return _sectionLabel; } }
        public bool     rowSeparator    { get { return _rowSeparator; } }

        public UIFieldConfig(string label, string tooltip)
        {
            if (label != null)      _label      = label;
            if (tooltip != null)    _tooltip    = tooltip;
        }

        public UIFieldConfig(string label, string tooltip, string sectionLabel, bool rowSeparator)
        {
            if (label != null)          _label          = label;
            if (tooltip != null)        _tooltip        = tooltip;
            if (sectionLabel != null)   _sectionLabel   = sectionLabel;

            _rowSeparator = rowSeparator;
        }
    }
}
#endif