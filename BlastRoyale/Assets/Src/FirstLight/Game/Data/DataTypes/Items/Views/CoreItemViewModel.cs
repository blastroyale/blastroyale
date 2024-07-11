using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Holder for item generators (e.g cores)
	/// </summary>
	public class CoreItemViewModel : IItemViewModel
	{
		public ItemData Item { get; }
		public GameId GameId => _gameId;
		public uint Amount => 1;
		public string DisplayName => GameId.GetLocalization().ToUpperInvariant();
		public VisualElement ItemCard => new GameIdIconSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public string ItemTypeDisplayName => GameIdGroup.Core.GetGameIdGroupLocalization();
		
		public void DrawIcon(VisualElement icon)
		{
			FLog.Verbose("Drawing CoreItem icon");
			DrawIconAsync(icon).Forget();
		}

		private async UniTaskVoid DrawIconAsync(VisualElement icon)
		{
			await UniTask.NextFrame();
			icon.RemoveSpriteClasses();
			icon.style.backgroundImage = StyleKeyword.Null;
#pragma warning disable CS4014
			UIUtils.SetSprite(GameId, icon);
#pragma warning restore CS4014
		}

		public string Description => null;

		private GameId _gameId;
		public CoreItemViewModel(ItemData item)
		{
			Item = item;
			_gameId = item.Id;
		}
	}
}