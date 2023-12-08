#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace Best.HTTP.SecureProtocol.Org.BouncyCastle.Math.EC.Multiplier
{
	/**
	* Interface for classes storing precomputation data for multiplication
	* algorithms. Used as a Memento (see GOF patterns) for
	* <code>WNafMultiplier</code>.
	*/
	public interface PreCompInfo
	{
	}
}
#pragma warning restore
#endif
