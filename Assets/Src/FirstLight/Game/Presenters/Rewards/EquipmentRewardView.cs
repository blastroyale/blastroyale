using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.UiService;
using Quantum;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the equipment reward part of the rewards screen
	/// </summary>
	public class EquipmentRewardView : UIView
	{
		private const float SKIP_ANIMATION_TIME = 2.5f;
		private const string USS_REWARD_EQUIPMENT_BLOCK = "rewards-equipment";
		private const string USS_GRADIENT_MODIFIER_FORMAT = USS_REWARD_EQUIPMENT_BLOCK + "__gradient--{0}";
		private const string USS_RAYS_MODIFIER_FORMAT = USS_REWARD_EQUIPMENT_BLOCK + "__rays--{0}";
		private const string USS_LABEL_MODIFIER_FORMAT = USS_REWARD_EQUIPMENT_BLOCK + "__tag-label--{0}";

		private RewardsAnimationController _animationController;
		private AnimatedBackground _animatedBackground;
		private PlayableDirector _animationDirector;
		private IConfigsProvider _configsProvider;

		private EquipmentCardElement _card;
		private Label _name;
		private Label _rarity;
		private LocalizedLabel _range;
		private VisualElement _icon;
		private VisualElement _rays;
		private VisualElement _gradient;
		private VisualElement _gradientStronger;

		public void Init(RewardsAnimationController animationController, AnimatedBackground animatedBackground, PlayableDirector animationDirector)
		{
			_animationController = animationController;
			_animatedBackground = animatedBackground;
			_animationDirector = animationDirector;
			_configsProvider = MainInstaller.ResolveServices().ConfigsProvider;
		}

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_card = element.Q<EquipmentCardElement>("EquipmentRewardCard").Required();
			_icon = element.Q<VisualElement>("EquipmentIcon").Required();
			_name = element.Q<Label>("EquipmentName").Required();
			_rarity = element.Q<Label>("EquipmentRarityLabel").Required();
			_range = element.Q<LocalizedLabel>("EquipmentRangeLabel").Required();
			_gradient = element.Q<VisualElement>("RaysGradient").Required();
			_gradientStronger = element.Q<VisualElement>("RaysStrongerGradient").Required();
			_rays = element.Q<VisualElement>("RaysEquipment").Required();
		}


		internal void ShowEquipment(EquipmentReward reward)
		{
			var rarityLower = reward.Equipment.Rarity.ToString().ToLowerInvariant().Replace("plus", "");

			_animatedBackground.SetColorByRarity(reward.Equipment.Rarity);
			_card.SetEquipment(reward.Equipment, new UniqueId());

			if (reward.GameId.IsInGroup(GameIdGroup.Weapon))
			{
				var isRanged = _configsProvider.GetConfig<QuantumWeaponConfig>((int) reward.GameId).UseRangedCam;
				_range.Localize(isRanged ? "UITRewards/long_range" : "UITRewards/short_range");
				_range.SetDisplay(true);
			}
			else
			{
				_range.SetDisplay(false);
			}

			_rarity.text = reward.Equipment.Rarity.GetLocalization();
			_name.text = reward.Equipment.GameId.GetLocalization();

			_rarity.RemoveModifiers();
			_gradient.RemoveModifiers();
			_gradientStronger.RemoveModifiers();
			_rays.RemoveModifiers();

			_rarity.AddToClassList(string.Format(USS_LABEL_MODIFIER_FORMAT, rarityLower));
			_gradient.AddToClassList(string.Format(USS_GRADIENT_MODIFIER_FORMAT, rarityLower));
			_gradientStronger.AddToClassList(string.Format(USS_GRADIENT_MODIFIER_FORMAT, rarityLower));
			_rays.AddToClassList(string.Format(USS_RAYS_MODIFIER_FORMAT, rarityLower));
#pragma warning disable CS4014
			// Ignore task return because it only loads the sprite and we don't want to wait for it
			UIUtils.SetSprite(reward.GameId, _icon);
#pragma warning restore CS4014
			_animationController.StartAnimation(_animationDirector, SKIP_ANIMATION_TIME);
		}
	}
}