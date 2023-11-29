#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Best.TLSSecurity.Databases.OCSP;
using Best.HTTP;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Ocsp;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Ocsp;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Security;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Utilities;
using Best.HTTP.SecureProtocol.Org.BouncyCastle.X509;
using Best.HTTP.Shared;
using Best.HTTP.Shared.Logger;

namespace Best.TLSSecurity.OCSP
{
    public static class OCSPCache
    {
        private static SecureRandom NonceGenerator = new SecureRandom();

        private static OCSPDatabase database;

        public static Status GetCertificateStatus(List<X509Certificate> chain, AuthorityInformationAccess aia, LoggingContext loggingContext)
        {
            if (chain.Count < 2)
                throw new Exception("At least leaf and issuer certificate expected!");

            var leaf = chain[0];

            var (hash, cachedEntry) = database.FindBy(leaf);
            if (cachedEntry != null)
            {
                HTTPManager.Logger.Information(nameof(OCSPCache), $"GetCertificateStatus - Cache Hit: {cachedEntry}", loggingContext);

                cachedEntry.LastUsed = DateTime.Now;
                database.Set(hash, cachedEntry);

                switch (cachedEntry.Status)
                {
                    case Status.Revoked:
                        return cachedEntry.Status;

                    case Status.Good:
                        if (cachedEntry.NextUpdate > DateTime.Now)
                            return cachedEntry.Status;
                        break;

                    case Status.Unknown:
                        if (cachedEntry.NextUpdate < DateTime.Now)
                        {
                            // Return with the unknown status now, but start a background OCSP request to try to update
                            if (cachedEntry.ReceivedAt + SecurityOptions.OCSP.OCSPCache.RetryUnknownAfter <= DateTime.Now)
                                SendRefreshRequest(hash, chain, aia, loggingContext);

                            return cachedEntry.Status;
                        }
                        break;
                }
            }

            var worstStatus = SendBlockingRequest(chain, aia, loggingContext);

            var newEntry = new OCSPCacheEntry();
            if (worstStatus.Item2 != null)
            {
                if (worstStatus.Item2.ThisUpdate != null)
                    newEntry.GeneratedAt = worstStatus.Item2.ThisUpdate.ToDateTime();

                if (worstStatus.Item2.NextUpdate != null)
                    newEntry.NextUpdate = worstStatus.Item2.NextUpdate.ToDateTime();
            }
            else
            {
                newEntry.GeneratedAt = DateTime.MinValue;
                newEntry.NextUpdate = DateTime.MinValue;
            }

            newEntry.ReceivedAt = DateTime.Now;
            newEntry.LastUsed = DateTime.Now;
            newEntry.Status = worstStatus.Item1;

            database.Set(hash, newEntry);

            return worstStatus.Item1;
        }

        public static void Load(string rootFolder)
        {
            if (database == null)
            {
                OCSPIndexingService.Instance?.Clear();
                database = new OCSPDatabase(rootFolder);
            }
        }

        public static void Unload()
        {
            database?.Dispose();
            database = null;
        }

        private static List<AccessDescription> CreateRequests(List<X509Certificate> chain, AuthorityInformationAccess aia, bool setTimeouts, Action<HTTPRequest, HTTPResponse, List<AccessDescription>> callback, LoggingContext loggingContext)
        {
            HTTPManager.Logger.Information(nameof(OCSPCache), $"{nameof(CreateRequests)}", loggingContext);

            List<AccessDescription> ocspEndpoints = new List<AccessDescription>();

            var descriptions = aia.GetAccessDescriptions();
            foreach (var desc in descriptions)
            {
                var locationType = desc.AccessLocation.TagNo;
                if (desc.AccessMethod.Id == OcspObjectIdentifiers.PkixOcsp.Id &&
                    (locationType == GeneralName.UniformResourceIdentifier || locationType == GeneralName.DnsName || locationType == GeneralName.IPAddress))
                    ocspEndpoints.Add(desc);
            }

            if (ocspEndpoints.Count == 0)
            {
                HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(CreateRequests)} - No endpoint could be found to send OCSP request!", loggingContext);

                throw new Exception("No endpoint could be found to send OCSP request!");
            }

            OcspReqGenerator gen = new OcspReqGenerator();
            gen.AddRequest(new CertificateID(CertificateID.HashSha1, /*issuer: */ chain[1], /*leaf: */ chain[0].SerialNumber));

            byte[] nonce = new byte[20];
            NonceGenerator.NextBytes(nonce);

            gen.SetRequestExtensions(new X509Extensions(
                new List<DerObjectIdentifier>() { OcspObjectIdentifiers.PkixOcspNonce },
                new List<X509Extension>() { new X509Extension(false, new BerOctetString(new BerOctetString(nonce).GetEncoded())) }
            ));

            HTTPManager.Logger.Information(nameof(OCSPCache), $"{nameof(CreateRequests)} - Creating {ocspEndpoints.Count} HTTP request(s)", loggingContext);

            for (int i = 0; i < ocspEndpoints.Count; ++i)
            {
                var endPoint = ocspEndpoints[i];

                var ocspReq = gen.Generate();
                var data = ocspReq.GetEncoded();

                HTTPMethods method = HTTPMethods.Post;
                string path = string.Empty;
                if (data.Length < SecurityOptions.OCSP.OCSPCache.HTTPRequestOptions.DataLengthThreshold)
                {
                    path = "/" + Uri.EscapeDataString(Convert.ToBase64String(data));
                    method = HTTPMethods.Get;
                }

                var request = new HTTPRequest(new Uri(endPoint.AccessLocation.Name.ToString() + path),
                                              method,
                                              // /*isKeepAlive: */ SecurityOptions.OCSP.OCSPCache.HTTPRequestOptions.UseKeepAlive,
                                              // /*disableCache: */ SecurityOptions.OCSP.OCSPCache.HTTPRequestOptions.UseCache,
                                              (req, resp) => callback(req, resp, ocspEndpoints));
                request.DownloadSettings.DisableCache = !SecurityOptions.OCSP.OCSPCache.HTTPRequestOptions.UseCache;

                if (method == HTTPMethods.Post)
                {
                    request.AddHeader("Content-Type", "application/ocsp-request");
                    //request.RawData = data;
                    request.UploadSettings.UploadStream = new MemoryStream(data);
                }

                // We might let refresh queries run longer
                if (setTimeouts)
                {
                    request.TimeoutSettings.ConnectTimeout = SecurityOptions.OCSP.OCSPCache.HTTPRequestOptions.ConnectTimeout;
                    request.TimeoutSettings.Timeout = SecurityOptions.OCSP.OCSPCache.HTTPRequestOptions.Timeout;
                }

                request.Context.Add("Parent HTTPS Request", loggingContext);

                request.Send();
            }

            return ocspEndpoints;
        }

        public static Asn1Object ReadDerObject(byte[] encoding)
        {
            /*
             * NOTE: The current ASN.1 parsing code can't enforce DER-only parsing, but since DER is
             * canonical, we can check it by re-encoding the result and comparing to the original.
             */
            Asn1Object result = TlsUtilities.ReadAsn1Object(encoding);
            byte[] check = result.GetEncoded(Asn1Encodable.Der);
            if (!Arrays.AreEqual(check, encoding))
                throw new TlsFatalAlert(AlertDescription.decode_error);

            return result;
        }

        private static void SendRefreshRequest(byte[] hash, List<X509Certificate> chain, AuthorityInformationAccess aia, LoggingContext loggingContext)
        {
            HTTPManager.Logger.Information(nameof(OCSPCache), $"{nameof(SendRefreshRequest)} - AuthorityInformationAccess: {aia}", loggingContext);

            CreateRequests(chain, aia, false, (req, resp, endpoints) =>
            {
                switch (req.State)
                {
                    case HTTPRequestStates.Finished:
                        if (resp.IsSuccess)
                        {
                            if (resp.GetFirstHeaderValue("Content-Type") == "application/ocsp-response")
                            {
                                SingleResponse singleResponse = null;
                                Status status = Status.Unknown;
                                try
                                {
                                    var obj = ReadDerObject(resp.Data) as Asn1Sequence;
                                    OcspResponse response = OcspResponse.GetInstance(obj);
                                    var CertificateStatus = new Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.CertificateStatus(CertificateStatusType.ocsp, response);

                                    singleResponse = OCSPValidation.GetOCSPResponse(CertificateStatus, chain, loggingContext);

                                    if (singleResponse.CertStatus.Status != DerNull.Instance)
                                    {
                                        if (singleResponse.CertStatus.TagNo == (byte)Status.Revoked)
                                            status = Status.Revoked;
                                    }
                                    else
                                        status = Status.Good;
                                }
                                catch
                                {
                                    status = Status.Unknown;
                                }

                                if (status != Status.Unknown)
                                {
                                    var cachedEntry = new OCSPCacheEntry();
                                    cachedEntry.GeneratedAt = singleResponse.ThisUpdate.ToDateTime();
                                    cachedEntry.NextUpdate = singleResponse.NextUpdate.ToDateTime();
                                    cachedEntry.ReceivedAt = DateTime.Now;
                                    cachedEntry.LastUsed = DateTime.Now;
                                    cachedEntry.Status = status;

                                    database.UpdateIfNotRevoked(hash, cachedEntry);
                                }
                            }
                            else
                                HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendRefreshRequest)} - Request finished Successfully, but the server sent an error. Status Code: {resp.StatusCode}-{resp.Message} Message: {resp.DataAsText}", loggingContext);
                        }
                        else
                        {
                            HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendRefreshRequest)} - Request finished Successfully, but the server sent an error. Status Code: {resp.StatusCode}-{resp.Message} Message: {resp.DataAsText}", loggingContext);
                        }
                        break;

                    case HTTPRequestStates.Error:
                        HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendRefreshRequest)} - Request Finished with Error! " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"), loggingContext);
                        break;

                    case HTTPRequestStates.Aborted:
                        HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendRefreshRequest)} - Request Aborted!", loggingContext);
                        break;

                    case HTTPRequestStates.ConnectionTimedOut:
                        HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendRefreshRequest)} - Connection Timed Out!", loggingContext);
                        break;

                    case HTTPRequestStates.TimedOut:
                        HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendRefreshRequest)} - Processing the request Timed Out!", loggingContext);
                        break;
                }
            }, loggingContext);
        }

        private static (Status, SingleResponse) SendBlockingRequest(List<X509Certificate> chain, AuthorityInformationAccess aia, LoggingContext loggingContext)
        {
            HTTPManager.Logger.Information(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - AuthorityInformationAccess: {aia}", loggingContext);

            var queue = new ConcurrentQueue<Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.CertificateStatus>();
            int completionCount = 0;
            var are = new AutoResetEvent(false);

            var ocspEndpoints = CreateRequests(chain, aia, true, (req, resp, endpoints) =>
            {
                switch (req.State)
                {
                    case HTTPRequestStates.Finished:
                        if (resp.IsSuccess)
                        {
                            if (resp.GetFirstHeaderValue("Content-Type") == "application/ocsp-response")
                            {
                                var obj = ReadDerObject(resp.Data) as Asn1Sequence;

                                OcspResponse response = OcspResponse.GetInstance(obj);

                                queue.Enqueue(new Best.HTTP.SecureProtocol.Org.BouncyCastle.Tls.CertificateStatus(CertificateStatusType.ocsp, response));
                            }
                            else
                                HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - Request finished Successfully, but the server sent an error. Status Code: {resp.StatusCode}-{resp.Message} Message: {resp.DataAsText}", loggingContext);
                        }
                        else
                        {
                            HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - Request finished Successfully, but the server sent an error. Status Code: {resp.StatusCode}-{resp.Message} Message: {resp.DataAsText}", loggingContext);
                        }
                        break;

                    case HTTPRequestStates.Error:
                        HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - Request Finished with Error! " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"), loggingContext);
                        break;

                    case HTTPRequestStates.Aborted:
                        HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - Request Aborted!", loggingContext);
                        break;

                    case HTTPRequestStates.ConnectionTimedOut:
                        HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - Connection Timed Out!", loggingContext);
                        break;

                    case HTTPRequestStates.TimedOut:
                        HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - Processing the request Timed Out!", loggingContext);
                        break;
                }
                
                int counter = Interlocked.Increment(ref completionCount);
                
                are.Set();
                
                // last callback calls dispose
                if (counter >= endpoints.Count)
                    are.Dispose(); 
            }, loggingContext);

            // Find and store the worst status we could find
            (Status, SingleResponse) worstStatus = (Status.Unknown, null);

            while (completionCount < ocspEndpoints.Count)
            {
                try
                {
                    if (!are.WaitOne(SecurityOptions.OCSP.OCSPCache.MaxWaitTime))
                    {
                        HTTPManager.Logger.Warning(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - No OCSP request finished in time({SecurityOptions.OCSP.OCSPCache.MaxWaitTime})!", loggingContext);
                        break;
                    }
                }
                catch
                { }

                while (queue.TryDequeue(out var certificateStatus))
                {
                    if (certificateStatus == null)
                        continue;

                    try
                    {
                        HTTPManager.Logger.Information(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - Processing certificateStatus: {certificateStatus}", loggingContext);

                        var singleResponse = OCSPValidation.GetOCSPResponse(certificateStatus, chain, loggingContext);

                        if (singleResponse.CertStatus.Status != DerNull.Instance)
                        {
                            if (singleResponse.CertStatus.TagNo == (byte)Status.Revoked)
                                worstStatus = (Status.Revoked, singleResponse);
                            else if (worstStatus.Item1 == Status.Good)
                                worstStatus = (Status.Unknown, singleResponse);
                        }
                        else if (worstStatus.Item1 == Status.Unknown)
                            worstStatus = (Status.Good, singleResponse);
                    }
                    catch (Exception ex)
                    {
                        if (HTTPManager.Logger.IsDiagnostic)
                            HTTPManager.Logger.Exception(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - Processing failed!", ex, loggingContext);

                        if (worstStatus.Item1 == Status.Good)
                            worstStatus = (Status.Unknown, null);

                        continue;
                    }
                }
            }

            HTTPManager.Logger.Information(nameof(OCSPCache), $"{nameof(SendBlockingRequest)} - returning with status: {worstStatus.Item1}", loggingContext);

            return worstStatus;
        }
    }
}

#endif
