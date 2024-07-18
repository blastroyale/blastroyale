using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Represents the current activity / status of a player.
	/// Used by the Friends API.
	/// </summary>
	[Preserve]
	[DataContract]
	public class FriendActivity
	{
		/// <summary>
		/// Status of the player.
		/// </summary>
		[Preserve]
		[DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
		public string Status { get; set; }
		
		[Preserve]
		[DataMember(Name = "avatar", IsRequired = true, EmitDefaultValue = true)]
		public string AvatarUrl { get; set; }
	}
}