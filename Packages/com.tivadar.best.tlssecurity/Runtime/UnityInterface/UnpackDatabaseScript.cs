#if !BESTHTTP_DISABLE_ALTERNATE_SSL && !UNITY_WEBGL && !UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities;
using Best.HTTP.Shared;
using Best.HTTP.Shared.PlatformSupport.FileSystem;
using Best.HTTP.Shared.PlatformSupport.Memory;

using UnityEngine;

namespace Best.TLSSecurity.Install
{
    public sealed class UnpackDatabaseScript : MonoBehaviour
    {
        Dictionary<string, ResourceRequest> resourceRequests = new Dictionary<string, ResourceRequest>();

        private void Awake()
        {
            HTTPManager.Logger.Information(nameof(UnpackDatabaseScript), $"Awake");

            GameObject.DontDestroyOnLoad(this.gameObject);
            StartCoroutine(Unpack());
        }

        IEnumerator Unpack()
        {
            string certificationStoreFolder = Path.Combine(HTTPManager.GetRootSaveFolder(), SecurityOptions.FolderAndFileOptions.FolderName, SecurityOptions.FolderAndFileOptions.DatabaseFolderName);

            ResourceRequest trustedRootsHashAssetAsyncOp = null;
            ResourceRequest trustedIntermediatesHashAssetAsyncOp = null;
            ResourceRequest clientCredentialsHashAssetAsyncOp = null;
            IEnumerator cp = null;

            try
            {
                resourceRequests.Clear();

                HTTPManager.Logger.Information(nameof(UnpackDatabaseScript), $"Unpack starting... certificationStoreFolder: {certificationStoreFolder}");

                if (!HTTPManager.IOService.DirectoryExists(certificationStoreFolder))
                    HTTPManager.IOService.DirectoryCreate(certificationStoreFolder);

                trustedRootsHashAssetAsyncOp = Resources.LoadAsync<TextAsset>(SecurityOptions.TrustedRootsOptions.Name + "_" + SecurityOptions.FolderAndFileOptions.HashExtension);
                trustedIntermediatesHashAssetAsyncOp = Resources.LoadAsync<TextAsset>(SecurityOptions.TrustedIntermediatesOptions.Name + "_" + SecurityOptions.FolderAndFileOptions.HashExtension);
                clientCredentialsHashAssetAsyncOp = Resources.LoadAsync<TextAsset>(SecurityOptions.ClientCredentialsOptions.Name + "_" + SecurityOptions.FolderAndFileOptions.HashExtension);

                cp = CompareAndCopy(certificationStoreFolder, SecurityOptions.TrustedRootsOptions.Name, trustedRootsHashAssetAsyncOp);
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(UnpackDatabaseScript), $"{nameof(Unpack)}", ex);
            }

            while (cp.MoveNext())
                yield return cp;
            HTTPManager.Logger.Information(nameof(UnpackDatabaseScript), $"... Roots CAs done!");

            try
            {
                cp = CompareAndCopy(certificationStoreFolder, SecurityOptions.TrustedIntermediatesOptions.Name, trustedIntermediatesHashAssetAsyncOp);
            }
            catch(Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(UnpackDatabaseScript), $"{nameof(Unpack)}", ex);
            }

            while (cp.MoveNext())
                yield return cp;

            HTTPManager.Logger.Information(nameof(UnpackDatabaseScript), $"... Trusted Intermediates done!");

            try
            {
                cp = CompareAndCopy(certificationStoreFolder, SecurityOptions.ClientCredentialsOptions.Name, clientCredentialsHashAssetAsyncOp);
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(UnpackDatabaseScript), $"{nameof(Unpack)}", ex);
            }

            while (cp.MoveNext())
                yield return cp;

            HTTPManager.Logger.Information(nameof(UnpackDatabaseScript), $"... Client Credentials done!");

            resourceRequests.Clear();

            TLSSecurity.PostInstallSetupFinishedCallback();

            if (Application.isPlaying)
                Destroy(this.gameObject);
            else
                DestroyImmediate(this.gameObject);
        }

        void AddResourceRequest(string resourceName, string extension)
        {
            string name = resourceName + "_" + extension;
            resourceRequests.Add(name, Resources.LoadAsync<TextAsset>(resourceName + "_" + extension));
        }

        ResourceRequest GetResourceRequest(string resourceName, string extension)
        {
            string name = resourceName + "_" + extension;
            return resourceRequests[name];
        }

        IEnumerator CompareAndCopy(string certificationStoreFolder, string name, ResourceRequest hashRequest)
        {
            HTTPManager.Logger.Information(nameof(UnpackDatabaseScript), $"{nameof(CompareAndCopy)}({certificationStoreFolder}, {name}, {hashRequest})");

            while (!hashRequest.isDone)
                yield return hashRequest;

            var asset = hashRequest.asset as TextAsset;

            string fileName = Path.ChangeExtension(Path.Combine(certificationStoreFolder, name), SecurityOptions.FolderAndFileOptions.HashExtension);
            bool copyAssets = CompareAssets(asset, fileName);

            Resources.UnloadAsset(asset);

            if (copyAssets)
            {
                AddResourceRequest(name, SecurityOptions.FolderAndFileOptions.MetadataExtension);
                AddResourceRequest(name, SecurityOptions.FolderAndFileOptions.DatabaseExtension);
                AddResourceRequest(name, SecurityOptions.FolderAndFileOptions.HashExtension);

                var lw = LoadAndWrite(certificationStoreFolder, name, SecurityOptions.FolderAndFileOptions.MetadataExtension);
                while (lw.MoveNext())
                    yield return lw;

                lw = LoadAndWrite(certificationStoreFolder, name, SecurityOptions.FolderAndFileOptions.DatabaseExtension);
                while (lw.MoveNext())
                    yield return lw;

                lw = LoadAndWrite(certificationStoreFolder, name, SecurityOptions.FolderAndFileOptions.HashExtension);
                while (lw.MoveNext())
                    yield return lw;
            }
        }

        IEnumerator LoadAndWrite(string targetFolder, string resourceName, string extension)
        {
            HTTPManager.Logger.Information(nameof(UnpackDatabaseScript), $"{nameof(LoadAndWrite)}({targetFolder}, {resourceName}, {extension})");

            var asyncOp = GetResourceRequest(resourceName, extension);
            while (!asyncOp.isDone)
                yield return asyncOp;

            var asset = asyncOp.asset as TextAsset;

            if (asset == null)
                yield break;

            string fileName = Path.ChangeExtension(Path.Combine(targetFolder, resourceName), extension);
            using (var outStream = HTTPManager.IOService.CreateFileStream(fileName, FileStreamModes.Create))
                outStream.Write(asset.bytes, 0, asset.bytes.Length);

            Resources.UnloadAsset(asset);
        }

        bool CompareAssets(TextAsset asset, string fileName)
        {
            bool copyAssets = true;

            try
            {
                if (HTTPManager.IOService.FileExists(fileName))
                {
                    using (var outStream = HTTPManager.IOService.CreateFileStream(fileName, FileStreamModes.OpenRead))
                    {
                        byte[] hash = BufferPool.Get(outStream.Length, true);
                        outStream.Read(hash, 0, (int)outStream.Length);

                        copyAssets = !Arrays.AreEqual(hash, 0, (int)outStream.Length, asset.bytes, 0, asset.bytes.Length);

                        BufferPool.Release(hash);
                    }
                }
            }
            catch(Exception ex)
            {
                HTTPManager.Logger.Exception(nameof(UnpackDatabaseScript), $"{nameof(CompareAssets)}({fileName})", ex);
            }

            return copyAssets;
        }
    }
}
#endif
