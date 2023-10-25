#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class CurveObjectSpawnSettingsProfile : Profile
    {
        [SerializeField]
        private CurveObjectSpawnSettings    _settings;

        public CurveObjectSpawnSettings     settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = ScriptableObject.CreateInstance<CurveObjectSpawnSettings>();
                    AssetDbEx.addObjectToAsset(_settings, this);
                }

                return _settings;
            }
        }

        private void OnDestroy()
        {
            AssetDbEx.removeObjectFromAsset(_settings, this);
            ScriptableObjectEx.destroyImmediate(_settings);
        }
    }
}
#endif