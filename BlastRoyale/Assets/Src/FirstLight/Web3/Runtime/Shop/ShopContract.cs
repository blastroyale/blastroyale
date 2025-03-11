using System;
using System.Numerics;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Util;
using Quantum;
using Sequence.ABI;
using Sequence.Contracts;

namespace FirstLight.Web3.Runtime.SequenceContracts
{
	public class ShopContract
	{
		private readonly IWeb3ExternalService _service;
		private string _contract;
		private Contract _contractInstance;
		private GameId _currency;

		private const string ABI =
			"[\n\t{\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"contract IERC20\",\n\t\t\t\t\"name\": \"shopCurrency\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t},\n\t\t\t{\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"owner\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"stateMutability\": \"nonpayable\",\n\t\t\"type\": \"constructor\"\n\t},\n\t{\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"target\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"AddressEmptyCode\",\n\t\t\"type\": \"error\"\n\t},\n\t{\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"account\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"AddressInsufficientBalance\",\n\t\t\"type\": \"error\"\n\t},\n\t{\n\t\t\"inputs\": [],\n\t\t\"name\": \"EnforcedPause\",\n\t\t\"type\": \"error\"\n\t},\n\t{\n\t\t\"inputs\": [],\n\t\t\"name\": \"ExpectedPause\",\n\t\t\"type\": \"error\"\n\t},\n\t{\n\t\t\"inputs\": [],\n\t\t\"name\": \"FailedInnerCall\",\n\t\t\"type\": \"error\"\n\t},\n\t{\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"uint64\",\n\t\t\t\t\"name\": \"gameId\",\n\t\t\t\t\"type\": \"uint64\"\n\t\t\t},\n\t\t\t{\n\t\t\t\t\"internalType\": \"bytes32\",\n\t\t\t\t\"name\": \"metadata\",\n\t\t\t\t\"type\": \"bytes32\"\n\t\t\t},\n\t\t\t{\n\t\t\t\t\"internalType\": \"uint256\",\n\t\t\t\t\"name\": \"price\",\n\t\t\t\t\"type\": \"uint256\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"IntentPurchase\",\n\t\t\"outputs\": [],\n\t\t\"stateMutability\": \"nonpayable\",\n\t\t\"type\": \"function\"\n\t},\n\t{\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"owner\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"OwnableInvalidOwner\",\n\t\t\"type\": \"error\"\n\t},\n\t{\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"account\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"OwnableUnauthorizedAccount\",\n\t\t\"type\": \"error\"\n\t},\n\t{\n\t\t\"inputs\": [],\n\t\t\"name\": \"ReentrancyGuardReentrantCall\",\n\t\t\"type\": \"error\"\n\t},\n\t{\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"token\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"SafeERC20FailedOperation\",\n\t\t\"type\": \"error\"\n\t},\n\t{\n\t\t\"anonymous\": false,\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"indexed\": true,\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"previousOwner\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t},\n\t\t\t{\n\t\t\t\t\"indexed\": true,\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"newOwner\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"OwnershipTransferred\",\n\t\t\"type\": \"event\"\n\t},\n\t{\n\t\t\"anonymous\": false,\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"indexed\": false,\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"account\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"Paused\",\n\t\t\"type\": \"event\"\n\t},\n\t{\n\t\t\"anonymous\": false,\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"indexed\": false,\n\t\t\t\t\"internalType\": \"uint64\",\n\t\t\t\t\"name\": \"gameId\",\n\t\t\t\t\"type\": \"uint64\"\n\t\t\t},\n\t\t\t{\n\t\t\t\t\"indexed\": false,\n\t\t\t\t\"internalType\": \"bytes32\",\n\t\t\t\t\"name\": \"metadata\",\n\t\t\t\t\"type\": \"bytes32\"\n\t\t\t},\n\t\t\t{\n\t\t\t\t\"indexed\": false,\n\t\t\t\t\"internalType\": \"uint256\",\n\t\t\t\t\"name\": \"price\",\n\t\t\t\t\"type\": \"uint256\"\n\t\t\t},\n\t\t\t{\n\t\t\t\t\"indexed\": false,\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"buyer\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"PurchaseIntentCreated\",\n\t\t\"type\": \"event\"\n\t},\n\t{\n\t\t\"inputs\": [],\n\t\t\"name\": \"renounceOwnership\",\n\t\t\"outputs\": [],\n\t\t\"stateMutability\": \"nonpayable\",\n\t\t\"type\": \"function\"\n\t},\n\t{\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"newOwner\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"transferOwnership\",\n\t\t\"outputs\": [],\n\t\t\"stateMutability\": \"nonpayable\",\n\t\t\"type\": \"function\"\n\t},\n\t{\n\t\t\"anonymous\": false,\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"indexed\": false,\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"account\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"Unpaused\",\n\t\t\"type\": \"event\"\n\t},\n\t{\n\t\t\"inputs\": [],\n\t\t\"name\": \"owner\",\n\t\t\"outputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"stateMutability\": \"view\",\n\t\t\"type\": \"function\"\n\t},\n\t{\n\t\t\"inputs\": [],\n\t\t\"name\": \"paused\",\n\t\t\"outputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"bool\",\n\t\t\t\t\"name\": \"\",\n\t\t\t\t\"type\": \"bool\"\n\t\t\t}\n\t\t],\n\t\t\"stateMutability\": \"view\",\n\t\t\"type\": \"function\"\n\t},\n\t{\n\t\t\"inputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"address\",\n\t\t\t\t\"name\": \"\",\n\t\t\t\t\"type\": \"address\"\n\t\t\t}\n\t\t],\n\t\t\"name\": \"purchases\",\n\t\t\"outputs\": [\n\t\t\t{\n\t\t\t\t\"internalType\": \"uint256\",\n\t\t\t\t\"name\": \"\",\n\t\t\t\t\"type\": \"uint256\"\n\t\t\t}\n\t\t],\n\t\t\"stateMutability\": \"view\",\n\t\t\"type\": \"function\"\n\t}\n]";
		
		public ShopContract(IWeb3ExternalService service, GameId currency, string contract)
		{
			_currency = currency;
			_service = service;
			_contract = contract;
			_contractInstance = new Contract(contract, ABI);
		}
		
		/// <summary>
		/// Claims a voucher which means consumes it from playfab and
		/// claims it on-chain instead.
		/// </summary>
		public TransactionWrapper PrepareBuyTransaction(TransactionWrapper tx, ItemData gameItem, BigInteger howMuch)
		{
			// TODO: BATCH TRANSACTIONS
			FLog.Verbose("Sending purchase intent for "+gameItem);
			howMuch = Web3Logic.ConvertToWei((decimal) howMuch);
			_service.GetCurrency(_currency).CurrencyContract.PrepareApprovalTransaction(tx, _contract, howMuch);
			var packedItem = Web3Logic.PackItem(gameItem);
			var itemId = (int) BitConverter.ToUInt16(packedItem.GameId);
			var metadata = new FixedByte(32, packedItem.Metadata.PadTo32Bytes());
			tx.EnqueueCall(_contractInstance.CallFunction("IntentPurchase", 
				itemId, 
				metadata,
				howMuch
			));
			return tx;
		}
	}
	

	[Event("PurchaseIntentCreated")]
	public class PurchaseIntentCreatedEvent : IEventDTO
	{
		[Parameter("address", "buyer", 1, true )]
		public virtual string Buyer { get; set; }
		
		[Parameter("uint64", "gameId", 2, false )]
		public virtual ulong GameId { get; set; }
		
		[Parameter("bytes32", "metadata", 3, false )]
		public virtual byte[] Metadata { get; set; }
		
		[Parameter("uint256", "price", 4, false )]
		public virtual BigInteger Price { get; set; }
	}
	
	[Event("PurchaseIntentCreated")]
	public class PurchaseIntentCreatedEventRaw : IEventDTO
	{
		[Parameter("address", "buyer", 1, true )]
		public virtual string Buyer { get; set; }
		
		[Parameter("uint64", "gameId", 2, false )]
		public virtual ulong GameId { get; set; }
		
		[Parameter("bytes32", "metadata", 3, false )]
		public virtual byte[] Metadata { get; set; }
		
		[Parameter("uint256", "price", 4, false )]
		public virtual BigInteger Price { get; set; }
	}
}