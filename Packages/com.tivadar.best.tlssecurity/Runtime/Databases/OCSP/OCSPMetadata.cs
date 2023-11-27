#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using System.IO;

using Best.TLSSecurity.Databases.Shared;

namespace Best.TLSSecurity.Databases.OCSP
{
    public class OCSPMetadata : Metadata
    {
        public byte[] Hash;

        public override void SaveTo(Stream stream)
        {
            base.SaveTo(stream);

            stream.WriteByte((byte)this.Hash.Length);
            stream.Write(this.Hash, 0, this.Hash.Length);
        }

        public override void LoadFrom(Stream stream)
        {
            base.LoadFrom(stream);

            int length = stream.ReadByte();
            this.Hash = new byte[length];
            stream.Read(this.Hash, 0, this.Hash.Length);
        }
    }
}
#endif
