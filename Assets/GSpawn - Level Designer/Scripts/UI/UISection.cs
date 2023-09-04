#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class UISection : ScriptableObject
    {
        [SerializeField]
        private bool            _isContentVisible   = true;
        [NonSerialized]
        private VisualElement   _sectionContainer;
        [NonSerialized]
        private VisualElement   _contentContainer;
        [NonSerialized]
        private Label           _titleLabel;

        public VisualElement    contentContainer    { get { return _contentContainer; } }
        public VisualElement    parent              { get { return _sectionContainer.parent; } }

        public void setTitle(string title)
        {
            _titleLabel.text = title;
        }

        public void setVisible(bool visible)
        {
            _sectionContainer.setDisplayVisible(visible);
        }

        public void build(string title, Texture2D icon, bool canToggleVisibility, VisualElement parent)
        {
            _sectionContainer                       = new VisualElement();
            _sectionContainer.style.backgroundColor = UIValues.uiSectionBkColor;
            _sectionContainer.style.flexDirection   = FlexDirection.Column;
            parent.Add(_sectionContainer);

            VisualElement headerContainer           = new VisualElement();
            headerContainer.style.flexDirection     = FlexDirection.Row;
            _sectionContainer.Add(headerContainer);

            _contentContainer = new VisualElement();
            _contentContainer.setDisplayVisible(_isContentVisible);
            _sectionContainer.Add(_contentContainer);

            float imgSize = UIValues.smallButtonSize;
            if (canToggleVisibility)
            {
                Button visToggle                = UI.createButton(_isContentVisible ? TexturePool.instance.itemArrowDown : TexturePool.instance.itemArrowRight, UI.ButtonStyle.Normal, headerContainer);
                visToggle.tooltip               = "Toggle visibility.";
                visToggle.style.backgroundColor = Color.white.createNewAlpha(0.0f);
                visToggle.style.width           = imgSize;
                visToggle.style.height          = imgSize;
                visToggle.style.minWidth        = imgSize;
                visToggle.style.minHeight       = imgSize;
                visToggle.clicked               += () =>
                {
                    _isContentVisible = !_isContentVisible;
                    _contentContainer.setDisplayVisible(_isContentVisible);
                    visToggle.style.backgroundImage = _isContentVisible ? TexturePool.instance.itemArrowDown : TexturePool.instance.itemArrowRight;
                };
            }

            if (icon != null)
            {
                VisualElement iconElem          = new VisualElement();
                iconElem.style.setBackgroundImage(icon, true);
                iconElem.style.width            = imgSize;
                iconElem.style.height           = imgSize;
                iconElem.style.marginTop        = 1.0f;
                iconElem.style.marginRight      = 2.0f;
                headerContainer.Add(iconElem);
            }

            _titleLabel                                 = new Label(title);
            _titleLabel.style.marginTop                 = 1.0f;
            _titleLabel.style.unityFontStyleAndWeight   = FontStyle.Bold;
            headerContainer.Add(_titleLabel);
        }
    }
}
#endif