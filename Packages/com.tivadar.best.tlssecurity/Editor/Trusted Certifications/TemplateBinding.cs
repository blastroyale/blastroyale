using UnityEngine;

namespace Best.TLSSecurity.Editor
{
    public sealed class TemplateBinding : ScriptableObject
    {
        public string header;

        public string originalURL;
        public string URL;
        public bool clearBeforeDownload = true;
        public bool keepCustomCertificates = true;

        public string status;
        public int count;
        public string certificateStats;

        public int MaxSubjectKeyIdentifierLength = 40;
        public int MinLengthToSearch = 3;

        public string MetadataExtension;
        public string DatabaseExtension;
        public string HashExtension;

        public string HelpURL;
    }
}
