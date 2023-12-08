using System;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace Best.TLSSecurity.Editor.Utils
{
    public class PasswordInputPopup : EditorWindow
    {
        public VisualTreeAsset passwordInputPopup;

        private Action<string> onPopupClosed;

        public static void Show(Action<string> onClosed)
        {
            try
            {
            PasswordInputPopup wnd = GetWindow<PasswordInputPopup>();
            wnd.titleContent = new GUIContent("Password for decrypt");
            wnd.onPopupClosed = onClosed;
#if UNITY_2019_4_OR_NEWER
            wnd.ShowModal();
#else
                wnd.Show();
#endif
            }
            catch
            {
                onClosed(string.Empty);
            }
        }

        public void OnEnable()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = this.passwordInputPopup; //AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorHelper.GetEditorFolder() + "/Utils/PasswordInput/PasswordInputPopup.uxml");
            VisualElement tree = visualTree.CloneTree();
            root.Add(tree);

#if UNITY_2019_3_OR_NEWER
            tree.Q<Button>("OKButton").clicked += OnOkButtonClicked;
#else
            tree.Q<Button>("OKButton").RegisterCallback<MouseUpEvent>(OnOkButtonClicked);
#endif

#if UNITY_2019_3_OR_NEWER
            tree.Q<Button>("CancelButton").clicked += OnCancelButtonClicked;
#else
            tree.Q<Button>("CancelButton").RegisterCallback<MouseUpEvent>(OnCancelButtonClicked);
#endif
        }

        public void OnDestroy()
        {
            if (this.onPopupClosed != null)
                this.onPopupClosed(string.Empty);
        }

        private void OnOkButtonClicked(
#if UNITY_2019_3_OR_NEWER
#else
            MouseUpEvent evt
#endif
            )
        {
            if (this.onPopupClosed != null)
                this.onPopupClosed(this.rootVisualElement.Q<TextField>("PasswordField").text);
            this.onPopupClosed = null;
            this.Close();
        }

        private void OnCancelButtonClicked(
#if UNITY_2019_3_OR_NEWER
#else
            MouseUpEvent evt
#endif
            )
        {
            if (this.onPopupClosed != null)
                this.onPopupClosed(string.Empty);
            this.onPopupClosed = null;
            this.Close();
        }
    }
}
