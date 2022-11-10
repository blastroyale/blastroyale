using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Forces the game state to become the given state.
	/// Requires admin permission on server.
	/// </summary>
	public struct ForceUpdateCommand : IGameCommand
	{
		public PlayerData PlayerData;
		public RngData RngData;
		public IdData IdData;
		public EquipmentData EquipmentData;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Admin;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			if (PlayerData != null)
			{
				PlayerData.CopyPropertiesShallowTo(ctx.Data.GetData<PlayerData>());
			}
			if (RngData != null)
			{
				RngData.CopyPropertiesShallowTo(ctx.Data.GetData<RngData>());
			}
			if (IdData != null)
			{
				IdData.CopyPropertiesShallowTo(ctx.Data.GetData<IdData>());
			}
			if (EquipmentData != null)
			{
				EquipmentData.CopyPropertiesShallowTo(ctx.Data.GetData<EquipmentData>());
			}
		}
	}
}