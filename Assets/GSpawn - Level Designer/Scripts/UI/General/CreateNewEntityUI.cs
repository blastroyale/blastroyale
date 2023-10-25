#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class CreateNewEntityUI : PluginUI
    {
        private string          _headerLabel        = string.Empty;
        private string          _descLabel          = string.Empty;
        private string          _nameFieldLabel     = "Name: ";

        public Action<string>   onCreate            { get; set; }
        public string           headerLabel         { get { return _headerLabel; } set { if (value != null) _headerLabel = value; } }
        public string           descriptionLabel    { get { return _descLabel; } set { if (value != null) _descLabel = value; } }
        public string           nameFieldLabel      { get { return _nameFieldLabel; } set { if (value != null) _nameFieldLabel = value; } }

        public static CreateNewEntityUI instance    { get { return GSpawn.active.createNewEntityUI; } }

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

            var buttons = new VisualElement();
            contentContainer.Add(buttons);
            buttons.style.marginTop = 15.0f;
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexShrink = 0.0f;

            UI.createHorizontalSpacer(buttons);

            var createBtn = new Button();
            buttons.Add(createBtn);
            createBtn.text = "Create";
            createBtn.style.width = 100.0f;
            createBtn.clicked += () => { if (onCreate != null && !string.IsNullOrEmpty(nameField.text)) { onCreate(nameField.text); targetWindow.Close(); } };
            createBtn.SetEnabled(false);
            nameField.RegisterValueChangedCallback(p => { createBtn.SetEnabled(!string.IsNullOrEmpty(p.newValue)); });

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