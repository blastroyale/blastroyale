using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLightServerSDK.Modules;
using Nethereum.Util;
using Newtonsoft.Json;
using Quantum;

namespace FirstLight.Game.Data
{
	[Serializable]
	public class Web3Voucher
	{
		public Guid VoucherId;
		public int Type;
		public BigInteger Value;
		public string Signature;

		public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
	}

	public static class VoucherTypes
	{
		/// <summary>
		/// Maps the game ids with the smart contract voucher types
		/// </summary>
		public static readonly Dictionary<GameId, int> ByGameId = new ()
		{
			{GameId.CS, 1},
			{GameId.MaleCorpos, 2},
			{GameId.NOOB, 3},
			{GameId.PartnerYGG, 4}
		};

		public static readonly Dictionary<int, GameId> ByVoucherType = ByGameId
			.ToDictionary(x => x.Value, x => x.Key);
	}
	
	[IgnoredInForceUpdate]
	[Serializable]
	public class Web3PlayerData
	{
		public string Wallet;
        
        // Vouchers to do web2 -> web3 purchases
		public List<Web3Voucher> Vouchers = new ();
        
        // Track how much player has spent on-chain
		public BigInteger NoobPurchases;

		public ulong Version;

		public override int GetHashCode()
		{
            int hash = 17;
            hash = hash * 23 + Wallet.GetDeterministicHashCode();
            hash = hash * 23 + (int)NoobPurchases;
            hash = hash * 23 + Vouchers.Count;
			hash = hash * 23 + (int)Version;
            return hash;
		}
	}
	
	public class Web3PrivateData
	{
		public string Signer;
	}
}