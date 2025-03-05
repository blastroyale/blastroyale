using System.Numerics;
using System.Threading.Tasks;

namespace FirstLightServerSDK.Services
{
	public interface IBlockchainApi
	{
		Task<BigInteger> GetSpentOnChain();
	}
}