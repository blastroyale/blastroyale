#if UNITY_EDITOR
using UnityEditor.UIElements;

namespace GSpawn
{
    public class PluginInspectorUI : PluginUI
    {
        private ToolbarButton           _spawnModeBtn;
        private ToolbarButton           _objectSelectAndManipBtn;
        private ToolbarButton           _objectEraseBtn;

        public static PluginInspectorUI instance                { get { return GSpawn.active.inspectorUI; } }

        protected override void onRefresh()
        {
            refreshLevelDesignToolButtons();
            updateVisibility();
        }

        protected override void onBuild()
        {
            createTopToolbar();
            ObjectSpawnUI.instance.build(contentContainer, targetEditor);
            ObjectSelectionUI.instance.build(contentContainer, targetEditor);
            ObjectEraseUI.instance.build(contentContainer, targetEditor);
            updateVisibility();
        }

        private void createTopToolbar()
        {
            Toolbar toolbar                 = UI.createStylizedToolbar(contentContainer);
            toolbar.style.height            = UIValues.mediumToolbarButtonSize + 3.0f;

            _spawnModeBtn                   = UI.createToolbarButton(TexturePool.instance.earthHammer, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_spawnModeBtn);
            _spawnModeBtn.style.marginTop   = 1.0f;
            _spawnModeBtn.clicked           += () => { GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectSpawn; SceneViewEx.focus(); };

            _objectSelectAndManipBtn                    = UI.createToolbarButton(TexturePool.instance.earthHand, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_objectSelectAndManipBtn);
            _objectSelectAndManipBtn.style.marginTop    = 1.0f;
            _objectSelectAndManipBtn.clicked            += () => { GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectSelection; SceneViewEx.focus(); };

            _objectEraseBtn                 = UI.createToolbarButton(TexturePool.instance.earthDelete, UI.ButtonStyle.Push, UIValues.mediumToolbarButtonSize, toolbar);
            UI.useDefaultMargins(_objectEraseBtn);
            _objectEraseBtn.style.marginTop = 1.0f;
            _objectEraseBtn.clicked         += () => { GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectErase; SceneViewEx.focus(); };

            refreshLevelDesignToolButtons();
        }

        private void refreshLevelDesignToolButtons()
        {
            _spawnModeBtn.tooltip                   = "Object Spawn" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(GlobalShortcutNames.objectSpawn);
            _spawnModeBtn.style.backgroundColor     = UIValues.inactiveButtonColor;

            _objectSelectAndManipBtn.tooltip                = "Object Select & Manipulate" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(GlobalShortcutNames.objectSelection);
            _objectSelectAndManipBtn.style.backgroundColor  = UIValues.inactiveButtonColor;

            _objectEraseBtn.tooltip                 = "Object Erase" + ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(GlobalShortcutNames.objectErase);
            _objectEraseBtn.style.backgroundColor   = UIValues.inactiveButtonColor;

            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn) _spawnModeBtn.style.backgroundColor = UIValues.activeButtonColor;
            else if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection) _objectSelectAndManipBtn.style.backgroundColor = UIValues.activeButtonColor;
            else if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectErase) _objectEraseBtn.style.backgroundColor = UIValues.activeButtonColor;
        }

        private void updateVisibility()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            ObjectSpawnUI.instance.setVisible(toolId == LevelDesignToolId.ObjectSpawn);
            ObjectSelectionUI.instance.setVisible(toolId == LevelDesignToolId.ObjectSelection);
            ObjectEraseUI.instance.setVisible(toolId == LevelDesignToolId.ObjectErase);
        }
    }
}
#endif