#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1;

namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Asn1.Misc
{
    public class VerisignCzagExtension
        : DerIA5String
    {
        public VerisignCzagExtension(DerIA5String str)
			: base(str.GetString())
        {
        }

        public override string ToString()
        {
            return "VerisignCzagExtension: " + this.GetString();
        }
    }
}
#pragma warning restore
#endif
