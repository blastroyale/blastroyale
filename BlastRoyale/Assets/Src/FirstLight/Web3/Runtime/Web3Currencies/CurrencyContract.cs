using System.Numerics;
using FirstLight.Game.Logic;
using Sequence;
using Sequence.Contracts;


namespace FirstLight.Web3.Runtime.SequenceContracts
{
	public class CurrencyContract
	{
		private ERC20 _erc20;
		private readonly IWeb3ExternalService _service;
		private string _contract;

		public CurrencyContract(IWeb3ExternalService service, string contract)
		{
			_service = service;
			_contract = contract;
			_erc20 = new ERC20(contract);
		}
		
		/// <summary>
		/// Approves another contract spending the currency in my behalf
		/// Generates a new transaction batch
		/// </summary>
		public TransactionWrapper PrepareApprovalTransaction(TransactionWrapper transaction, string contract, BigInteger amount)
		{
			transaction.EnqueueCall(_erc20.Approve(contract, amount));
			return transaction;
		}

		public TransactionWrapper PrepareTransferTransaction(TransactionWrapper transaction, string destination, BigInteger amount)
		{
			transaction.EnqueueCall(_erc20.Transfer(destination, Web3Logic.ConvertToWei((decimal)amount)));
			return transaction;
		}
	}
}