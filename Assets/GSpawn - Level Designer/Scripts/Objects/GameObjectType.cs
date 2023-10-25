#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [Flags]
    public enum GameObjectType
    {
        None            = 0,
        Mesh            = 1,
        Terrain         = 2,
        Sprite          = 4,
        Camera          = 8,
        Light           = 16,
        ParticleSystem  = 32,
        Empty           = 64,
        All             = ~0
    }

    public static class GameObjectTypeEx
    {
        private static int                              _numTypes;
        private static List<GameObjectType>             _allObjectTypes;

        static GameObjectTypeEx()
        {
            var allTypes    = Enum.GetValues(typeof(GameObjectType));
            _numTypes       = allTypes.Length;

            _allObjectTypes = new List<GameObjectType>(_numTypes);
            foreach (var type in allTypes) _allObjectTypes.Add((GameObjectType)type);
        }

        public static int                               numTypes        { get { return _numTypes; } }
        public static GameObjectType[]                  allObjectTypes  { get { return _allObjectTypes.ToArray(); } }

        public static bool is3DObject(this GameObjectType objectType)
        {
            return objectType != GameObjectType.Sprite;
        }

        public static bool is2DObject(this GameObjectType objectType)
        {
            return objectType == GameObjectType.Sprite;
        }

        public static bool hasVolume(this GameObjectType objectType)
        {
            return objectType != GameObjectType.Terrain && objectType != GameObjectType.Mesh && objectType != GameObjectType.Sprite;
        }
    }
}
#endif