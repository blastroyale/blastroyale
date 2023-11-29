using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Best.HTTP;
using Best.TLSSecurity.Examples.CSV;
using Best.HTTP.Shared;

#if !UNITY_WEBGL || UNITY_EDITOR
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;
#endif

using UnityEngine;

namespace Best.TLSSecurity.Examples.BatchedTest
{
    public struct Batch
    {
        public readonly string Header;
        public readonly string ResourceName;
        public readonly int ColumnIdx;
        public readonly OnRequestFinishedDelegate Callback;

        public Batch(string header, string name, int idx, OnRequestFinishedDelegate callback)
        {
            this.Header = header;
            this.ResourceName = name;
            this.ColumnIdx = idx;
            this.Callback = callback;
        }
    }

    public class BatchedTestEntry : MonoBehaviour
    {
        const int MaxActiveRequests = 10;

        public KeyValueLine KeyValueLinePrefab;
        public GameObject ContentRoot;

        Batch[] batches = null;
        int batchIdx = -1;

        int activeRequests = 0;
        int domainIdx = 0;
        List<CSVValue> values;

        private void Start()
        {
            TLSSecurity.Setup();

            var settings = HTTPManager.PerHostSettings.Get("*");
            settings.HTTP2ConnectionSettings.MaxIdleTime = settings.HTTP1ConnectionSettings.MaxConnectionIdleTime = TimeSpan.FromSeconds(6);

            batches = new Batch[]
            {
                new Batch("Expected To Fail Addresses", "MustFailAddresses", 0, ExpectToFailCallback),
                new Batch("Top 500 Domains", "top500domains", 1, ExpectToSucceedCallback)
            };

            ProcessNextBatch();
        }

        private void ProcessNextBatch()
        {
            if (batchIdx >= batches.Length - 1)
            {
                values = null;
                return;
            }

            var batch = batches[++batchIdx];

            var assets = Resources.LoadAll<TextAsset>(batch.ResourceName);
            try
            {
                if (assets == null || assets.Length == 0)
                {
                    ProcessNextBatch();
                    return;
                }

                CSVDB db = CSVReader.Read(new MemoryStream(assets.FirstOrDefault().bytes));
                values = db.Columns[batch.ColumnIdx].Values;

                KeyValueLine kvl = Instantiate<KeyValueLine>(this.KeyValueLinePrefab, this.ContentRoot.transform);
                kvl.SetAsHeader(batch.Header);

                SendRequests();
            }
            finally
            {
                foreach(var asset in assets)
                    Resources.UnloadAsset(asset);
            }
        }

        private void SendRequests()
        {
            if (values == null)
                return;

            while (activeRequests < MaxActiveRequests && domainIdx < values.Count)
            {
                KeyValueLine kvl = Instantiate<KeyValueLine>(this.KeyValueLinePrefab, this.ContentRoot.transform);

                var request = new HTTPRequest(new Uri(values[domainIdx++].Value), batches[batchIdx].Callback);

                request.DownloadSettings.DisableCache = true;
                request.TimeoutSettings.ConnectTimeout = TimeSpan.FromMinutes(1);
                request.TimeoutSettings.Timeout = TimeSpan.FromMinutes(10);

                request.Tag = kvl;

                request.Send();

                activeRequests++;

                kvl.Init(string.Format("[{0,3}/{1}] {2}", domainIdx, values.Count, request.Uri.ToString()), "sent");
            }

            if (domainIdx >= values.Count)
            {
                domainIdx = 0;
                ProcessNextBatch();
            }
        }

        void ExpectToFailCallback(HTTPRequest req, HTTPResponse resp)
        {
            activeRequests--;

            KeyValueLine kvl = req.Tag as KeyValueLine;

            switch (req.State)
            {
                // The request finished without any problem.
                case HTTPRequestStates.Finished:
                    kvl.SetFailed("Expectation Failed!");
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
#if !UNITY_WEBGL || UNITY_EDITOR
                    if (req.Exception is TlsFatalAlert || req.Exception is TlsFatalAlertReceived)
                        kvl.SetSuccess("Failed Successfully!");
                    else
#endif
                        kvl.SetWarning("Failed with Error!");
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    kvl.SetWarning("Aborted!");
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    kvl.SetWarning("Connection Timed Out!");
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    kvl.SetWarning("Timed Out!");
                    break;
            }

            SendRequests();
        }

        void ExpectToSucceedCallback(HTTPRequest req, HTTPResponse resp)
        {
            activeRequests--;

            KeyValueLine kvl = req.Tag as KeyValueLine;

            switch (req.State)
            {
                // The request finished without any problem.
                case HTTPRequestStates.Finished:
                    kvl.SetSuccess("Succeeded!");
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
#if !UNITY_WEBGL || UNITY_EDITOR
                    if (req.Exception is TlsFatalAlert)
                        kvl.SetFailed("TLS Verification Failed: " + (req.Exception.InnerException != null ? req.Exception.InnerException.Message : req.Exception.Message));
                    else
#endif
                        kvl.SetWarning(req.Exception.Message);
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    kvl.SetWarning("Aborted!");
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    kvl.SetWarning("Connection Timed Out!");
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    kvl.SetWarning("Timed Out!");
                    break;
            }

            SendRequests();
        }
    }
}
