#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class ModularWallsObjectSpawnUI : PluginUI
    {
        [SerializeField]
        private UISection _modularSnapSettingsSection;
        [SerializeField]
        private UISection _settingsSection;

        public static ModularWallsObjectSpawnUI instance { get { return GSpawn.active.modularWallsObjectSpawnUI; } }

        protected override void onBuild()
        {
            _settingsSection.build("Modular Snap", TexturePool.instance.modularSnapSpawn, true, contentContainer);
            ObjectSpawn.instance.modularWallObjectSpawn.modularSnapSettings.buildUI(_settingsSection.contentContainer);

            _settingsSection.build("Modular Walls", TexturePool.instance.modularWallSpawn, true, contentContainer);
            ObjectSpawn.instance.modularWallObjectSpawn.settings.buildUI(_settingsSection.contentContainer);
        }

        protected override void onRefresh()
        {
        }

        protected override void onEnabled()
        {
            if (_modularSnapSettingsSection == null) _modularSnapSettingsSection = UISection.CreateInstance<UISection>();
            if (_settingsSection == null) _settingsSection = UISection.CreateInstance<UISection>();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_modularSnapSettingsSection);
            ScriptableObjectEx.destroyImmediate(_settingsSection);
        }
    }
}
#endif