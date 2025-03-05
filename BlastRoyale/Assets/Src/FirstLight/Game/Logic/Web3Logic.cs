using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using Newtonsoft.Json;
using Quantum;
using UnityEngine;
using BigInteger = System.Numerics.BigInteger;

namespace FirstLight.Game.Logic
{
	
	public class PackedItem
	{
		public byte[] GameId = new byte[2];
		public byte[] Metadata = new Byte[32];
	}
	
	public interface IWeb3DataProvider
	{
		public GameId [] ValidWeb3Currencies => new GameId []
		{
			GameId.NOOB
		};
		
		public BigInteger GetCurrencyAsVouchers(GameId type);
		public IReadOnlyList<Web3Voucher> PlayerDataVouchers { get; }
		public IWeb3Service OnChainData => MainInstaller.ResolveWeb3();
		public BigInteger LastBlockSynced { get; }
		public ItemData UnpackItem(PackedItem packed);
		public PackedItem PackItem(ItemData item);
		public bool CanUseWeb3();
		public int NoobSpentInLogic();
	}

	public interface IWeb3Logic : IWeb3DataProvider
	{
		public void AwardWeb3Currency(GameId id, BigInteger amount, string contract, int chain);
	}

	public class Web3Logic : AbstractBaseLogic<Web3PlayerData>, IWeb3Logic, IGameLogicInitializer
	{
		private ABIEncode abi = new ();
		private string _random = "0x7Ac410F4E36873022b57821D7a8EB3D7513C045a";

		public IReadOnlyList<Web3Voucher> PlayerDataVouchers => Data.Vouchers;
		
		public Web3Logic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}
		
		public BigInteger GetCurrencyAsVouchers(GameId forType)
		{
			var type = VoucherTypes.ByGameId[forType];
			if (Data.Vouchers.Count == 0) return 0;
			
			return Data.Vouchers
				.Where(v => v.Type == type)
				.Select(v => ConvertFromWei(v.Value))
				.Aggregate((currentSum, item) => currentSum + item);
		}

		public void AwardWeb3Currency(GameId currency, BigInteger amount, string contract, int chain)
		{
			var type = VoucherTypes.ByGameId[currency];
			var newVoucher = new Web3Voucher()
			{
				VoucherId = Guid.NewGuid(),
				Type = type,
				Value = ConvertToWei((decimal)amount)
			};
			newVoucher.Signature = SignVoucher(newVoucher, contract, chain);
			Data.Vouchers.Add(newVoucher);
			Data.Version++;
			GameLogic.MessageBrokerService.Publish(new VoucherCreatedMessage()
			{
				Voucher = newVoucher
			});
		}
		
		public static BigInteger ConvertToWei(decimal ether)
		{
			decimal weiValue = ether * (decimal)Math.Pow(10, 18);
			return new BigInteger(weiValue);
		}
		
		public static BigInteger ConvertFromWei(BigInteger wei)
		{
			decimal etherValue = (decimal)wei / (decimal)Math.Pow(10, 18);
			return (BigInteger)etherValue;
		}

		public BigInteger LastBlockSynced { get; set; }

		public string SignVoucher(Web3Voucher voucher, string contract, int chain)
		{
			var signer = DataProvider.GetData<Web3PrivateData>().Signer ?? _random;
			var domainTypeHash = KeccackString("EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)");
			var nameHash = KeccackString("FLG");
			var versionHash = KeccackString("1");
			var chainFormatted = "0x" + chain.ToString("x"); // parsing to solidity hex format
			var encodedDomain = Sha3Keccack.Current.CalculateHash(abi.GetABIEncoded(
				new ABIValue("bytes32", domainTypeHash),
				new ABIValue("bytes32", nameHash),
				new ABIValue("bytes32", versionHash),
				new ABIValue("uint256", chainFormatted),
				new ABIValue("address", contract)
			));

			var voucherData = voucher.Value.ToByteArray();
			var data = voucherData.Reverse().ToArray().PadTo32Bytes(); // solidity needs the padding
			var voucherId = voucher.VoucherId.ToByteArray();
			
			// god damn different OS on mobile M.fkers
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(voucherId, 0, 4);
				Array.Reverse(voucherId, 4, 2);
				Array.Reverse(voucherId, 6, 2);
			}

			var voucherStructHash = Sha3Keccack.Current.CalculateHash(abi.GetABIEncoded(
				new ABIValue("bytes32", KeccackString("NFTVoucher(bytes16 voucherId,bytes32[] data,address wallet,uint64 signatureType)")),
				new ABIValue("bytes16", voucherId),
				new ABIValue("bytes32", Sha3Keccack.Current.CalculateHash(data)),
				new ABIValue("address", Data.Wallet),
				new ABIValue("uint64", voucher.Type)
			));

			var digestMessage = Sha3Keccack.Current.CalculateHash(abi.GetABIEncodedPacked(
				new ABIValue("bytes2", new byte[] {0x19, 0x01}),
				new ABIValue("bytes32", encodedDomain),
				new ABIValue("bytes32", voucherStructHash)
			));

			var signature = new EthereumMessageSigner().SignAndCalculateV(digestMessage, new EthECKey(signer));
			var encodedSignature = abi.GetABIEncodedPacked(signature.R, signature.S, signature.V);
			return encodedSignature.ToHex();
		}
		
		public PackedItem PackItem(ItemData item)
		{
			var packed = new PackedItem();
			packed.GameId = BitConverter.GetBytes((ushort) item.Id);
			if (item.TryGetMetadata<CurrencyMetadata>(out var m1))
			{
				packed.Metadata = BitConverter.GetBytes((ushort) m1.Amount).PadTo32Bytes(); 
			} else if (item.TryGetMetadata<CollectionMetadata>(out var m2))
			{
				if (m2.Traits.Length > 0)
				{
					throw new Exception("Metadata serialization not yet implemented on web3 shop");
				}
			}
			return packed;
		}

		public bool CanUseWeb3()
		{
			return GameLogic.PlayerLogic.Flags.HasFlag(PlayerFlags.FLGOfficial);
		}

		public int NoobSpentInLogic()
		{
			return (int)Data.NoobPurchases;
		}

		public ItemData UnpackItem(PackedItem item)
		{
			var gameIdBytes = item.GameId;
			var metadataBytes = item.Metadata;
			Assert.Check(gameIdBytes.Length == 2, "Invalid gameid size");
			Assert.Check(metadataBytes.Length == 32, "Invalid metadata size");
			var gameId = (GameId)BitConverter.ToUInt16(gameIdBytes);
			if (gameId.IsInGroup(GameIdGroup.Currency) || gameId.IsInGroup(GameIdGroup.Resource))
			{
				return ItemFactory.Currency(gameId, (int)BitConverter.ToUInt16(metadataBytes[30..32]));
			} if (gameId.IsInGroup(GameIdGroup.Collection))
			{
				return ItemFactory.Collection(gameId);
			}
			throw new Exception("Item type "+gameId+" is not supported");
		}
		
		private byte[] KeccackString(string s)
		{
			return Sha3Keccack.Current.CalculateHash(System.Text.Encoding.UTF8.GetBytes(s));
		}

		/// <inheritdoc />
		public void Init()
		{
		}

		public void ReInit()
		{
		}
	}
}