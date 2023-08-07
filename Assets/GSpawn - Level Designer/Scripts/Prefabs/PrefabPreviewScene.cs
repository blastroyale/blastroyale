#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
#if GSPAWN_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace GSpawn
{
    public class PrefabPreviewScene
    {
        private bool                        _initialized;
        private Scene                       _scene;
        private List<GameObject>            _gameObjects    = new List<GameObject>();
        private Camera                      _camera;
        private Light                       _mainLight;
#if GSPAWN_HDRP
        private HDAdditionalCameraData      _hdCameraData;
#endif

        public Light                    mainLight       { get { return _mainLight; } }
        public Camera                   camera          { get { return _camera; } }
        #if GSPAWN_HDRP
        public HDAdditionalCameraData   hdCameraData    { get { return _hdCameraData; } }
        #endif
        public Scene                    scene           { get { return _scene; } }

        public void initialize()
        {
            if (!_initialized)
            {
                _scene = EditorSceneManager.NewPreviewScene();
                if (!_scene.IsValid())
                    throw new UnityException("Prefab Preview Scene could not be created.");

                var cameraGO                = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
                addGameObject(cameraGO);

                #if GSPAWN_HDRP
                bool hdrp = GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset;
                #endif

                _camera                     = cameraGO.getCamera();
                _camera.cameraType          = CameraType.Preview;
                _camera.orthographic        = false;
                _camera.enabled             = true;
                _camera.clearFlags          = CameraClearFlags.SolidColor;
                _camera.fieldOfView         = 45.0f;
                _camera.farClipPlane        = 10000.0f;
                _camera.nearClipPlane       = 0.001f;
                _camera.renderingPath       = RenderingPath.Forward;
                _camera.useOcclusionCulling = false;
                _camera.scene               = _scene;
                _camera.allowHDR            = false;   // Set this to false to allow for transparent backgrounds

#if GSPAWN_HDRP
                if (hdrp)
                {
                    _hdCameraData = _camera.gameObject.AddComponent<HDAdditionalCameraData>();
                    _hdCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                }
#endif

                var lightGO                 = EditorUtility.CreateGameObjectWithHideFlags("Preview Light 0", HideFlags.HideAndDontSave, typeof(Light));
                addGameObject(lightGO);

                _mainLight                  = lightGO.getLight();
                _mainLight.intensity        = 1.0f;
                _mainLight.color            = Color.white;
                _mainLight.type             = LightType.Directional;

                #if GSPAWN_HDRP
                if (hdrp)
                {
                    _mainLight.gameObject.AddComponent<HDAdditionalLightData>();
                }
                #endif

                _initialized                = true;
            }
        }

        public void addGameObject(GameObject gameObject)
        {
            if (_gameObjects.Contains(gameObject))
                return;

            SceneManager.MoveGameObjectToScene(gameObject, _scene);
            _gameObjects.Add(gameObject);
        }

        public GameObject instantiatePrefab(GameObject prefab)
        {
            var gameObject = prefab.instantiatePrefab(_scene);
            _gameObjects.Add(gameObject);

            return gameObject;
        }

        public void cleanup()
        {
            EditorSceneManager.ClosePreviewScene(_scene);

            foreach (var gameObject in _gameObjects)
                Object.DestroyImmediate(gameObject);

            _gameObjects.Clear();
            _initialized = false;
        }
    }
}
#endif