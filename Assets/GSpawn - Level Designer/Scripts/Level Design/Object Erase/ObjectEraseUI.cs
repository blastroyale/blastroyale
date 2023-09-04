#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectEraseUI : PluginUI
    {
        private ToolbarButton       _objectEraseCursorBtn;
        private ToolbarButton       _objectEraseBrush2DBtn;
        private ToolbarButton       _objectEraseBrush3DBtn;

        [SerializeField]
        private UISection           _eraseMaskSection;
        [SerializeField]
        private UISection           _eraseCursorSettingsSection;
        [SerializeField]
        private UISection           _eraseBrush2DSettingsSection;
        [SerializeField]
        private UISection           _eraseBrush3DSettingsSection;

        public static ObjectEraseUI instance    { get { return GSpawn.active.objectEraseUI; } }

        protected override void onRefresh()
        {
            refreshObjectEraseToolsButtons();
        }

        protected override void onBuild()
        {
            var toolbarContainer                    = new VisualElement();
            toolbarContainer.style.flexDirection    = FlexDirection.Row;
            contentContainer.Add(toolbarContainer);

            Toolbar toolbar         = UI.createToolSelectionToolbar(toolbarContainer);
            _objectEraseCursorBtn   = UI.createToolbarButton(TexturePool.instance.objectEraseCursor, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_objectEraseCursorBtn);
            _objectEraseCursorBtn.style.marginTop = 1.0f;
            _objectEraseCursorBtn.clicked += () => { ObjectErase.instance.activeToolId = ObjectEraseToolId.Cursor; };

            _objectEraseBrush2DBtn  = UI.createToolbarButton(TexturePool.instance.objectEraseBrush2D, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_objectEraseBrush2DBtn);
            _objectEraseBrush2DBtn.style.marginTop = 1.0f;
            _objectEraseBrush2DBtn.clicked += () => { ObjectErase.instance.activeToolId = ObjectEraseToolId.Brush2D; };

            _objectEraseBrush3DBtn  = UI.createToolbarButton(TexturePool.instance.objectEraseBrush3D, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_objectEraseBrush3DBtn);
            _objectEraseBrush3DBtn.style.marginTop = 1.0f;
            _objectEraseBrush3DBtn.clicked += () => { ObjectErase.instance.activeToolId = ObjectEraseToolId.Brush3D; };

            refreshObjectEraseToolsButtons();

            _eraseMaskSection.build("Erase Mask", null, true, contentContainer);
            ObjectErase.instance.eraseMask.buildUI(_eraseMaskSection.contentContainer, "");

            UI.createUISectionRowSeparator(contentContainer);
            _eraseCursorSettingsSection.build("Erase Cursor", TexturePool.instance.objectEraseCursor, true, contentContainer);
            ObjectErase.instance.eraseCursorSettings.buildUI(_eraseCursorSettingsSection.contentContainer);  

            UI.createUISectionRowSeparator(contentContainer);
            _eraseBrush2DSettingsSection.build("Erase Brush 2D", TexturePool.instance.objectEraseBrush2D, true, contentContainer);
            ObjectErase.instance.eraseBrush2DSettings.buildUI(_eraseBrush2DSettingsSection.contentContainer);

            UI.createUISectionRowSeparator(contentContainer);
            _eraseBrush3DSettingsSection.build("Erase Brush 3D", TexturePool.instance.objectEraseBrush3D, true, contentContainer);
            ObjectErase.instance.eraseBrush3DSettings.buildUI(_eraseBrush3DSettingsSection.contentContainer);    
        }

        private void refreshObjectEraseToolsButtons()
        {
            _objectEraseCursorBtn.tooltip = "Erase Cursor" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectEraseShortcutNames.eraseCursor, "Erase objects using the mouse cursor.");
            _objectEraseCursorBtn.style.backgroundColor = UIValues.inactiveButtonColor;

            _objectEraseBrush2DBtn.tooltip = "Erase Brush 2D" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectEraseShortcutNames.eraseBrush2D, "Erase objects using a 2D brush.");
            _objectEraseBrush2DBtn.style.backgroundColor = UIValues.inactiveButtonColor;

            _objectEraseBrush3DBtn.tooltip = "Erase Brush 3D" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectEraseShortcutNames.eraseBrush3D, "Erase objects using a 3D brush.");
            _objectEraseBrush3DBtn.style.backgroundColor = UIValues.inactiveButtonColor;

            if (ObjectErase.instance.activeToolId == ObjectEraseToolId.Cursor) _objectEraseCursorBtn.style.backgroundColor = UIValues.activeButtonColor;
            else if (ObjectErase.instance.activeToolId == ObjectEraseToolId.Brush2D) _objectEraseBrush2DBtn.style.backgroundColor = UIValues.activeButtonColor;
            else if (ObjectErase.instance.activeToolId == ObjectEraseToolId.Brush3D) _objectEraseBrush3DBtn.style.backgroundColor = UIValues.activeButtonColor;
        }

        protected override void onEnabled()
        {
            if (_eraseMaskSection == null)              _eraseMaskSection               = ScriptableObject.CreateInstance<UISection>();
            if (_eraseCursorSettingsSection == null)    _eraseCursorSettingsSection     = ScriptableObject.CreateInstance<UISection>();
            if (_eraseBrush2DSettingsSection == null)   _eraseBrush2DSettingsSection    = ScriptableObject.CreateInstance<UISection>();
            if (_eraseBrush3DSettingsSection == null)   _eraseBrush3DSettingsSection    = ScriptableObject.CreateInstance<UISection>();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_eraseMaskSection);
            ScriptableObjectEx.destroyImmediate(_eraseCursorSettingsSection);
            ScriptableObjectEx.destroyImmediate(_eraseBrush2DSettingsSection);
            ScriptableObjectEx.destroyImmediate(_eraseBrush3DSettingsSection);
        }
    }
}
#endif