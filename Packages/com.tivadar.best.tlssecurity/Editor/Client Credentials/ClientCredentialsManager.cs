using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Best.TLSSecurity.Databases.ClientCredentials;
using Best.TLSSecurity.Editor.Utils;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Crypto;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.OpenSsl;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Pkcs;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;
using Best.HTTP.Shared;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace Best.TLSSecurity.Editor
{
    public sealed class UnityMetadataWrapper : ScriptableObject
    {
        public int idx;
        public string targetDomain;
        public string authority;        
    }

    public sealed class ClientCredentialsSetupBinding : ScriptableObject
    {
        public string header = "Client Certificates";
        public string status;

        public int count;
        public string certificateStats;

        public string HelpURL = "https://bestdocshub.pages.dev/TLS%20Security/intermediate-topics/CertificationManagerWindow/#client-certificates";
    }

    public sealed class ClientCredentialsManager : IPasswordFinder
    {
        public VisualTreeAsset template;

        private VisualElement rootVisual;
        private ClientCredentialDatabase database;

        ClientCredentialsSetupBinding setup;
        SerializedObject serializedSetup;

        private ListView credentialsView;
        private UnityMetadataWrapper[] MetadataWrappers = new UnityMetadataWrapper[0];
        SerializedObject[] serializedObjects = new SerializedObject[0];

        public static string SelectClientCredentialFile()
        {
            return EditorUtility.OpenFilePanelWithFilters("Select Client Credential", "", new string[] { "PKCS#12 Files", "p12,pfx", "Certification Files", "crt,cert,cer,pem,der", "All files", "*" });
        }

        public ClientCredentialsManager(VisualTreeAsset itemTemplate, VisualElement credentialsRoot, ClientCredentialDatabase credentialDatabase)
        {
            this.template = itemTemplate;
            this.rootVisual = credentialsRoot;
            this.database = credentialDatabase;

            this.setup = ScriptableObject.CreateInstance<ClientCredentialsSetupBinding>();
            this.serializedSetup = new SerializedObject(setup);
            this.rootVisual.Bind(this.serializedSetup);

            if (TLSSecurity.IsSetupCalled)
            {
                OnTLSecuritySetupFinished();
            }
            else
            {
                TLSSecurity.OnSetupFinished += OnTLSecuritySetupFinished;
                TLSSecurity.Setup();
            }
        }

        void OnTLSecuritySetupFinished()
        {
            TLSSecurity.OnSetupFinished -= OnTLSecuritySetupFinished;

            //this.template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorHelper.GetEditorFolder() + "/UXML/ClientCredentialItemTemplate.uxml");

            //this.rootVisual.Q<ToolbarButton>("Add").clicked += OnAddClientCredentialClicked;
#if UNITY_2019_3_OR_NEWER
            this.rootVisual.Q<ToolbarButton>("AddTargetDomain").clicked += OnAddClientCredentialForDomainClicked;
#else
            this.rootVisual.Q<ToolbarButton>("AddTargetDomain").RegisterCallback<MouseUpEvent>(OnAddClientCredentialForDomainClicked);
#endif

#if UNITY_2019_3_OR_NEWER
            this.rootVisual.Q<ToolbarButton>("DeleteSelected").clicked += OnDeleteSelectedClientCredentialsClicked;
#else
            this.rootVisual.Q<ToolbarButton>("DeleteSelected").RegisterCallback<MouseUpEvent>(OnDeleteSelectedClientCredentialsClicked);
#endif

            this.rootVisual.Query<ToolbarButton>("HelpButton").ForEach(b => {
#if UNITY_2019_3_OR_NEWER
                b.clicked += () => { Application.OpenURL(this.setup.HelpURL); };
#else
                b.RegisterCallback<MouseUpEvent>(evt => Application.OpenURL(this.setup.HelpURL));
#endif
            });

            this.credentialsView = this.rootVisual.Q<ListView>("ListView");
            this.credentialsView.makeItem = MakeItem;
            this.credentialsView.bindItem = BindItem;
#if UNITY_2022_2_OR_NEWER
            this.credentialsView.itemsChosen += (obj) => {
                var selectedMetadatas = obj.Cast<ClientCredentialMetadata>();
                foreach (var meta in selectedMetadatas)
                    Debug.Log(meta);
            };
#elif UNITY_2020_1_OR_NEWER
            this.credentialsView.onItemsChosen += (obj) => {
                var selectedMetadatas = obj.Cast<ClientCredentialMetadata>();
                foreach (var meta in selectedMetadatas)
                    Debug.Log(meta);
            };
#else
            this.credentialsView.onItemChosen += (obj) =>
            {
                var selectedMetadata = obj as ClientCredentialMetadata;
                if (selectedMetadata != null)
                    Debug.Log(selectedMetadata);
            };
#endif

            RefreshView();
        }

        private VisualElement MakeItem()
        {
            return this.template.CloneTree();
        }

        private void BindItem(VisualElement element, int index)
        {
            if (this.MetadataWrappers.Length <= index)
                Array.Resize<UnityMetadataWrapper>(ref this.MetadataWrappers, index + 1);

            if (this.MetadataWrappers[index] == null)
            {
                this.MetadataWrappers[index] = ScriptableObject.CreateInstance<UnityMetadataWrapper>();
                this.MetadataWrappers[index].idx = index + 1;
                this.MetadataWrappers[index].targetDomain = this.database.MetadataService.Metadatas[index].TargetDomain;
                this.MetadataWrappers[index].authority = this.database.MetadataService.Metadatas[index].GetAuthorityStr();
            }

            if (this.serializedObjects.Length <= index)
                Array.Resize<SerializedObject>(ref this.serializedObjects, index + 1);

            if (this.serializedObjects[index] == null)
                this.serializedObjects[index] = new SerializedObject(this.MetadataWrappers[index]);

            element.Bind(this.serializedObjects[index]);
        }

        private void RefreshView()
        {
            this.credentialsView.itemsSource = (from m in this.database.MetadataService.Metadatas where !m.IsDeleted select m).ToList();

            var stats = this.database.MetadataService.GetStats();
            this.setup.count = stats.count;

            if (stats.count > 0)
                this.setup.certificateStats = $"Certificate size stats: Min: {stats.min}, Max: {stats.max}, Sum: {stats.sum}, Avg: {stats.sum / stats.count}";
            else
                this.setup.certificateStats = string.Empty;

#if UNITY_2021_2_OR_NEWER
            this.credentialsView.Rebuild();
#else
            this.credentialsView.Refresh();
#endif
        }

        private void OnAddClientCredentialClicked()
        {
            var selectedFile = SelectClientCredentialFile();
            LoadFrom(selectedFile, null);
        }

        private void LoadFrom(string selectedFile, string targetDomain)
        {
            if (string.IsNullOrEmpty(selectedFile))
                return;

            var extension = Path.GetExtension(selectedFile);
            switch (extension.ToLowerInvariant())
            {
                case ".p12":
                case ".pfx":
                    ReadFromPKCS12(selectedFile, targetDomain);
                    break;

                case ".crt":
                case ".cert":
                case ".cer":
                case ".pem":
                case ".der":
                    ReadPEM(selectedFile, targetDomain);
                    break;
            }
        }

        private void OnAddClientCredentialForDomainClicked(
#if UNITY_2019_3_OR_NEWER
#else
            MouseUpEvent evt
#endif
            )
        {
            DomainAndFileSelectorPopup.Show((targetDomain, selectedFile) =>
            {
                LoadFrom(selectedFile, targetDomain);
            });
        }

        private void OnDeleteSelectedClientCredentialsClicked(
#if UNITY_2019_3_OR_NEWER
#else
            MouseUpEvent evt
#endif
            )
        {
            if (this.credentialsView.selectedItem == null)
                return;

            if (EditorUtility.DisplayDialog("Delete Credentials", "Are you sure you want to delete the selected credentials?", "Yes", "No"
#if UNITY_2019_3_OR_NEWER
                ,DialogOptOutDecisionType.ForThisMachine, "BestHTTP_TLSSecurityAddon_DeleteSelectedCredentials"
#endif
                ))
            {
                List<ClientCredentialMetadata> selectedToRemove =
#if UNITY_2020_1_OR_NEWER
                    this.credentialsView.selectedItems.Cast<ClientCredentialMetadata>().ToList();
#else
                    new List<ClientCredentialMetadata>()
                    {
                        this.credentialsView.selectedItem as ClientCredentialMetadata
                    };
#endif
                this.database.Delete(selectedToRemove);

                CopyBack();
                Array.Clear(this.MetadataWrappers, 0, this.MetadataWrappers.Length);
                Array.Clear(this.serializedObjects, 0, this.serializedObjects.Length);
                RefreshView();

                this.setup.status = $"Removed {selectedToRemove.Count} credentials!";

#if UNITY_2020_1_OR_NEWER
                this.credentialsView.ClearSelection();
#else
                this.credentialsView.selectedIndex = -1;
#endif
            }
        }

        void CopyBack()
        {
            this.database.CompactAndSave();

            // Copy bytes back to the resource folder

            string resourcesFolder = EditorHelper.GetPluginResourcesFolder();
            string certificationStoreFolder = Path.Combine(HTTPManager.GetRootSaveFolder(), SecurityOptions.FolderAndFileOptions.FolderName, SecurityOptions.FolderAndFileOptions.DatabaseFolderName);

            // a) metadata
            string fromFileName = Path.ChangeExtension(Path.Combine(certificationStoreFolder, this.database.Name), SecurityOptions.FolderAndFileOptions.MetadataExtension);
            string toFileName = Path.ChangeExtension(Path.Combine(resourcesFolder, this.database.Name + "_" + SecurityOptions.FolderAndFileOptions.MetadataExtension), "bytes");
            if (File.Exists(fromFileName))
                File.Copy(fromFileName, toFileName, true);

            // b) DB
            fromFileName = Path.ChangeExtension(Path.Combine(certificationStoreFolder, this.database.Name), SecurityOptions.FolderAndFileOptions.DatabaseExtension);
            toFileName = Path.ChangeExtension(Path.Combine(resourcesFolder, this.database.Name + "_" + SecurityOptions.FolderAndFileOptions.DatabaseExtension), "bytes");
            if (File.Exists(fromFileName))
                File.Copy(fromFileName, toFileName, true);

            // c) Hash
            fromFileName = Path.ChangeExtension(Path.Combine(certificationStoreFolder, this.database.Name), SecurityOptions.FolderAndFileOptions.HashExtension);
            toFileName = Path.ChangeExtension(Path.Combine(resourcesFolder, this.database.Name + "_" + SecurityOptions.FolderAndFileOptions.HashExtension), "bytes");
            if (File.Exists(fromFileName))
                File.Copy(fromFileName, toFileName, true);
        }

        private void ReadFromPKCS12(string selectedFile, string targetDomain)
        {
            PasswordInputPopup.Show(passwd =>
            {
                if (string.IsNullOrEmpty(passwd))
                    return;

                var store = new Pkcs12StoreBuilder().Build();
                store.Load(File.OpenRead(selectedFile), passwd.ToCharArray());

                foreach (string alias in store.Aliases)
                {
                    var certificate = new Certificate((from cert in store.GetCertificateChain(alias) select new BestHTTPTlsCertificate(cert.Certificate.CertificateStructure)).ToArray());
                    var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(store.GetKey(alias).Key);

                    this.database.Add(targetDomain, new ClientCredential { Certificate = certificate, KeyInfo = privateKeyInfo });
                }

                this.database.Save();
                CopyBack();
                RefreshView();

                this.setup.status = "Client credentials loaded from " + Path.GetFileName(selectedFile);
            });
        }

        private void ReadPEM(string selectedFile, string targetDomain)
        {
            List<X509Certificate> certificates = new List<X509Certificate>();
            AsymmetricKeyParameter privateKey = null;

            using (var stream = File.OpenText(selectedFile))
            {
                var reader = new HTTP.SecureProtocol.Org.BouncyCastle.OpenSsl.PemReader(stream, this);
                object obj = null;
                while ((obj = reader.ReadObject()) != null)
                {
                    X509Certificate certificate = obj as X509Certificate;
                    if (certificate != null)
                        certificates.Add(certificate);
                    else
                    {
                        AsymmetricKeyParameter key = obj as AsymmetricKeyParameter;
                        if (key != null)
                            privateKey = key;
                    }
                }
            }

            if (certificates.Count == 0 || privateKey == null)
            {
                EditorUtility.DisplayDialog("Alert", "This file contains no corresponding private key!", "Ok");
            }
            else
            {
                var certificate = new Certificate((from cert in certificates select new BestHTTPTlsCertificate(cert.CertificateStructure)).ToArray());
                var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKey);

                this.database.Add(targetDomain, new ClientCredential { Certificate = certificate, KeyInfo = privateKeyInfo });
            }

            this.database.Save();
            CopyBack();
            RefreshView();

            this.setup.status = "Client credentials loaded from " + Path.GetFileName(selectedFile);
        }

        public char[] GetPassword()
        {
            string result = null;
            using (AutoResetEvent are = new AutoResetEvent(false))
            {
                PasswordInputPopup.Show(passwd =>
                {
                    result = passwd;
                    are.Set();
                });

                are.WaitOne();

                return result.ToCharArray();
            }
        }
    }
}
