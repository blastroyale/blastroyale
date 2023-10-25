#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class UIHeader
    {
        private VisualElement   _panel;
        private Button          _icon;
        private Label           _title;

        public string           title           { get { return _title.text; } set { _title.text = value; } }
        public IStyle           titleStyle      { get { return _title.style; } }
        public Color            backgroundColor { get { return _panel.style.backgroundColor.value; } set { _panel.style.backgroundColor = value; } }
        public Vector2          iconSize2D      { set { _icon.style.width = value.x; _icon.style.height = value.y; _panel.style.minHeight = value.y; } }
        public IStyle           iconStyle       { get { return _icon.style; } }
        public Texture2D        icon
        {
            get { return _icon.style.backgroundImage.value.texture; }
            set
            {
                if (value == _icon.style.backgroundImage.value.texture) return;

                _icon.style.backgroundImage = value;
                if (value != null)
                {
                    _icon.style.width       = value.width;
                    _icon.style.height      = value.height;
                    _panel.style.minHeight  = value.height;
                }
            }
        }

        public UIHeader(VisualElement parentElement, Texture2D iconTexture)
        {
            _panel                      = new VisualElement();
            _panel.style.flexShrink     = 0.0f;
            _panel.style.flexDirection  = new StyleEnum<FlexDirection>(FlexDirection.Row);
            parentElement.Add(_panel);

            _icon                       = new Button();
            _icon.style.backgroundColor = Color.white.createNewAlpha(0.0f);
            _icon.style.setBorderWidth(0.0f);
            _panel.Add(_icon);

            icon = iconTexture;

            _title                                  = new Label();
            _title.style.alignSelf                  = new StyleEnum<Align>(Align.Center);
            _title.style.unityFontStyleAndWeight    = new StyleEnum<FontStyle>(FontStyle.Bold);
            _title.style.color                      = UIValues.headerTextColor;

            _panel.Add(_title);
        }

        public UIHeader(VisualElement parentElement, Texture2D iconTexture, float iconSize)
        {
            _panel                      = new VisualElement();
            _panel.style.flexShrink     = 0.0f;
            _panel.style.flexDirection  = new StyleEnum<FlexDirection>(FlexDirection.Row);
            parentElement.Add(_panel);

            _icon                       = new Button();
            _icon.style.backgroundColor = Color.white.createNewAlpha(0.0f);
            _icon.style.setBorderWidth(0.0f);
            _panel.Add(_icon);

            icon = iconTexture;

            _title                      = new Label();
            _title.style.alignSelf      = new StyleEnum<Align>(Align.Center);
            _title.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            _title.style.color          = UIValues.headerTextColor;

            _panel.Add(_title);

            iconSize2D = Vector2Ex.create(iconSize);
        }

        public void setIconSize(float iconSize)
        {
            iconSize2D = Vector2Ex.create(iconSize);
        }
    }
}
#endif