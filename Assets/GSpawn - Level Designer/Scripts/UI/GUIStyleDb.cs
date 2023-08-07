#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    public class GUIStyleDb : Singleton<GUIStyleDb>
    {
        private GUIStyle _sceneViewInfoLabel    = null;
        private GUIStyle _uiInfoLabel           = null;

        public GUIStyle sceneViewInfoLabel
        {
            get
            {
                if (_sceneViewInfoLabel == null)
                {
                    _sceneViewInfoLabel                     = new GUIStyle("Tooltip");
                    _sceneViewInfoLabel.normal.textColor    = UIValues.isProSkin ? Color.white : Color.black;
                   // _sceneViewInfoLabel.fontStyle           = FontStyle.Bold;
                    _sceneViewInfoLabel.fontSize            = 12;
                }

                return _sceneViewInfoLabel;
            }
        }

        public GUIStyle uiInfoLabel
        {
            get
            {
                if (_uiInfoLabel == null)
                {
                    _uiInfoLabel            = new GUIStyle("Label");
                    _uiInfoLabel.fontStyle  = FontStyle.Bold;
                    _uiInfoLabel.wordWrap   = true;
                }

                return _uiInfoLabel;
            }
        }
    }
}
#endif