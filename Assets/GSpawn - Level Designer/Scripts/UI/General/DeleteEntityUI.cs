#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class DeleteEntityUI : PluginUI
    {
        private string _headerLabel     = string.Empty;
        private string _question        = string.Empty;

        public Action onDelete          { get; set; }
        public string headerLabel       { get { return _headerLabel; } set { if (value != null) _headerLabel = value; } }
        public string question          { get { return _question; } set { if (value != null) _question = value; } }

        public static DeleteEntityUI instance { get { return GSpawn.active.deleteEntityUI; } }

        protected override void onBuild()
        {
            rootElement.style.setMargins(UIValues.pluginWindowMargin);

            var headerLabel = new Label();
            contentContainer.Add(headerLabel);
            headerLabel.text = _headerLabel;
            headerLabel.style.marginBottom = 5.0f;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var questionLabel = new Label();
            contentContainer.Add(questionLabel);
            questionLabel.text = _question;
            questionLabel.style.marginBottom = 10.0f;

            var buttons = new VisualElement();
            contentContainer.Add(buttons);
            buttons.style.marginTop = 15.0f;
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexShrink = 0.0f;

            UI.createHorizontalSpacer(buttons);

            var createBtn = new Button();
            buttons.Add(createBtn);
            createBtn.text = "Delete";
            createBtn.style.width = 100.0f;
            createBtn.clicked += () => { if (onDelete != null) { onDelete(); targetWindow.Close(); } };

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