#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public class GameObjectDataDb : Singleton<GameObjectDataDb>
    {
        private Dictionary<Mesh, ObjectToObjectSnapData>        _meshToSnapData     = new Dictionary<Mesh, ObjectToObjectSnapData>();
        private Dictionary<Sprite, ObjectToObjectSnapData>      _spriteToSnapData   = new Dictionary<Sprite, ObjectToObjectSnapData>();
        private Dictionary<GameObject, GameObjectType>          _objectTypeMap      = new Dictionary<GameObject, GameObjectType>();

        public void refresh()
        {
            _objectTypeMap.Clear();
            _meshToSnapData.Clear();
            _spriteToSnapData.Clear();
        }

        public ObjectToObjectSnapData getSnapData(GameObject gameObject)
        {
            if (gameObject == null) return null;

            Mesh mesh = gameObject.getMesh();
            if (mesh != null)
            {
                if (_meshToSnapData.ContainsKey(mesh)) return _meshToSnapData[mesh];
                var snapData = ObjectToObjectSnapData.create(gameObject);
                if (snapData == null) return null;

                _meshToSnapData.Add(mesh, snapData);
                return snapData;
            }

            Sprite sprite = gameObject.getSprite();
            if (sprite != null)
            {
                if (_spriteToSnapData.ContainsKey(sprite)) return _spriteToSnapData[sprite];

                var snapData = ObjectToObjectSnapData.create(gameObject);
                if (snapData == null) return null;

                _spriteToSnapData.Add(sprite, snapData);
                return snapData;
            }

            return null;
        }

        public GameObjectType getGameObjectType(GameObject gameObject)
        {
            /*Terrain terrain = gameObject.getTerrain();
            if (terrain != null) return GameObjectType.Terrain;

            Mesh mesh = gameObject.getMesh();
            if (mesh != null) return GameObjectType.Mesh;

            Sprite sprite = gameObject.getSprite();
            if (sprite != null) return GameObjectType.Sprite;

            Light light = gameObject.GetComponent<Light>();
            if (light != null) return GameObjectType.Light;

            ParticleSystem particleSystem = gameObject.GetComponent<ParticleSystem>();
            if (particleSystem != null) return GameObjectType.ParticleSystem;

            Camera camera = gameObject.GetComponent<Camera>();
            if (camera != null) return GameObjectType.Camera;

            return GameObjectType.Empty;*/

            GameObjectType type = GameObjectType.None;
            if (_objectTypeMap.TryGetValue(gameObject, out type)) return type;
            else
            {
                Terrain terrain = gameObject.getTerrain();
                if (terrain != null)
                {
                    _objectTypeMap.Add(gameObject, GameObjectType.Terrain);
                    return GameObjectType.Terrain;
                }

                Mesh mesh = gameObject.getMesh();
                if (mesh != null)
                {
                    _objectTypeMap.Add(gameObject, GameObjectType.Mesh);
                    return GameObjectType.Mesh;
                }

                Sprite sprite = gameObject.getSprite();
                if (sprite != null)
                {
                    _objectTypeMap.Add(gameObject, GameObjectType.Sprite);
                    return GameObjectType.Sprite;
                }

                Light light = gameObject.getLight();
                if (light != null)
                {
                    _objectTypeMap.Add(gameObject, GameObjectType.Light);
                    return GameObjectType.Light;
                }

                ParticleSystem particleSystem = gameObject.getParticleSystem();
                if (particleSystem != null)
                {
                    _objectTypeMap.Add(gameObject, GameObjectType.ParticleSystem);
                    return GameObjectType.ParticleSystem;
                }

                Camera camera = gameObject.getCamera();
                if (camera != null)
                {
                    _objectTypeMap.Add(gameObject, GameObjectType.Camera);
                    return GameObjectType.Camera;
                }

                _objectTypeMap.Add(gameObject, GameObjectType.Empty);
                return GameObjectType.Empty;
            }
        }
    }
}
#endif