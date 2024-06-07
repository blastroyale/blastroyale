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
		/// The ID of the squad to join.
		/// </summary>
		[Preserve]
		[DataMember(Name = "squad_code", IsRequired = false, EmitDefaultValue = true)]
		public string SquadCode { get; private set; }

		public static PlayerMessage CreateSquadInvite(string squadCode)
		{
			return new PlayerMessage
			{
				SquadCode = squadCode
			};
		}
	}
}