using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	public class ProfilePictureRewardSummaryItemElement : RewardSummaryItemElement
	{
		private int _requestTextureHandle = -1;
		
		public override RewardSummaryItemElement SetReward(IItemViewModel itemViewModel)
		{
			var services = MainInstaller.ResolveServices();
			services.RemoteTextureService.CancelRequest(_requestTextureHandle);
			
			var avatarCollectableConfigs = services.ConfigsProvider.GetConfig<AvatarCollectableConfig>();
			var avatarUrl = avatarCollectableConfigs.GameIdUrlDictionary[itemViewModel.GameId];
			
			_icon.style.backgroundImage = StyleKeyword.Null;
			
			_requestTextureHandle = services.RemoteTextureService.RequestTexture(
				avatarUrl, 
				tex =>
				{
					if (_icon != null && _icon.panel != null)
					{
						_icon.style.backgroundImage = new StyleBackground(tex);
					}
				},
				() =>
				{
					FLog.Error($"Could not retrieve remote texture for url: {avatarUrl}");
				});
			
			_label.text = itemViewModel.DisplayName;
			return this;
		}

		public new class UxmlFactory : UxmlFactory<CollectionRewardsSummaryElement, UxmlTraits>
		{
		}
	}
}