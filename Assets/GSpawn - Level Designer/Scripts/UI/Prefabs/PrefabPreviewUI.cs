#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using UnityEditor;

namespace GSpawn
{
    public class PrefabPreviewUI
    {
        private bool                    _rotatingPreview;
        private VisualElement           _previewImage;
        private Label                   _prefabNameLabel;
        private Toolbar                 _bottomToolbar;
        private ToolbarButton           _resetPreviewButton;
        private ToolbarButton           _pingButton;             

        public bool                     rotatingPreview                 { get { return _rotatingPreview; } }
        public VisualElement            previewImage                    { get { return _previewImage; } }
        public Label                    prefabNameLabel                 { get { return _prefabNameLabel; } }
        public Toolbar                  bottomToolbar                   { get { return _bottomToolbar; } }
        public ToolbarButton            resetPreviewButton              { get { return _resetPreviewButton; } }
        public ToolbarButton            pingButton                      { get { return _pingButton; } }
        public Action<MouseMoveEvent>   onRotatePreview                 { get; set; }

        public void initialize(VisualElement parent, Vector2 imageSize, Action onCreatedResetPreviewButton = null)
        {
            _previewImage                   = new VisualElement();
            _previewImage.style.flexWrap    = Wrap.NoWrap;
            _previewImage.style.width       = imageSize.x;
            _previewImage.style.height      = imageSize.y;
            parent.Add(_previewImage);

            _previewImage.RegisterCallback<MouseDownEvent>(p => { if (FixedShortcuts.ui_BeginPreviewRotationOnClick(p)) { _rotatingPreview = true; _previewImage.CaptureMouse(); } });
            _previewImage.RegisterCallback<MouseUpEvent>(p => { _rotatingPreview = false; _previewImage.ReleaseMouse(); });
            _previewImage.RegisterCallback<MouseMoveEvent>
            (p =>
            {
                if (_rotatingPreview)
                {
                    if (onRotatePreview != null) onRotatePreview(p);
                    EditorWindow focusedWnd = EditorWindow.focusedWindow;
                    if (focusedWnd != null) focusedWnd.Repaint();
                }
            });

            _prefabNameLabel                    = new Label();
            _prefabNameLabel.style.overflow     = Overflow.Hidden;
            _previewImage.Add(_prefabNameLabel);

            _bottomToolbar                      = new Toolbar();
            _bottomToolbar.style.flexWrap       = Wrap.NoWrap;
            _bottomToolbar.style.marginRight    = -1.0f;          // Note: Needed to actually clamp to the preview edge. Otherwise the preview background shows through.
            parent.Add(_bottomToolbar);

            _resetPreviewButton = UI.createSmallResetPrefabPreviewToolbarButton(_bottomToolbar);
            if (onCreatedResetPreviewButton != null) onCreatedResetPreviewButton();
            UI.createFlexGrow(_bottomToolbar);

            _pingButton = UI.createSmallPrefabAssetPingToolbarButton(_bottomToolbar);
        }
    }
}
#endif