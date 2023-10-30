using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A single item in the summary part of the RewardsScreen
	/// </summary>
	public class UnlockRewardSummaryElement : RewardSummaryItemElement
	{
		/// <summary>
		/// Update the current displayed reward
		/// </summary>
		public override RewardSummaryItemElement SetReward(IItemViewModel itemViewModel)
		{
			_icon.style.backgroundImage = StyleKeyword.Null;
			_icon.RemoveSpriteClasses();
			var ur = itemViewModel as UnlockItemViewModel;
			switch (ur.UnlockSystem)
			{
				// TODO: Make this more dynamic
				case UnlockSystem.Shop:
					_icon.AddToClassList("sprite-home__icon-shop");
					break;
				case UnlockSystem.Collection:
					_icon.AddToClassList("sprite-home__icon-heroes");
					break;
				case UnlockSystem.Leaderboards:
					_icon.AddToClassList("sprite-home__icon-leaderboards");
					break;
				case UnlockSystem.Equipment:
					_icon.AddToClassList("sprite-home__icon-equipment");
					break;
				case UnlockSystem.GameModes:
					//TODO: use a unique or seperate icon here to represent game mdoes
					_icon.AddToClassList("sprite-home__icon-marketplace");
					break;
			}
			_label.text = ur.DisplayName;
			return this;
		}

		public new class UxmlFactory : UxmlFactory<UnlockRewardSummaryElement, UxmlTraits>
		{
		}
	}
}