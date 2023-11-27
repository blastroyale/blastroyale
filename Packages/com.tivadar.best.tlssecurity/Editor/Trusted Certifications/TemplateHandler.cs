using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Best.TLSSecurity.CSV;
using Best.TLSSecurity.Databases.Shared;
using Best.TLSSecurity.Databases.X509;
using Best.TLSSecurity.Editor.Utils;
using Best.HTTP;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;
using Best.HTTP.Shared;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace Best.TLSSecurity.Editor
{
    public sealed class TemplateHandler
    {
        VisualElement templateRoot;
        TemplateBinding templateBinding;
        SerializedObject serializedTemplateBinding;
        X509Database x509Database;

        ListView certificatesView;
        List<X509Metadata> metadatas;

        ToolbarSearchField searchField;

        List<X509Metadata> filtered;
        CertificationModel[] models = new CertificationModel[0];
        SerializedObject[] serializedObjects = new SerializedObject[0];

        VisualTreeAsset itemTemplate;

        public TemplateHandler(VisualTreeAsset itemTemplate, VisualElement templateRoot, TemplateBinding templateBinding, X509Database x509Database)
        {
            this.itemTemplate = itemTemplate;
            this.templateRoot = templateRoot;
            this.templateBinding = templateBinding;
            this.x509Database = x509Database;

            this.serializedTemplateBinding = new SerializedObject(templateBinding);
            this.templateRoot.Bind(this.serializedTemplateBinding);

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

            //this.itemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorHelper.GetEditorFolder() + "/UXML/CertificationItemTemplate.uxml");
            this.filtered = this.metadatas = this.x509Database.MetadataService.Metadatas;

            this.templateRoot.Q<TextField>("URL").viewDataKey = this.templateBinding.header + "_URL";

#if UNITY_2019_3_OR_NEWER
            this.templateRoot.Q<Button>("Download").clicked += OnDownloadClicked;
#else
            this.templateRoot.Q<Button>("Download").RegisterCallback<MouseUpEvent>(evt => OnDownloadClicked());
#endif

#if UNITY_2019_3_OR_NEWER
            this.templateRoot.Q<Button>("Clear").clicked += OnClearClicked;
#else
            this.templateRoot.Q<Button>("Clear").RegisterCallback<MouseUpEvent>(evt => OnClearClicked());
#endif

#if UNITY_2019_3_OR_NEWER
            this.templateRoot.Q<Button>("AddCustom").clicked += OnAddCustomClicked;
#else
            this.templateRoot.Q<Button>("AddCustom").RegisterCallback<MouseUpEvent>(evt => OnAddCustomClicked());
#endif

#if UNITY_2019_3_OR_NEWER
            this.templateRoot.Q<Button>("DeleteSelected").clicked += OnDeleteSelected;
#else
            this.templateRoot.Q<Button>("DeleteSelected").RegisterCallback<MouseUpEvent>(evt => OnDeleteSelected());
#endif

#if UNITY_2019_3_OR_NEWER
            this.templateRoot.Q<Button>("ResetURL").clicked += () => this.templateBinding.URL = this.templateBinding.originalURL;
#else
            this.templateRoot.Q<Button>("ResetURL").RegisterCallback<MouseUpEvent>(evt => this.templateBinding.URL = this.templateBinding.originalURL);
#endif

            this.templateRoot.Query<ToolbarButton>("HelpButton").ForEach(b => {
#if UNITY_2019_3_OR_NEWER
                b.clicked += () => { Application.OpenURL(this.templateBinding.HelpURL); };
#else
                b.RegisterCallback<MouseUpEvent>(evt => { Application.OpenURL(this.templateBinding.HelpURL); });
#endif
            });

            this.certificatesView = this.templateRoot.Q<ListView>("ListView");
            this.certificatesView.makeItem = MakeItem;
            this.certificatesView.bindItem = BindItem;
#if UNITY_2022_2_OR_NEWER
            this.certificatesView.itemsChosen += (selectedItems) => {
                var certificates = this.x509Database.FromMetadatas(selectedItems.Cast<X509Metadata>());
                foreach (var cert in certificates)
                    Debug.Log(cert);
            };
#elif UNITY_2020_1_OR_NEWER
            this.certificatesView.onItemsChosen += (selectedItems) => {
                var certificates = this.x509Database.FromMetadatas(selectedItems.Cast<X509Metadata>());
                foreach (var cert in certificates)
                    Debug.Log(cert);
            };
#else
            this.certificatesView.onItemChosen += (selectedItem) => {
                var certificates = this.x509Database.FromMetadatas(new X509Metadata[] { selectedItem as X509Metadata });
                foreach (var cert in certificates)
                    Debug.Log(cert);
            };
#endif

            this.searchField = this.templateRoot.Q<ToolbarSearchField>("Search");
            this.searchField.RegisterValueChangedCallback(OnSearchChanged);
            this.searchField.viewDataKey = this.templateBinding.header + "_search";
            RunFilter();
        }

        VisualElement MakeItem()
        {
            return this.itemTemplate.CloneTree();
        }

        void BindItem(VisualElement element, int index)
        {
            if (this.filtered.Count <= index)
            {
                element.Unbind();
                element.Query().Children<Label>().ForEach(l => l.text = string.Empty);
                return;
            }

            if (this.models.Length <= index)
                Array.Resize<CertificationModel>(ref this.models, index + 1);

            if (this.models[index] == null)
            {
                this.models[index] = ScriptableObject.CreateInstance<CertificationModel>();
                this.models[index].idx = index + 1;
                this.models[index].subject = this.filtered[index].GetSubjectStr();
                this.models[index].issuer = this.filtered[index].GetIssuerStr();
                this.models[index].isUserAdded = this.filtered[index].IsUserAdded ? "✔" : "➖";
                this.models[index].isLocked = this.filtered[index].IsLocked ? "✔" : "➖";
            }

            if (this.serializedObjects.Length <= index)
                Array.Resize<SerializedObject>(ref this.serializedObjects, index + 1);

            if (this.serializedObjects[index] == null)
                this.serializedObjects[index] = new SerializedObject(this.models[index]);

            element.Bind(this.serializedObjects[index]);
        }

        private void RefreshCertificateView()
        {
            if (this.certificatesView.itemsSource == null)
                this.certificatesView.itemsSource = this.filtered;
#if UNITY_2021_2_OR_NEWER
            this.certificatesView.Rebuild();
#else
            this.certificatesView.Refresh();
#endif

            int min = int.MaxValue;
            int max = 0;
            int count = 0;
            int sum = 0;
            foreach (var filtered in this.filtered)
            {
                if (filtered.IsDeleted)
                    continue;

                if (filtered.Length > max)
                    max = filtered.Length;
                if (filtered.Length < min)
                    min = filtered.Length;
                sum += filtered.Length;
                count++;
            }

            this.templateBinding.count = count;

            if (count > 0)
                this.templateBinding.certificateStats = $"Certifications: {count} | Certificate size stats: Min: {min}, Max: {max}, Sum: {sum}, Avg: {sum / count}";
            else
                this.templateBinding.certificateStats = string.Empty;
        }

        void CopyBack()
        {
            this.x509Database.CompactAndSave();

            // Copy bytes back to the resource folder

            string resourcesFolder = EditorHelper.GetPluginResourcesFolder();
            string certificationStoreFolder = Path.Combine(HTTPManager.GetRootSaveFolder(), SecurityOptions.FolderAndFileOptions.FolderName, SecurityOptions.FolderAndFileOptions.DatabaseFolderName);

            // a) metadata
            string fromFileName = Path.ChangeExtension(Path.Combine(certificationStoreFolder, this.x509Database.Name), this.templateBinding.MetadataExtension);
            string toFileName = Path.ChangeExtension(Path.Combine(resourcesFolder, this.x509Database.Name + "_" + this.templateBinding.MetadataExtension), "bytes");
            File.Copy(fromFileName, toFileName, true);

            // b) DB
            fromFileName = Path.ChangeExtension(Path.Combine(certificationStoreFolder, this.x509Database.Name), this.templateBinding.DatabaseExtension);
            toFileName = Path.ChangeExtension(Path.Combine(resourcesFolder, this.x509Database.Name + "_" + this.templateBinding.DatabaseExtension), "bytes");
            File.Copy(fromFileName, toFileName, true);

            // c) Hash
            fromFileName = Path.ChangeExtension(Path.Combine(certificationStoreFolder, this.x509Database.Name), this.templateBinding.HashExtension);
            toFileName = Path.ChangeExtension(Path.Combine(resourcesFolder, this.x509Database.Name + "_" + this.templateBinding.HashExtension), "bytes");
            File.Copy(fromFileName, toFileName, true);
        }

        private void OnDownloadClicked()
        {
            if (this.templateBinding.clearBeforeDownload)
                this.x509Database.Clear(this.templateBinding.keepCustomCertificates);

            //HTTPManager.TlsClientFactory = HTTPManager.DefaultTlsClientFactory;

            // 1.) Download CSV
            var request = new HTTPRequest(new Uri(this.templateBinding.URL), (req, resp) =>
            {
                switch (req.State)
                {
                    case HTTPRequestStates.Finished:
                        if (resp.IsSuccess)
                        {
                            try
                            {
                                EditorUtility.DisplayProgressBar("Parsing CSV database", "", 0);

                                // 2.) Parse CSV
                                var csvDB = CSVReader.Read(new MemoryStream(resp.Data));

                                // 3.) Remove certificates from the DB
                                //this.x509Database.Clear(this.setup.keepCustomCertificates);

                                var parser = new HTTP.SecureProtocol.Org.BouncyCastle.X509.X509CertificateParser();
                                var rawCertificates = csvDB.Columns[csvDB.Columns.Count - 1].Values;

                                // 4.) Add certificates to the DB
                                for (int i = 0; i < rawCertificates.Count; ++i)
                                {
                                    string info = $"Loading certificate {i}/{rawCertificates.Count}";
                                    EditorUtility.DisplayProgressBar("Certificate parsing", info, i / (float)rawCertificates.Count);

                                    var value = rawCertificates[i];
                                    if (string.IsNullOrEmpty(value.Value))
                                        continue;

                                    var bytes = System.Text.Encoding.UTF8.GetBytes(value.Value);

                                    if (bytes == null || bytes.Length <= 2)
                                        continue;

                                    int length = bytes.Length - 2;

                                    try
                                    {
                                        var cert = parser.ReadCertificate(new MemoryStream(bytes, 1, length));

                                        if (AlreadyinDB(cert))
                                            continue;

                                        // Expired, or will expire in the near future
                                        if (!cert.IsValid(DateTime.Now + TimeSpan.FromDays(30)))
                                            continue;

                                        MetadataFlags flags = MetadataFlags.None;

                                        string certSubject = cert.SubjectDN.ToString();
                                        if (certSubject == "C=US,O=DigiCert Inc,OU=www.digicert.com,CN=DigiCert Global Root CA" ||
                                            certSubject == "C=US,O=DigiCert Inc,CN=DigiCert SHA2 Secure Server CA")
                                        {
                                            flags = MetadataFlags.Locked;
                                        }

                                        x509Database.Add(cert, flags);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (HTTPManager.Logger.IsDiagnostic)
                                        {
                                            var comment = csvDB.Columns[csvDB.Columns.Count - 2].Values[i].Value;
                                            HTTPManager.Logger.Exception(nameof(TemplateHandler), info + " " + value.Value + " " + comment, ex, req.Context);
                                        }
                                    }
                                }

                                // 5.) Save DB
                                x509Database.Save();

                                // 6.) Copy bytes back to the resource folder

                                CopyBack();

                                this.templateBinding.status = "Downloading & processing finished";

                                RunFilter();
                            }
                            catch (Exception ex)
                            {
                                this.templateBinding.status = ex.Message;
                            }
                            finally
                            {
                                EditorUtility.ClearProgressBar();
                            }
                        }
                        else
                        {
                            this.templateBinding.status = resp.Message;
                        }
                        break;

                    case HTTPRequestStates.Aborted:
                        this.templateBinding.status = "Aborted";
                        break;

                    case HTTPRequestStates.ConnectionTimedOut:
                        this.templateBinding.status = "Connection Timed Out";
                        break;
                    case HTTPRequestStates.TimedOut:
                        this.templateBinding.status = "Download Timed Out";
                        break;

                    case HTTPRequestStates.Error:
                        this.templateBinding.status = "Internal Error: " + req.Exception.Message;
                        if (req.Exception.InnerException != null)
                        {
                            this.templateBinding.status += " " + req.Exception.InnerException.Message;
                        }
                        break;
                }
            });

            request.DownloadSettings.ContentStreamMaxBuffered = 10 * 1024 * 1024;

            request.DownloadSettings.OnDownloadProgress += (req, down, len) =>
            {
                this.templateBinding.status = $"Downloading... ({down:N0}/{len:N0})";
            };
            request.Send();

            this.templateBinding.status = "Request sent";
        }

        private bool AlreadyinDB(X509Certificate certificate)
        {
            var encodedCertificate = certificate.GetEncoded();

            var foundCertificates = x509Database.FindBySubjectDN(certificate.SubjectDN);

            if (foundCertificates != null)
                foreach (var cert in foundCertificates)
                    if (Arrays.AreEqual(encodedCertificate, cert.GetEncoded()))
                        return true;

            return false;
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            DoSearch(evt.newValue);
        }

        private void RunFilter()
        {
            var searchTerm = EditorPrefs.GetString(searchField.viewDataKey, string.Empty);
            // in some cases setting value triggered an onChange event, but not always.
            // So, here we call DoSearch too, calling it twice in some cases.
            DoSearch(searchField.value = searchTerm);
        }

        private void DoSearch(string searchFor)
        {
            if (!string.IsNullOrEmpty(searchFor) && searchFor.Length >= this.templateBinding.MinLengthToSearch)
            {
                this.filtered = (from meta in this.metadatas where !meta.IsDeleted && meta.Subject.ToString().IndexOf(searchFor, StringComparison.OrdinalIgnoreCase) >= 0 select meta).ToList();
                Array.Clear(this.models, 0, this.models.Length);
                Array.Clear(this.serializedObjects, 0, this.serializedObjects.Length);

                this.certificatesView.itemsSource = this.filtered;
            }
            else
            {
                this.filtered = (from meta in this.metadatas where !meta.IsDeleted select meta).ToList();
                Array.Clear(this.models, 0, this.models.Length);
                Array.Clear(this.serializedObjects, 0, this.serializedObjects.Length);

                this.certificatesView.itemsSource = this.filtered;
            }

            RefreshCertificateView();
            EditorPrefs.SetString(this.templateBinding.header + "_search", searchFor);
        }

        private void OnAddCustomClicked()
        {
            var selectedFile = EditorUtility.OpenFilePanelWithFilters("Select certification", "", new string[] { "Certification files", "cer,pem,p7b", "All files", "*" });
            if (string.IsNullOrEmpty(selectedFile))
                return;

            X509CertificateParser parser = new X509CertificateParser();

            IList<X509Certificate> certs = null;
            try
            {
                certs = parser.ReadCertificates(File.OpenRead(selectedFile));
                foreach (X509Certificate cert in certs)
                    x509Database.Add(cert, MetadataFlags.UserAdded);

                x509Database.Save();

                CopyBack();

                this.templateBinding.status = $"{certs.Count} certification(s) loaded from '{Path.GetFileName(selectedFile)}'";
            }
            catch (Exception ex)
            {
                this.templateBinding.status = ex.Message;
            }

            RunFilter();
            this.certificatesView.ScrollToItem(this.filtered.Count - 1);
        }

        private void OnClearClicked()
        {
            int deletedCount = this.x509Database.Clear(this.templateBinding.keepCustomCertificates);
            CopyBack();

            Array.Clear(this.models, 0, this.models.Length);
            Array.Clear(this.serializedObjects, 0, this.serializedObjects.Length);
            RunFilter();

            this.templateBinding.status = $"Clear removed {deletedCount} certificates!";
        }

        private void OnDeleteSelected()
        {
            if (this.certificatesView.selectedItem == null)
                return;

            if (EditorUtility.DisplayDialog("Delete Certificates", "Are you sure you want to delete the selected certificates?", "Yes", "No"
#if UNITY_2019_3_OR_NEWER
                , DialogOptOutDecisionType.ForThisMachine, "BestHTTP_TLSSecurityAddon_DeleteSelectedCertificates"
#endif
                ))
            {
                var selectedToRemove =
#if UNITY_2020_1_OR_NEWER
                    this.certificatesView.selectedItems.Cast<X509Metadata>();
#else
                    new List<X509Metadata>()
                    {
                        this.certificatesView.selectedItem as X509Metadata
                    };
#endif
                int deleted = this.x509Database.Delete(selectedToRemove);

                CopyBack();
                Array.Clear(this.models, 0, this.models.Length);
                Array.Clear(this.serializedObjects, 0, this.serializedObjects.Length);

                RunFilter();

                this.templateBinding.status = $"Removed {deleted} certificates!";

#if UNITY_2020_1_OR_NEWER
                this.certificatesView.ClearSelection();
#else
                this.certificatesView.selectedIndex = -1;
#endif
            }
        }
    }
}
