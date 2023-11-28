#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class ShaderPool : Singleton<ShaderPool>
    {
        private Shader _xzGrid;
        private Shader _xzGridCoordSystemLine;
        private Shader _simpleDiffuse;
        private Shader _simpleDiffuseTex;

        public Shader xzGrid 
        { 
            get 
            {
                if (_xzGrid == null) _xzGrid = Shader.Find(GSpawn.pluginName + "/XZGrid");
                return _xzGrid;
            } 
        }
        public Shader xzGridCoordSystemLine
        {
            get
            {
                if (_xzGridCoordSystemLine == null) _xzGridCoordSystemLine = Shader.Find(GSpawn.pluginName + "/XZGridCoordSystemLine");
                return _xzGridCoordSystemLine;
            }
        }
        public Shader simpleDiffuse
        {
            get
            {
                if (_simpleDiffuse == null) _simpleDiffuse = Shader.Find(GSpawn.pluginName + "/SimpleDiffuse");
                return _simpleDiffuse;
            }
        }
        public Shader simpleDiffuseTex
        {
            get
            {
                if (_simpleDiffuseTex == null) _simpleDiffuseTex = Shader.Find(GSpawn.pluginName + "/SimpleDiffuseTex");
                return _simpleDiffuseTex;
            }
        }
    }
}
#endif