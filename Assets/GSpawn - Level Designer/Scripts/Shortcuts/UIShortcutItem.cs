#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class UIShortcutItem : ListViewItem<Shortcut>
    {
        private Button              _useDefaultButton;
        private Label               _hotkeyLabel;
        private TextField           _hotkeyAssignField;
        private VisualElement       _warningIcon;

        public override string      displayName         { get { return data.shortcutName; } }
        public override PluginGuid  guid                { get { return getItemId(data); } }

        public static float         shortcutNameWidth   { get { return 370.0f; } }

        public static PluginGuid getItemId(Shortcut shortcut) 
        { 
            return shortcut.guid; 
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onBuildUIBeforeDisplayName()
        {
            style.height = 20.0f;

            _useDefaultButton                   = UI.createIconButton(TexturePool.instance.refresh, UIValues.smallIconSize, this);
            _useDefaultButton.style.marginTop   = 2.0f;
            Add(_useDefaultButton);

            // Note: _useDefaultButton has variable state, but 'applyVariableStates'
            //       is called in 'onBuildUIAfterDisplayName'.
        }

        protected override void onBuildUIAfterDisplayName()
        {
            _displayNameLabel.style.width = shortcutNameWidth;

            _hotkeyLabel                    = new Label();
            _hotkeyLabel.style.alignSelf    = Align.Center;
            _hotkeyLabel.style.marginRight  = UIValues.listItemRightMargin;
            _hotkeyLabel.text               = data.keyCombo.ToString();
            _hotkeyLabel.style.flexGrow     = 1.0f;
            Add(_hotkeyLabel);

            _hotkeyAssignField                      = new TextField();
            _hotkeyAssignField.style.alignSelf      = Align.Center;
            _hotkeyAssignField.style.marginRight    = UIValues.listItemRightMargin;
            _hotkeyAssignField.setDisplayVisible(false);
            _hotkeyAssignField.style.width          = 50.0f;
            Add(_hotkeyAssignField);

            _warningIcon                    = new VisualElement();
            _warningIcon.style.setBackgroundImage(TexturePool.instance.warning, true);
            _warningIcon.setDisplayVisible(data.hasConflicts);
            _warningIcon.style.marginTop    = 2.0f;
            _warningIcon.tooltip            = data.getConflictsTooltip();
            Add(_warningIcon);

            applyVariableStates();
            registerCallbacks();
        }

        private void beginHotkeyAssign()
        {
            _hotkeyLabel.setDisplayVisible(false);
            _hotkeyAssignField.setDisplayVisible(true);
            _hotkeyAssignField.value = string.Empty;

            _hotkeyAssignField.focsuEx();
            _hotkeyAssignField.SelectAll();
            _warningIcon.setDisplayVisible(false);
        }

        private void cancelHotkeyAssign()
        {
            _hotkeyLabel.setDisplayVisible(true);
            _hotkeyAssignField.setDisplayVisible(false);
            _warningIcon.setDisplayVisible(data.hasConflicts);
        }

        private void endHotkeyAssign(KeyDownEvent e)
        {
            _hotkeyLabel.setDisplayVisible(true);
            _hotkeyAssignField.setDisplayVisible(false);

            if (e.keyCode != KeyCode.None && e.keyCode != KeyCode.Escape)
            {
                KeyCombo.State comboState   = new KeyCombo.State();
                comboState.cmd              = e.commandKey;
                comboState.ctrl             = e.ctrlKey;
                comboState.shift            = e.shiftKey;
                comboState.key              = e.keyCode;

                UndoEx.record(ShortcutProfileDb.instance.activeProfile);
                ShortcutProfileDb.instance.activeProfile.setShortcutKeyComboState(data, comboState);
                ShortcutProfileDb.instance.activeProfile.detectConflicts(true);

                _hotkeyLabel.text           = data.keyCombo.ToString();
            }
        }

        private void applyVariableStates()
        {
            _hotkeyLabel.text = data.keyCombo.ToString();

            if (data.canChangeFromUI)
            {
                _useDefaultButton.style.unityBackgroundImageTintColor   = data.canChangeFromUI ? Color.white : UIValues.disabledColor;
                _useDefaultButton.tooltip                               = "Reset to default.";

                _hotkeyLabel.style.color                                = UIValues.listItemTextColor ;
            }
            else 
            {
                _useDefaultButton.tooltip                   = string.Empty;
                _useDefaultButton.style.backgroundColor     = Color.white.createNewAlpha(0.0f);

                _hotkeyLabel.style.color                    = UIValues.importantInfoLabelColor;
                _hotkeyLabel.style.unityFontStyleAndWeight  = FontStyle.Bold;
                _hotkeyLabel.tooltip                        = "This shortcut is locked and can't be changed.";
            }

            _warningIcon.setDisplayVisible(data.hasConflicts);
            _warningIcon.tooltip = data.getConflictsTooltip();
        }

        private void registerCallbacks()
        {
            RegisterCallback<MouseDownEvent>(p =>
            {
                if (data.canChangeFromUI && FixedShortcuts.ui_BeginHotkeyAssignmentOnDblClick(p)) beginHotkeyAssign();
            });
            RegisterCallback<FocusOutEvent>(p => { cancelHotkeyAssign(); });

            _hotkeyAssignField.RegisterCallback<KeyDownEvent>(p =>
            {
                // Note: For some reason, keyboard events are sometimes still sent even after
                //       the field has been hidden.
                if (_hotkeyAssignField.isDisplayHidden()) return;

                if (p.keyCode == KeyCode.Escape || p.keyCode == KeyCode.Return) cancelHotkeyAssign();
                else
                {
                    if (p.keyCode != KeyCode.LeftCommand && p.keyCode != KeyCode.RightCommand &&
                        p.keyCode != KeyCode.LeftControl && p.keyCode != KeyCode.RightControl &&
                        p.keyCode != KeyCode.LeftShift && p.keyCode != KeyCode.RightShift &&
                        p.keyCode != KeyCode.LeftAlt && p.keyCode != KeyCode.RightAlt) endHotkeyAssign(p);
                }
            });

            _useDefaultButton.clicked += () =>
            {
                ShortcutProfileDb.instance.activeProfile.useDefault(data, true, true);
                _hotkeyLabel.text = data.keyCombo.ToString();
            };
        }
    }
}
#endif