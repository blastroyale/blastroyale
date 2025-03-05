using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Web3.Runtime.SequenceContracts;
using Nethereum.Web3;
using Quantum;
using Sequence;
using Sequence.EmbeddedWallet;
using Sequence.Provider;

namespace FirstLight.Web3.Runtime
{
	/// <summary>
	/// Represents web3 service collection
	/// </summary>
	public interface IWeb3ExternalService : IWeb3Service
	{
		SequenceEthClient Rpc { get; }
		Web3Config Web3Config { get; }
		Web3PlayerDataService Web3PlayerData { get; }
		IIndexer Indexer { get; }
		IGameServices GameServices { get; }
		IGameDataProvider GameData { get; }
		SequenceWallet Wallet { get; }
		Web3Currency GetCurrency(GameId id);
		Web3ShopService web3ShopService { get; }
		VoucherService VoucherService { get; }
	}

}