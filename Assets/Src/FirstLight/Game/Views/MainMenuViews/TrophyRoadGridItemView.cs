using FirstLight.Game.Commands;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Services;
using I2.Loc;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This script controls rewards on the Trophy Road screen.
	/// </summary>
	public class TrophyRoadGridItemView : GridItemBase<TrophyRoadGridItemView.TrophyRoadGridItemData>
	{
		public struct TrophyRoadGridItemData
		{
			public TrophyRoadRewardInfo Info;
		}
		
		[SerializeField, Required] private Button _button;
		[SerializeField, Required] private Slider _slider;
		[SerializeField, Required] private TextMeshProUGUI _levelText;
		[SerializeField, Required] private TextMeshProUGUI _rewardText;
		[SerializeField, Required] private TextMeshProUGUI _xpText;
		[SerializeField, Required] private TextMeshProUGUI _largeLevelNumberText;
		[SerializeField, Required] private TextMeshProUGUI _currentXpText;
		[SerializeField, Required] private TextMeshProUGUI _collectText;
		[SerializeField, Required] private Image _rewardImage;
		[SerializeField, Required] private Image _rewardClaimedImage;
		[SerializeField, Required] private Button _infoButton;
		[SerializeField, Required] private GameObject _newItemsHolder;
		[SerializeField, Required] private GameObject _currentXPHolder;
		[SerializeField, Required] private GameObject _sliderHandle;
		[SerializeField, Required] private Image _sliderHandleImage;
		[SerializeField, Required] private TrophyRoadUnlockView _smallCardRef;
		[SerializeField, Required] private TrophyRoadUnlockView _largeCardRef;
		[SerializeField, Required] private Animation _collectAnimation;
		[SerializeField, Required] private Image _rewardButtonImage;
		[SerializeField, Required] private Sprite _collectButtonSprite;
		[SerializeField, Required] private Sprite _rewardButtonSprite;

		private IObjectPool<TrophyRoadUnlockView> _smallCardPool;
		private IObjectPool<TrophyRoadUnlockView> _largeCardPool;
		private TrophyRoadGridItemData _data;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_smallCardPool = new GameObjectPool<TrophyRoadUnlockView>(3, _smallCardRef);
			_largeCardPool = new GameObjectPool<TrophyRoadUnlockView>(3, _largeCardRef);
			
			_smallCardRef.gameObject.SetActive(false);
			_largeCardRef.gameObject.SetActive(false);
			_sliderHandleImage.enabled = false;
			_button.onClick.AddListener(OnButtonClick);
			_infoButton.onClick.AddListener(OnInfoClick);
		}

		protected override void OnUpdateItem(TrophyRoadGridItemData data)
		{
			_data = data;
			_xpText.text = string.Format(ScriptLocalization.MainMenu.TrophyRoadXP, _data.Info.XpNeeded.ToString());
			_rewardClaimedImage.enabled = data.Info.IsCollected;
			
			_smallCardPool.DespawnAll();
			_largeCardPool.DespawnAll();

			ShowSliderValue();
			ShowRewards();
		}

		private void ShowSliderValue()
		{
			var playerInfo = _gameDataProvider.PlayerDataProvider.CurrentLevelInfo;

			if (playerInfo.Level == _data.Info.Level)
			{
				_rewardButtonImage.sprite = _rewardButtonSprite;
				_slider.value = (float) playerInfo.Xp / playerInfo.Config.LevelUpXP;
				_collectText.enabled = true;
				_collectText.text = ScriptLocalization.MainMenu.NextGoal;
				_currentXpText.text = string.Format(ScriptLocalization.MainMenu.TrophyRoadXP, playerInfo.TotalCollectedXp.ToString());
				_currentXPHolder.transform.position = new Vector3(_sliderHandle.transform.position.x, _currentXPHolder.transform.position.y, 0);
				
				_currentXPHolder.SetActive(true);
				_collectAnimation.Stop();
			}
			else if (_data.Info.IsReadyToCollect)
			{
				_rewardButtonImage.sprite = _collectButtonSprite;
				_slider.value = 1f;
				_collectText.enabled = true;
				_collectText.text = ScriptLocalization.MainMenu.Collect;
				
				_currentXPHolder.SetActive(false);
				_collectAnimation.Rewind();
				_collectAnimation.Play();
			}
			else
			{
				_rewardButtonImage.sprite = _rewardButtonSprite;
				_slider.value =  playerInfo.Level < _data.Info.Level ? 0f : 1f;
				_collectText.enabled = false;
				
				_currentXPHolder.SetActive(false);
				_collectAnimation.Stop();
			}
		}

		private async void ShowRewards()
		{
			var playerLevel = _gameDataProvider.PlayerDataProvider.Level.Value;
			var rewardId = _data.Info.Reward.RewardId;

			_levelText.text =  $"{ScriptLocalization.General.Level}: {_data.Info.Level.ToString()}"; ;
			_largeLevelNumberText.text = _data.Info.Level.ToString();
			_rewardText.text = rewardId.IsInGroup(GameIdGroup.LootBox)
				                   ? rewardId.GetTranslation()
				                   : $"{_data.Info.Reward.Value.ToString()} {rewardId.GetTranslation()}";

			_newItemsHolder.SetActive(_data.Info.UnlockedSystems.Count > 0);

			_infoButton.gameObject.SetActive(rewardId.IsInGroup(GameIdGroup.LootBox) && !_data.Info.IsReadyToCollect);
			
			for (var i = 0; i < _data.Info.UnlockedSystems.Count; i++)
			{
				_smallCardPool.Spawn().SetInfo(_newItemsHolder.transform, _data.Info.UnlockedSystems[i],
				                               ScriptLocalization.MainMenu.NewUnlock,
				                               _data.Info.UnlockedSystems[i].ToString().ToUpper(),
				                               playerLevel >= _data.Info.Level);
			}

			_rewardImage.enabled = true;
			_rewardImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(GetRewardSpriteId(rewardId));
		}

		private void OnButtonClick()
		{
			if (!_data.Info.IsReadyToCollect)
			{
				// OnInfoClick();
				return;
			}
			
			if (!_data.Info.IsCollected && _gameDataProvider.PlayerDataProvider.Level.Value >= _data.Info.Level)
			{
				_services.CommandService.ExecuteCommand(new CollectTrophyRoadRewardCommand { Level = _data.Info.Level } );
			}
		}

		private void OnInfoClick()
		{
			if (_data.Info.Reward.RewardId.IsInGroup(GameIdGroup.CoreBox))
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};

				var coreBoxInfo = _gameDataProvider.LootBoxDataProvider.GetLootBoxInfo(_data.Info.Reward.Value);
				
				_services.GenericDialogService.OpenLootInfoDialog(confirmButton, coreBoxInfo);
			}
		}
		

		private GameId GetRewardSpriteId(GameId rewardId)
		{
			if (rewardId == GameId.SC)
			{
				return GameId.ScBundle1;
			}
			
			if (rewardId == GameId.HC)
			{
				return GameId.HcBundle1;
			}

			return rewardId;
		}
	}
}

