using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// Handles a specific reward object in the main menu. A reward can be a currency or piece of equipment.
	/// </summary>
	public class MainMenuRewardView : MonoBehaviour
	{
		[SerializeField, Required] private Image _backgroundImage;
		[SerializeField, Required] private Image _rewardIconImage;
		[SerializeField, Required] private TextMeshProUGUI _itemName;
		[SerializeField, Required] private Button _button;
		[SerializeField, Required] private Image _infoIcon;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private GameId _gameId;
		private int _rewardValue;
		
		/// <summary>
		/// Initializes the object with the given reward <paramref name="gameId"/> & reward <paramref name="value"/> to show
		/// </summary>
		public async void Initialise(GameId gameId, int value)
		{
			_services ??= MainInstaller.Resolve<IGameServices>();
			_gameDataProvider ??= MainInstaller.Resolve<IGameDataProvider>();
			_gameId = gameId;
			_rewardValue = value;
			
			gameObject.SetActive(true);

			_backgroundImage.enabled = !gameId.IsInGroup(GameIdGroup.LootBox);
			_infoIcon.enabled = gameId.IsInGroup(GameIdGroup.LootBox);
			
			if (value > 0 && gameId.IsInGroup(GameIdGroup.Currency) )
			{
				_itemName.text = value.ToString();
			}
			else
			{
				_itemName.text = value == -1 ? "???" : "";
			}

			_rewardIconImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(GetRewardSpriteId(gameId), false);
			_button?.onClick.AddListener(OnButtonClick);
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

		private void OnButtonClick()
		{
			if (_gameId.IsInGroup(GameIdGroup.CoreBox))
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};

				var coreBoxInfo = _gameDataProvider.LootBoxDataProvider.GetLootBoxInfo(_rewardValue);
				_services.GenericDialogService.OpenLootInfoDialog(confirmButton, coreBoxInfo);
			}
		}
		
	}
}

