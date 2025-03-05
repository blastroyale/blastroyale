using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Quantum;

namespace FirstLight.Web3.Runtime
{
	using System;

	/// <summary>
	/// Compact serializer for blockchain
	/// Blockchain works in 32 byte pages, we need to serialize the item into 32 bytes no matter what from and to hex format.
	/// </summary>
	public static class Web3Serializer
	{
		
	}
}