using UnityEngine;

namespace Best.TLSSecurity.Editor
{
    public sealed class CertificationModel : ScriptableObject
    {
        public int idx;
        public string isUserAdded;
        public string isLocked;
        public string subject;
        public string issuer;
    }
}
