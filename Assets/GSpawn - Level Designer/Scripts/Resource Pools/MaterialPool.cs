#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class MaterialPool : Singleton<MaterialPool>
    {
        private Material _xzGrid;
        private Material _xzGridCoordSystemLine;
        private Material _simpleDiffuse;
        private Material _simpleDiffuseTex;

        public Material xzGrid
        {
            get
            {
                if (_xzGrid == null) _xzGrid = new Material(ShaderPool.instance.xzGrid);
                return _xzGrid;
            }
        }
        public Material xzGridCoordSystemLine
        {
            get
            {
                if (_xzGridCoordSystemLine == null) _xzGridCoordSystemLine = new Material(ShaderPool.instance.xzGridCoordSystemLine);
                return _xzGridCoordSystemLine;
            }
        }
        public Material simpleDiffuse
        {
            get
            {
                if (_simpleDiffuse == null) _simpleDiffuse = new Material(ShaderPool.instance.simpleDiffuse);
                return _simpleDiffuse;
            }
        }
        public Material simpleDiffuseTex
        {
            get
            {
                if (_simpleDiffuseTex == null) _simpleDiffuseTex = new Material(ShaderPool.instance.simpleDiffuseTex);
                return _simpleDiffuseTex;
            }
        }
    }
}
#endif