#if UNITY_EDITOR
using UnityEngine;
using System;

namespace GSpawn
{
    [Serializable]
    public class PrefabPreview
    {
        [SerializeField]
        private GameObject  _prefabAsset;
        [SerializeField]
        private Texture2D   _texture;

        [SerializeField]
        private float       _yaw        = 0.0f;
        [SerializeField]
        private float       _pitch      = 0.0f;

        public GameObject   prefabAsset { get { return _prefabAsset; } }
        public Texture2D    texture 
        { 
            get 
            {
                // Note: This can happen when switching to play mode.
                if (_texture == null && _prefabAsset != null) generatePreview();
                return _texture; 
            } 
        }
        public float        yaw         { get { return _yaw; } set { _yaw = value; } }
        public float        pitch       { get { return _pitch; } set { _pitch = value; } }

        public void setPrefab(GameObject prefabAsset)
        {
            releaseTexture();

            _prefabAsset        = prefabAsset;
            if (_prefabAsset != null)
            {
                var prefabData  = PrefabDataDb.instance.getData(_prefabAsset);
                _texture        = PrefabPreviewFactory.instance.createPreviewTexture(prefabData);
                generatePreview();
            }
        }

        public void reset()
        {
            yaw     = 0.0f;
            pitch   = 0.0f;
            generatePreview();
        }

        public void regenerate()
        {
            releaseTexture();
            generatePreview();
        }
        
        public void rotate(Vector2 yawPitch)
        {
            if (_prefabAsset == null) return;

            var prefabData = PrefabDataDb.instance.getData(_prefabAsset);
            if (prefabData.hasVolume)
            {
                const float sensitivity = 1.0f;
                yaw     += -yawPitch.x * sensitivity;
                pitch   += -yawPitch.y * sensitivity;
                generatePreview();
            }
        }

        private void generatePreview()
        {
            if (_prefabAsset == null) return;

            var prefabData = PrefabDataDb.instance.getData(_prefabAsset);
            if (_texture == null) _texture = PrefabPreviewFactory.instance.createPreviewTexture(prefabData);

            var previewConfig   = new PrefabPreviewFactory.PreviewConfig();
            previewConfig.yaw   = yaw;
            previewConfig.pitch = pitch;
            PrefabPreviewFactory.instance.renderPreview(_texture, _prefabAsset, prefabData, previewConfig);
        }

        public void releaseTexture()
        {
            if (_texture != null)
            {
                Texture2D.DestroyImmediate(_texture, true);
                _texture = null;
            }
        }
    }
}
#endif