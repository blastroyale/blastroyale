#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class RenameEntityUI : PluginUI
    {
        private string          _headerLabel         = string.Empty;
        private string          _descLabel           = string.Empty;
        private string          _nameFieldLabel      = "Name: ";
        private string          _currentName         = string.Empty;

        public Action<string>   onRename            { get; set; }
        public string           headerLabel         { get { return _headerLabel; } set { if (value != null) _headerLabel = value; } }
        public string           descriptionLabel    { get { return _descLabel; } set { if (value != null) _descLabel = value; } }
        public string           nameFieldLabel      { get { return _nameFieldLabel; } set { if (value != null) _nameFieldLabel = value; } }
        public string           currentName         { get { return _currentName; } set { if (value != null) _currentName = value; } }

        public static RenameEntityUI instance       { get { return GSpawn.active.renameEntityUI; } }

        protected override void onBuild()
        {
            rootElement.style.setMargins(UIValues.pluginWindowMargin);

            var headerLabel = new Label();
            contentContainer.Add(headerLabel);
            headerLabel.text = _headerLabel;
            headerLabel.style.marginBottom = 5.0f;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var descLabel = new Label();
            contentContainer.Add(descLabel);
            descLabel.text = _descLabel;
            descLabel.style.marginBottom = 10.0f;

            var nameField = new TextField();
            contentContainer.Add(nameField);
            nameField.label = _nameFieldLabel;
            nameField.SetValueWithoutNotify(_currentName);

            var buttons = new VisualElement();
            contentContainer.Add(buttons);
            buttons.style.marginTop = 15.0f;
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexShrink = 0.0f;

            UI.createHorizontalSpacer(buttons);

            var renameBtn = new Button();
            buttons.Add(renameBtn);
            renameBtn.text = "Rename";
            renameBtn.style.width = 100.0f;
            renameBtn.clicked += () => { if (onRename != null && !string.IsNullOrEmpty(nameField.text)) { onRename(nameField.text); targetWindow.Close(); } };
            renameBtn.SetEnabled(nameField.text.Length != 0);
            nameField.RegisterValueChangedCallback(p => { renameBtn.SetEnabled(!string.IsNullOrEmpty(p.newValue) && p.newValue != _currentName); });

            var cancelBtn = new Button();
            buttons.Add(cancelBtn);
            cancelBtn.text = "Cancel";
            cancelBtn.style.width = 100.0f;
            cancelBtn.clicked += () => { targetWindow.Close(); };
        }

        protected override void onRefresh()
        {
        }
    }
}
#endif