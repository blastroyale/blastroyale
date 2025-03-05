using FirstLight.Game.Data;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public class VoucherCreatedMessage : IMessage
	{
		public Web3Voucher Voucher;
	}
	
	public class VoucherConsumedMessage : IMessage
	{
		public Web3Voucher Voucher;
	}
}