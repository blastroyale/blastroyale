#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Extensions;
using Best.HTTP.Shared.Logger;
using Best.HTTP.Shared.PlatformSupport.FileSystem;
using Best.HTTP.Shared.PlatformSupport.Threading;
using Best.HTTP.Shared.TLS;
using Best.TLSSecurity.Databases.ClientCredentials;
using Best.TLSSecurity.Databases.X509;
using Best.TLSSecurity.OCSP;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Best.TLSSecurity
{
    /// <summary>
    /// A static class responsible for handling TLS security operations including setup, unloading databases, and waiting for the setup to finish.
    /// </summary>
    /// <remarks>
    /// The class manages Root CAs, Intermediate Certificates, Client Certificates, and provides functionality for the setup process. 
    /// </remarks>
    public static class TLSSecurity
    {
        /// <summary>
        /// Database of Root CAs that trusted by the client.
        /// </summary>
        public static X509Database trustedRootCertificates { get; private set; }

        /// <summary>
        /// Database of Intermediate Certificates that trusted by the client.
        /// </summary>
        public static X509Database trustedIntermediateCertificates { get; private set; }

        /// <summary>
        /// Database of Client Certificates that's available to send when the server requests it.
        /// </summary>
        public static ClientCredentialDatabase ClientCredentials { get; private set; }

        /// <summary>
        /// True if Setup already called.
        /// </summary>
        public static bool IsSetupCalled { get; private set; }

        /// <summary>
        /// True if setup process finished successfully.
        /// </summary>
        public static bool IsSetupFinished { get { return _isSetupFinished; } private set { _isSetupFinished = value; } }
        private volatile static bool _isSetupFinished;

        /// <summary>
        /// Called when all databases are in place and loaded.
        /// </summary>
        public static Action OnSetupFinished;

        /// <summary>
        /// Initiates the setup process of the TLS Security package.
        /// </summary>
        public static void Setup()
        {
            if (IsSetupCalled)
                return;
            IsSetupCalled = true;

            HTTPManager.Logger.Information(nameof(TLSSecurity), $"{nameof(Setup)}");

            HTTPManager.PerHostSettings.Get("*")
                .TLSSettings.BouncyCastleSettings.TlsClientFactory = TLSSecurity.ExtendedTlsClientFactory;

#if UNITY_EDITOR
            // Direct compare&copy DB files in the editor.

            string certificationStoreFolder = Path.Combine(HTTPManager.GetRootSaveFolder(), SecurityOptions.FolderAndFileOptions.FolderName, SecurityOptions.FolderAndFileOptions.DatabaseFolderName);

            if (!HTTPManager.IOService.DirectoryExists(certificationStoreFolder))
                HTTPManager.IOService.DirectoryCreate(certificationStoreFolder);

            CompareAndCopy(certificationStoreFolder, SecurityOptions.TrustedRootsOptions.Name);
            CompareAndCopy(certificationStoreFolder, SecurityOptions.TrustedIntermediatesOptions.Name);

            CreateClientCredentialsIfNotFound();
            CompareAndCopy(certificationStoreFolder, SecurityOptions.ClientCredentialsOptions.Name);

            TLSSecurity.PostInstallSetupFinishedCallback();
#else
            var go = new UnityEngine.GameObject($"{nameof(Best.TLSSecurity.Install.UnpackDatabaseScript)} (Best.TLSSecurity)");
            go.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            go.AddComponent<Best.TLSSecurity.Install.UnpackDatabaseScript>();
#endif
        }

        /// <summary>
        /// Unloads all databases (certificates and OCSP cache).
        /// </summary>
        public static void UnloadDatabases()
        {
            HTTPManager.Logger.Information(nameof(TLSSecurity), $"{nameof(UnloadDatabases)}");

            IsSetupCalled = false;
            _isSetupFinished = false;

            try
            {
                TLSSecurity.trustedRootCertificates?.Dispose();
                TLSSecurity.trustedRootCertificates = null;

                TLSSecurity.trustedIntermediateCertificates?.Dispose();
                TLSSecurity.trustedIntermediateCertificates = null;

                TLSSecurity.ClientCredentials?.Dispose();
                TLSSecurity.ClientCredentials = null;

                OCSPCache.Unload();
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(TLSSecurity), $"{nameof(UnloadDatabases)}", ex);
            }

            HTTPManager.Logger.Information(nameof(TLSSecurity), $"{nameof(UnloadDatabases)} - unloaded!");
        }

        /// <summary>
        /// Called by the database unpacker. It loads the databases and calls OnSetupFinished.
        /// </summary>
        internal static void PostInstallSetupFinishedCallback()
        {
            // get the root cache folder on the main thread (UnityEngine.Application.persistentDataPath can be called only from there)
            // and load the databases from a thread
            string rootFolder = HTTPManager.GetRootSaveFolder();
            HTTPUpdateDelegator.CheckInstance();

            ThreadedRunner.RunShortLiving(() =>
            {
                try
                {
                    string certificationStoreFolder = Path.Combine(rootFolder, SecurityOptions.FolderAndFileOptions.FolderName, SecurityOptions.FolderAndFileOptions.DatabaseFolderName);

                    HTTPManager.Logger.Information(nameof(TLSSecurity), $"{nameof(PostInstallSetupFinishedCallback)} certificationStoreFolder: {certificationStoreFolder}");

                    if (TLSSecurity.trustedRootCertificates == null)
                    {
                        HTTPManager.Logger.Information(nameof(TLSSecurity), $"loading Root CAs ...");

                        var x509DatabaseIndexingService = new X509DatabaseIndexingService();

                        TLSSecurity.trustedRootCertificates = new X509Database(certificationStoreFolder,
                            SecurityOptions.TrustedRootsOptions,
                            x509DatabaseIndexingService,
                            new X509CertificateContentParser(),
                            new x509MetadataService(x509DatabaseIndexingService));
                        HTTPManager.Logger.Information(nameof(TLSSecurity), $"... done!");
                    }

                    if (TLSSecurity.trustedIntermediateCertificates == null)
                    {
                        HTTPManager.Logger.Information(nameof(TLSSecurity), $"loading Trusted Intermediates ...");

                        var x509DatabaseIndexingService = new X509DatabaseIndexingService();

                        TLSSecurity.trustedIntermediateCertificates = new X509Database(certificationStoreFolder,
                            SecurityOptions.TrustedIntermediatesOptions,
                            x509DatabaseIndexingService,
                            new X509CertificateContentParser(),
                            new x509MetadataService(x509DatabaseIndexingService));

                        HTTPManager.Logger.Information(nameof(TLSSecurity), $"... done!");
                    }

                    if (TLSSecurity.ClientCredentials == null)
                    {
                        HTTPManager.Logger.Information(nameof(TLSSecurity), $"loading Client Credentials ...");

                        var indexingService = new ClientCredentialIndexingService();

                        TLSSecurity.ClientCredentials = new ClientCredentialDatabase(certificationStoreFolder,
                            SecurityOptions.ClientCredentialsOptions,
                            indexingService,
                            new ClientCredentialParser(),
                            new ClientCredentialsMetadataService(indexingService));
                        HTTPManager.Logger.Information(nameof(TLSSecurity), $"... done!");
                    }

                    OCSPCache.Load(rootFolder);
                    HTTPManager.Logger.Information(nameof(TLSSecurity), $"OCSPCache loaded");

                    // call OnSetupFinished from the main thread
                    if (OnSetupFinished != null)
                        new OnSetupFinishedDispatcher(OnSetupFinished);
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(TLSSecurity), $"{nameof(PostInstallSetupFinishedCallback)}", ex);
                }
                finally
                {
                    _isSetupFinished = true;
                }
            });
        }

        /// <summary>
        /// Blocks the current thread until setup is finished.
        /// </summary>
        public static void WaitForSetupFinish()
        {
            SpinWait spinWait = new SpinWait();
            while (!_isSetupFinished)
                spinWait.SpinOnce();
        }

        /// <summary>
        /// TLSClient factory implementation that can be used for HTTPManager.TlsClientFactory.
        /// </summary>
        public static AbstractTls13Client ExtendedTlsClientFactory(Uri uri, List<ProtocolName> protocols, LoggingContext context)
        {
            // http://tools.ietf.org/html/rfc3546#section-3.1
            // -It is RECOMMENDED that clients include an extension of type "server_name" in the client hello whenever they locate a server by a supported name type.
            // -Literal IPv4 and IPv6 addresses are not permitted in "HostName".

            // User-defined list has a higher priority
            List<ServerName> hostNames = null;

            // If there's no user defined one and the host isn't an IP address, add the default one
            if (!uri.IsHostIsAnIPAddress())
            {
                hostNames = new List<ServerName>(1);
                hostNames.Add(new ServerName(0, System.Text.Encoding.UTF8.GetBytes(uri.Host)));
            }

            return new SecureTlsClient(uri, hostNames, protocols, context);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#if UNITY_2019_3_OR_NEWER
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
        static void ResetSetup()
        {
            HTTPManager.Logger.Information(nameof(TLSSecurity), $"{nameof(ResetSetup)}");
            UnloadDatabases();
        }

        const string Folder_Plugin = "com.Tivadar.Best.TLSSecurity";

        static string GetPluginResourcesFolder()
        {
            var pluginFolder = Path.GetDirectoryName(UnityEditor.Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(Folder_Plugin));
            var rootFolder = Path.GetDirectoryName(UnityEngine.Application.dataPath);
            return Path.Combine(rootFolder, pluginFolder, "Resources");
        }

        /// <summary>
        /// Creates the client credentials DB if it can't be found in the resources folder.
        /// </summary>
        /// <see href="https://github.com/Benedicht/BestHTTP-Issues/issues/144"/>
        static void CreateClientCredentialsIfNotFound()
        {
            HTTPManager.Logger.Information(nameof(TLSSecurity), $"{nameof(CreateClientCredentialsIfNotFound)}()");

            var resourcesFolder = GetPluginResourcesFolder();
            var dbName = SecurityOptions.ClientCredentialsOptions.Name;

            var hashFileName = Path.Combine(resourcesFolder, $"{dbName}_{SecurityOptions.FolderAndFileOptions.HashExtension}.bytes");
            var metadataFileName = Path.Combine(resourcesFolder, $"{dbName}_{SecurityOptions.FolderAndFileOptions.MetadataExtension}.bytes");
            var dbFileName = Path.Combine(resourcesFolder, $"{dbName}_{SecurityOptions.FolderAndFileOptions.DatabaseExtension}.bytes");

            if (!File.Exists(hashFileName)) using (File.Create(hashFileName)) { }
            if (!File.Exists(metadataFileName)) using (File.Create(metadataFileName)) { }
            if (!File.Exists(dbFileName)) using (File.Create(dbFileName)) { }
        }

        static void CompareAndCopy(string certificationStoreFolder, string dbname)
        {
            HTTPManager.Logger.Information(nameof(TLSSecurity), $"{nameof(CompareAndCopy)}(\"{certificationStoreFolder}\", \"{dbname}\")");

            var asset = File.ReadAllBytes(Path.Combine(GetPluginResourcesFolder(), $"{dbname}_{SecurityOptions.FolderAndFileOptions.HashExtension}.bytes"));
            bool copyAssets = CompareAssets(asset, Path.ChangeExtension(Path.Combine(certificationStoreFolder, dbname), SecurityOptions.FolderAndFileOptions.HashExtension));

            if (copyAssets)
            {
                LoadAndWrite(certificationStoreFolder, dbname, SecurityOptions.FolderAndFileOptions.MetadataExtension);
                LoadAndWrite(certificationStoreFolder, dbname, SecurityOptions.FolderAndFileOptions.DatabaseExtension);
                LoadAndWrite(certificationStoreFolder, dbname, SecurityOptions.FolderAndFileOptions.HashExtension);
            }
        }

        static void LoadAndWrite(string targetFolder, string resourceName, string extension)
        {
            HTTPManager.Logger.Information(nameof(TLSSecurity), $"{nameof(LoadAndWrite)}(\"{targetFolder}\", \"{resourceName}\", \"{extension}\")");

            var asset = File.ReadAllBytes(Path.Combine(GetPluginResourcesFolder(), $"{resourceName}_{extension}.bytes"));

            string targetFileName = Path.ChangeExtension(Path.Combine(targetFolder, resourceName), extension);
            using (var outStream = HTTPManager.IOService.CreateFileStream(targetFileName, FileStreamModes.Create))
            {
                outStream.Write(asset, 0, asset.Length);
                HTTPManager.Logger.Information(nameof(TLSSecurity), $"-- wrote {asset.Length:N0} bytes to {targetFileName}");
            }
        }

        static bool CompareAssets(byte[] asset, string fileName)
        {
            bool copyAssets = true;

            try
            {
                byte[] hash = File.ReadAllBytes(fileName);
                copyAssets = !HTTP.SecureProtocol.Org.BouncyCastle.Utilities.Arrays.AreEqual(hash, 0, hash.Length, asset, 0, asset.Length);
            }
            catch (Exception ex)
            {
                if (HTTPManager.Logger.IsDiagnostic)
                    HTTPManager.Logger.Exception(nameof(TLSSecurity), $"{nameof(CompareAssets)}({fileName})", ex);
            }

            return copyAssets;
        }
#endif

        /// <summary>
        /// Helper class to call OnSetupFinished on Unity's main thread
        /// </summary>
        sealed class OnSetupFinishedDispatcher : IHeartbeat
        {
            Action _onSetupFinished;

            public OnSetupFinishedDispatcher(Action onSetupFinished)
            {
                this._onSetupFinished = onSetupFinished;

                HTTPManager.Heartbeats.Subscribe(this);
            }

            public void OnHeartbeatUpdate(DateTime now, TimeSpan dif)
            {
                try
                {
                    this._onSetupFinished?.Invoke();
                }
                catch (Exception ex)
                {
                    HTTPManager.Logger.Exception(nameof(OnSetupFinishedDispatcher), $"{nameof(_onSetupFinished)}", ex);
                }
                finally
                {
                    HTTPManager.Heartbeats.Unsubscribe(this);
                }
            }
        }
    }
}

#endif
