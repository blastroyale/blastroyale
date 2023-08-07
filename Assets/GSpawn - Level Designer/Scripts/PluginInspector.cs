#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    [CustomEditor(typeof(GSpawn))]
    public class PluginInspector : Editor
    {
        private VisualElement _rootElement;

        public override void OnInspectorGUI()
        {
        }

        public override VisualElement CreateInspectorGUI()
        {
            _rootElement                = new VisualElement();
            _rootElement.style.flexGrow = 1.0f;

            PluginInspectorUI.instance.build(_rootElement, this);
            return _rootElement;
        }
    }
}
#endif