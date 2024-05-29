using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Represents a message that was sent by a player.
	/// Used by the Friends API.
	/// </summary>
	[Preserve]
	[DataContract]
	public class PlayerMessage
	{
		/// <summary>
		/// The message that was sent
		/// </summary>
		[Preserve]
		[DataMember(Name = "message", IsRequired = true, EmitDefaultValue = true)]
		public string Message { get; set; }
	}
}