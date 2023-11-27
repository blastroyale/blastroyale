using System;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace Best.TLSSecurity.Editor.Utils
{
    public class DomainAndFileSelectorPopup : EditorWindow
    {
        public VisualTreeAsset domainAndFileSelectorPopup;
        Action<string, string> popupCallback;
        TextField domainTextField;
        string selectedFile;

        public static void Show(Action<string, string> callback)
        {
            DomainAndFileSelectorPopup wnd = GetWindow<DomainAndFileSelectorPopup>();
            wnd.titleContent = new GUIContent("Domain and File Selector");
            wnd.popupCallback = callback;
            wnd.maxSize = new Vector2(1024, 70);
            wnd.minSize = new Vector2(512, 70);
#if UNITY_2019_4_OR_NEWER
            wnd.ShowModal();
#else
            wnd.Show();
#endif
        }

        public void OnEnable()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = this.domainAndFileSelectorPopup; //AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorHelper.GetEditorFolder() + "/Utils/DomainAndFileSelector/DomainAndFileSelectorPopup.uxml");
            VisualElement labelFromUXML = visualTree.CloneTree();
            root.Add(labelFromUXML);

            this.domainTextField = root.Q<TextField>("DomainInput");
#if UNITY_2019_3_OR_NEWER
            root.Q<Button>("SelectButton").clicked += OnSelectCertificateButtonClicked;
#else
            root.Q<Button>("SelectButton").RegisterCallback<MouseUpEvent>(OnSelectCertificateButtonClicked);
#endif

#if UNITY_2019_3_OR_NEWER
            root.Q<Button>("OKButton").clicked += OnOkButtonClicked;
#else
            root.Q<Button>("OKButton").RegisterCallback<MouseUpEvent>(OnOkButtonClicked);
#endif

#if UNITY_2019_3_OR_NEWER
            root.Q<Button>("CancelButton").clicked += OnCancelButtonClicked;
#else
            root.Q<Button>("CancelButton").RegisterCallback<MouseUpEvent>(OnCancelButtonClicked);
#endif
        }

        private void OnSelectCertificateButtonClicked(
#if UNITY_2019_3_OR_NEWER
#else
            MouseUpEvent evt
#endif
            )
        {
            this.selectedFile = ClientCredentialsManager.SelectClientCredentialFile();
            this.rootVisualElement.Q<Label>("SelectedFile").text = this.selectedFile;
        }

        public void OnDestroy()
        {
            if (this.popupCallback != null)
                this.popupCallback(string.Empty, string.Empty);
        }

        private void OnOkButtonClicked(
#if UNITY_2019_3_OR_NEWER
#else
            MouseUpEvent evt
#endif
            )
        {
            if (this.popupCallback != null)
                this.popupCallback(this.domainTextField.text, this.selectedFile);
            this.popupCallback = null;
            Close();
        }

        private void OnCancelButtonClicked(
#if UNITY_2019_3_OR_NEWER
#else
            MouseUpEvent evt
#endif
            )
        {
            if (this.popupCallback != null)
                this.popupCallback(string.Empty, string.Empty);
            this.popupCallback = null;
            Close();
        }
    }
}
