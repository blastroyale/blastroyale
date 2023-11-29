#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System;

using Best.TLSSecurity.Databases.ClientCredentials;
using Best.TLSSecurity.Databases.OCSP;
using Best.TLSSecurity.Databases.X509;

namespace Best.TLSSecurity
{
    /// <summary>
    /// Represents configuration options specific to the HTTP requests made for OCSP caching.
    /// </summary>
    public sealed class OCSPCacheHTTPRequestOptions
    {
        /// <summary>
        /// Gets or sets the threshold for data length.
        /// </summary>
        public int DataLengthThreshold = 256;

        /// <summary>
        /// Determines whether to use the KeepAlive feature in the HTTP requests.
        /// </summary>
        public bool UseKeepAlive = true;

        /// <summary>
        /// Determines whether to allow caching for the HTTP requests.
        /// </summary>
        public bool UseCache = true;

        /// <summary>
        /// Gets or sets the connection timeout duration for the HTTP request.
        /// </summary>
        public TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets or sets the general timeout duration for the HTTP request.
        /// </summary>
        public TimeSpan Timeout = TimeSpan.FromSeconds(2);
    }

    /// <summary>
    /// Represents configuration options specific to OCSP caching.
    /// </summary>
    public sealed class OCSPCacheOptions
    {
        /// <summary>
        /// Gets or sets the maximum wait time for OCSP responses.
        /// </summary>
        public TimeSpan MaxWaitTime = TimeSpan.FromSeconds(4);

        /// <summary>
        /// Determines the duration after which to retry in cases of unknown OCSP statuses.
        /// </summary>
        public TimeSpan RetryUnknownAfter = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the folder name for OCSP caching.
        /// </summary>
        public string FolderName = "OCSPCache";

        /// <summary>
        /// Represents database-specific options for OCSP caching.
        /// </summary>
        public OCSPDatabaseOptions DatabaseOptions = new OCSPDatabaseOptions("OCSPStatus");

        /// <summary>
        /// Represents HTTP request-specific options for OCSP caching.
        /// </summary>
        public OCSPCacheHTTPRequestOptions HTTPRequestOptions = new OCSPCacheHTTPRequestOptions();
    }

    /// <summary>
    /// Represents the configuration options for handling OCSP (Online Certificate Status Protocol) operations.
    /// </summary>
    /// <remarks>
    /// The OCSPOptions class provides settings that dictate behavior for:
    /// <list type="bullet">
    /// <item><description>Sending OCSP requests for certificate revocation checks.</description></item>
    /// <item><description>Thresholds for lifespan to determine revocation checks.</description></item>
    /// <item><description>Handling scenarios with unknown revocation statuses.</description></item>
    /// <item><description>Ensuring server compliance with the must-staple flag.</description></item>
    /// <item><description>Configurations related to caching OCSP responses.</description></item>
    /// </list>
    /// The class enables granular control over OCSP operations, ensuring that applications can fine-tune their security and performance behaviors 
    /// with regard to certificate revocation checks.
    /// </remarks>
    public sealed class OCSPOptions
    {
        /// <summary>
        /// Enable or disable sending out OCSP requests for revocation checking.
        /// </summary>
        public bool EnableOCSPQueries = true;

        /// <summary>
        /// The plugin not going to check revocation status for short lifespan certificates.
        /// </summary>
        public TimeSpan ShortLifeSpanThreshold = TimeSpan.FromDays(10);

        /// <summary>
        /// Treat unknown revocation statuses (unknown OCSP status or unreachable servers) as revoked and abort the TLS negotiation.
        /// </summary>
        public bool FailHard = false;

        /// <summary>
        /// Treat the TLS connection failed if the leaf certificate has the must-staple flag, but the server doesn't send certificate status.
        /// </summary>
        public bool FailOnMissingCertStatusWhenMustStaplePresent = true;

        /// <summary>
        /// OCSP Cache Options
        /// </summary>
        public OCSPCacheOptions OCSPCache = new OCSPCacheOptions();
    }

    /// <summary>
    /// Contains options related to file and folder naming conventions used within the plugin.
    /// </summary>
    public sealed class FolderAndFileOptions
    {
        /// <summary>
        /// Gets or sets the main folder name for the plugin's file storage.
        /// </summary>
        public string FolderName = "TLSSecurity";

        /// <summary>
        /// Gets or sets the folder name designated for database storage.
        /// </summary>
        public string DatabaseFolderName = "Databases";

        /// <summary>
        /// Gets or sets the file extension for metadata files.
        /// </summary>
        public string MetadataExtension = "metadata";

        /// <summary>
        /// Gets or sets the file extension for database files.
        /// </summary>
        public string DatabaseExtension = "db";

        /// <summary>
        /// Gets or sets the file extension for database free list files.
        /// </summary>
        public string DatabaseFreeListExtension = "dfl";

        /// <summary>
        /// Gets or sets the file extension for hash files.
        /// </summary>
        public string HashExtension = "hash";
    }

    /// <summary>
    /// Provides centralized security settings and configuration options related to certificate management, 
    /// OCSP (Online Certificate Status Protocol) operations, and other file and folder settings. The 
    /// SecurityOptions class consolidates options for determining how the plugin interacts 
    /// with server-sent certificates, OCSP operations, and databases for trusted certificate authorities and credentials.
    /// </summary>
    /// <remarks>
    /// This class centralizes various security settings, allowing developers to easily configure and fine-tune 
    /// security behaviors in the application, ensuring optimal balance between security, compatibility, and performance.
    /// </remarks>
    public static class SecurityOptions
    {
        /// <summary>
        /// If false, only certificates stored in the trusted intermediates database are used to reconstruct the certificate chain. 
        /// When set to <c>true</c> (default), it improves compatibility but the plugin going to use/accept certificates that not stored in its trusted database.
        /// </summary>
        public static bool UseServerSentIntermediateCertificates = true;

        /// <summary>
        /// Folder, file and extension options.
        /// </summary>
        public static FolderAndFileOptions FolderAndFileOptions = new FolderAndFileOptions();

        /// <summary>
        /// OCSP and OCSP cache options.
        /// </summary>
        public static OCSPOptions OCSP = new OCSPOptions();

        /// <summary>
        /// Database options of the Trusted CAs database
        /// </summary>
        public static X509DatabaseOptions TrustedRootsOptions = new X509DatabaseOptions("TrustedRoots");

        /// <summary>
        /// Database options of the Trusted Intermediate Certifications database
        /// </summary>
        public static X509DatabaseOptions TrustedIntermediatesOptions = new X509DatabaseOptions("TrustedIntermediates");

        /// <summary>
        /// Database options of the Client Credentials database
        /// </summary>
        public static ClientCredentialDatabaseOptions ClientCredentialsOptions = new ClientCredentialDatabaseOptions("ClientCredentials");
    }
}

#endif
