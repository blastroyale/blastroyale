#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GSpawn
{
    public class PrefabPreviewFactory
    {
        public struct PreviewConfig
        {
            public float yaw;
            public float pitch;

            public static readonly PreviewConfig    defaultConfig       = new PreviewConfig() { yaw = 0.0f, pitch = 0.0f };
        }

        private PrefabPreviewScene                  _previewScene       = new PrefabPreviewScene();
        private ObjectBounds.QueryConfig            _boundsQConfig      = new ObjectBounds.QueryConfig();
        private RenderTexture                       _renderTexture;

        public static int                           previewSize         { get { return 128; } }
        public static PrefabPreviewFactory          instance            { get { return GSpawn.active.prefabPreviewFactory; } }

        public PrefabPreviewFactory()
        {
            _boundsQConfig.objectTypes      = GameObjectType.All & (~(GameObjectType.Light | GameObjectType.ParticleSystem | GameObjectType.Empty | GameObjectType.Camera));
            _boundsQConfig.volumelessSize   = Vector3Ex.create(0.0f);
        }

        public void initialize()
        {
            _previewScene.initialize();
            createRenderTexture();
        }

        public void cleanup()
        {
            destroyRenderTexture();

            if (_previewScene != null)
                _previewScene.cleanup();
        }

        public Texture2D createPreviewTexture(PrefabData prefabData)
        {
            bool isLinear = QualitySettings.activeColorSpace != ColorSpace.Linear;
            if (prefabData.hasVolume) return new Texture2D(previewSize, previewSize, TextureFormat.ARGB32, false, isLinear);
            else
            {
                Texture2D previewIcon = getVolumlessPreviewIcon(prefabData);
                return new Texture2D(previewIcon.width, previewIcon.height, previewIcon.format, false, isLinear);
            }
        }

        public void renderPreview(Texture2D previewTexture, GameObject prefabAsset, PrefabData prefabData, PreviewConfig previewConfig)
        {
            if (!prefabData.hasVolume)
            {
                Graphics.CopyTexture(getVolumlessPreviewIcon(prefabData), 0, 0, previewTexture, 0, 0);
                return;
            }

            if (prefabData.hasMeshes || prefabData.hasSprites || prefabData.hasTerrains)
            {
                Transform camTransform              = _previewScene.camera.transform;
                RenderTexture oldRenderTexture      = RenderTexture.active;
                RenderTexture.active                = _previewScene.camera.targetTexture;

                camTransform.rotation               = Quaternion.identity;
                if (prefabData.hasMeshes || prefabData.hasTerrains) 
                    camTransform.rotation = Quaternion.AngleAxis(-135.0f, Vector3.up) * Quaternion.AngleAxis(22.0f, Vector3.right);

                GameObject previewObject            = _previewScene.instantiatePrefab(prefabAsset);
                Transform previewObjectTransform    = previewObject.transform;
                previewObjectTransform.position     = Vector3.zero;
                previewObjectTransform.rotation     = Quaternion.AngleAxis(previewConfig.pitch, camTransform.right) * Quaternion.AngleAxis(previewConfig.yaw, Vector3.up);
                previewObjectTransform.localScale   = prefabAsset.transform.lossyScale;

                // Note: For some reason, prefabs that use LOD groups, can sometimes disappear when 
                //       rendering their previews (e.g. when pressing the reset preview button in UI).
                //       So we will disable the lod group and disable all children except for the first
                //       one which is usually LOD0.
                var lodGroup = previewObject.getLODGroup();
                if (lodGroup != null)
                {
                    int numChildren = previewObjectTransform.childCount;
                    lodGroup.enabled = false;
                    for (int i = 1; i < numChildren; ++i)
                    {
                        // Note: Only disable mesh and skinned mesh renderers.
                        if (previewObject.getMeshRenderer() != null || previewObject.getSkinnedMeshRenderer() != null)
                            previewObjectTransform.GetChild(i).gameObject.SetActive(false);
                    }
                }

                var oldAmbMode              = RenderSettings.ambientMode;
                var oldAmbLight             = RenderSettings.ambientLight;
                //var oldSkyboxMaterial       = RenderSettings.skybox;
                var oldFogEnabled           = RenderSettings.fog;
                RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.1f, 0.0f);
                RenderSettings.fog          = false;

                // Note: Currently disabled as it seems that when restoring it, the settings are not applied
                //       (needs clicking on the field in the LightingWindow). Therefore scenes that use a skybox
                //       will get a bit darker after generating prefab previews.
                //RenderSettings.skybox       = null;   // Set skybox material to null. Otherwise, the previews appear washed out.

                OBB previewOBB              = ObjectBounds.calcHierarchyWorldOBB(previewObject, _boundsQConfig);
                Sphere previewSphere        = new Sphere(previewOBB);
                float offsetFromCamera      = _previewScene.camera.calcFrustumDistance(previewSphere.radius * 2.0f);
                camTransform.position       = previewSphere.center - camTransform.forward * (offsetFromCamera + _previewScene.camera.nearClipPlane + 0.1f);
         
                _previewScene.mainLight.transform.forward  = camTransform.forward;
                _previewScene.mainLight.transform.rotation *= Quaternion.AngleAxis(-45.0f, _previewScene.mainLight.transform.right);

                bool mightBeDecal = prefabData.hasMeshes && previewOBB.size.anyZero(1e-5f);
                if (!mightBeDecal)
                {
                    // Note: When rendering trees, we need to set the alpha of the background to 1.0 and
                    //       set it back to 0 as a post process.
                    bool isTree = previewObject.hierarchyHasTrees(false, false);
                    if (isTree) _previewScene.camera.backgroundColor = Color.black;
                    else _previewScene.camera.backgroundColor = Color.black.createNewAlpha(0.0f);

#if GSPAWN_HDRP
                    if (_previewScene.hdCameraData != null)
                        _previewScene.hdCameraData.backgroundColorHDR = _previewScene.camera.backgroundColor;
#endif

                    _previewScene.camera.clearFlags = CameraClearFlags.SolidColor;
                    _previewScene.camera.Render();

                    if (!isTree)
                    {
                        previewTexture.ReadPixels(new Rect(0, 0, previewSize, previewSize), 0, 0);
                        previewTexture.Apply();
                    }
                    else
                    {
                        previewTexture.ReadPixels(new Rect(0, 0, previewSize, previewSize), 0, 0);
                        Vector4 opaqueBkColor = Color.black;
                        Color[] pixels = previewTexture.GetPixels();
                        for (int row = 0; row < previewTexture.height; ++row)
                        {
                            for (int col = 0; col < previewTexture.width; ++col)
                            {
                                int pixelIndex = row * previewTexture.width + col;
                                Vector4 colorVec = pixels[pixelIndex];

                                float distance = (colorVec - opaqueBkColor).magnitude;
                                if (distance < 1e-5f) pixels[pixelIndex] = pixels[pixelIndex].createNewAlpha(0.0f);
                            }
                        }

                        previewTexture.SetPixels(pixels);
                        previewTexture.Apply();
                    }
                }
                else
                {
                    Color decalBkColor = ColorEx.create(82, 82, 82, 255);
                    _previewScene.camera.backgroundColor = decalBkColor;
                    _previewScene.camera.Render();

#if GSPAWN_HDRP
                    if (_previewScene.hdCameraData != null)
                        _previewScene.hdCameraData.backgroundColorHDR = _previewScene.camera.backgroundColor;
#endif

                    previewTexture.ReadPixels(new Rect(0, 0, previewSize, previewSize), 0, 0);

                    Vector4 decalBkColorVec = decalBkColor;
                    Color[] pixels = previewTexture.GetPixels();
                    for (int row = 0; row < previewTexture.height; ++row)
                    {
                        for (int col = 0; col < previewTexture.width; ++col)
                        {
                            int pixelIndex = row * previewTexture.width + col;
                            Vector4 colorVec = pixels[pixelIndex];

                            float distance = (colorVec - decalBkColorVec).magnitude;
                            if (distance < 1e-5f) pixels[pixelIndex] = pixels[pixelIndex].createNewAlpha(0.0f);
                        }
                    }

                    previewTexture.SetPixels(pixels);
                    previewTexture.Apply();
                }

                GameObject.DestroyImmediate(previewObject);
                RenderTexture.active        = oldRenderTexture;
                RenderSettings.ambientMode  = oldAmbMode;
                RenderSettings.ambientLight = oldAmbLight;
                RenderSettings.fog          = oldFogEnabled;
                //RenderSettings.skybox       = oldSkyboxMaterial;
            }
        }

        private Texture2D getVolumlessPreviewIcon(PrefabData prefabData)
        {
            if (prefabData.hasLights)           return TexturePool.instance.lightGizmo;
            if (prefabData.hasParticleSystems)  return TexturePool.instance.particleSystemGizmo;
            if (prefabData.hasCameras)          return TexturePool.instance.cameraGizmo;
            return TexturePool.instance.questionMark;
        }

        private void createRenderTexture()
        {
            destroyRenderTexture();
            if (_previewScene.camera == null) return;
     
            RenderTextureFormat textureFormat = _previewScene.camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            if (PlayerSettings.colorSpace == ColorSpace.Gamma) _renderTexture = new RenderTexture(previewSize, previewSize, 32, textureFormat, RenderTextureReadWrite.Default);
            else _renderTexture = new RenderTexture(previewSize, previewSize, 32, textureFormat, RenderTextureReadWrite.Default);    
            
            _renderTexture.hideFlags = HideFlags.HideAndDontSave;
            _previewScene.camera.targetTexture = _renderTexture;
        }

        private void destroyRenderTexture()
        {
            if (_renderTexture != null)
            {
                if (_previewScene != null) _previewScene.camera.targetTexture = null;

                RenderTexture.DestroyImmediate(_renderTexture);
                _renderTexture = null;
            }
        }
    }
}
#endif