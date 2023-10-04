using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Profile picture collection items view model
	/// </summary>
	public class ProfilePictureViewModel : IItemViewModel
	{
		private int _requestTextureHandle = -1;
		
		public GameId GameId { get; }
		public uint Amount => 1;
		public string DisplayName { get; }
		
		public VisualElement ItemCard => new ProfilePictureRewardSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public void LegacyRenderSprite(VisualElement icon, Label name, Label amount)
		{
			icon.style.backgroundImage = StyleKeyword.Null;
			var services = MainInstaller.ResolveServices();
			services.RemoteTextureService.CancelRequest(_requestTextureHandle);
			
			var avatarCollectableConfigs = services.ConfigsProvider.GetConfig<AvatarCollectableConfig>();
			var avatarUrl = avatarCollectableConfigs.GameIdUrlDictionary[GameId];
				
			_requestTextureHandle = services.RemoteTextureService.RequestTexture(
				avatarUrl, 
				tex =>
				{
					if (icon != null && icon.panel != null)
					{
						icon.style.backgroundImage = new StyleBackground(tex);
					}
				},
				() =>
				{
					FLog.Error($"Could not retrieve remote texture for url: {avatarUrl}");
				});
			
			name.text = DisplayName;
		}
		
		public ProfilePictureViewModel(ItemData item)
		{
			GameId = item.Id;
			DisplayName = GameId.GetLocalization();
		}
	}
}