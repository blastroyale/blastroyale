using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the summary part of the rewards screen
	/// </summary>
	public class RewardsSummaryView : UIView
	{
		private const float SKIP_ANIMATION_TIME = 0.2f;
		private const int MANY_REWARDS_AMOUNT = 5;
		private const string USS_REWARD_SUMMARY_CONTAINER_MANY_REWARDS_MODIFIER = "rewards-summary__rewards-container--manyrewards";
		private const string USS_FAME_REWARDS_SUMMARY = "rewards-summary--fame";
		
		private const string USS_AVATAR_NFT = "player-header__avatar--nft";

		private IGameDataProvider _dataProvider;
		private IGameServices _services;

		private RewardsAnimationController _animationController;
		private AnimatedBackground _animatedBackground;
		private PlayableDirector _animationDirector;

		private VisualElement _container;
		private VisualElement _avatar;
		private VisualElement _avatarPfp;

		private int _avatarRequestHandle = -1;

		public void Init(RewardsAnimationController animationController, AnimatedBackground animatedBackground, PlayableDirector animationDirector)
		{
			_animationController = animationController;
			_animatedBackground = animatedBackground;
			_animationDirector = animationDirector;
		}

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_container = element.Q<VisualElement>("RewardsContainer").Required();
			_avatar = element.Q("Avatar").Required();
			_avatarPfp = element.Q("AvatarPFP").Required();

			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.ResolveServices();

			UpdatePFP();
		}

		public override void UnsubscribeFromEvents()
		{
			_services.RemoteTextureService.CancelRequest(_avatarRequestHandle);
		}

		public void CreateSummaryElements(IList<IReward> rewards, bool fameRewards)
		{
			// Clean up example elements
			for (var i = _container.childCount - 1; i >= 0; i--)
			{
				_container.RemoveAt(i);
			}

			foreach (var dataReward in rewards)
			{
				if (dataReward is EquipmentReward er)
				{
					var eq = new EquipmentCardElement(er.Equipment, new UniqueId())
					{
						pickingMode = PickingMode.Ignore
					};
					_container.Add(eq);
					continue;
				}

				var el = new RewardSummaryItemElement(dataReward)
				{
					pickingMode = PickingMode.Ignore
				};
				_container.Add(el);
			}

			if (rewards.Count >= MANY_REWARDS_AMOUNT)
			{
				_container.AddToClassList(USS_REWARD_SUMMARY_CONTAINER_MANY_REWARDS_MODIFIER);
			}

			if (fameRewards)
			{
				Element.AddToClassList(USS_FAME_REWARDS_SUMMARY);
			}
		}

		private void UpdatePFP()
		{
			var avatarUrl = _dataProvider.AppDataProvider.AvatarUrl;
			if (string.IsNullOrEmpty(avatarUrl)) return;

			// DBG: Use random PFP
			// avatarUrl = avatarUrl.Replace("1.png", $"{Random.Range(1, 888)}.png");

			_avatar.SetVisibility(false);
			_avatar.AddToClassList(USS_AVATAR_NFT);
			_avatarRequestHandle = _services.RemoteTextureService.RequestTexture(
				avatarUrl,
				tex =>
				{
					_avatarPfp.style.backgroundImage = new StyleBackground(tex);
					_avatar.SetVisibility(true);
				},
				() =>
				{
					_avatar.RemoveFromClassList(USS_AVATAR_NFT);
					_avatar.SetVisibility(true);
				});
		}

		public void Show()
		{
			_animatedBackground.SetDefault();
			_animationController.StartAnimation(_animationDirector, SKIP_ANIMATION_TIME);
		}
	}
}