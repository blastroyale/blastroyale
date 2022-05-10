using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Utils;
using FirstLight.Game.Infos;
using FirstLight.Game.Services;
using Quantum;
using Sirenix.OdinInspector;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class handles each individual card reward shown at the end of the CollectLootRewardState, during the summary sequence.
	/// </summary>
	public class CollectedLootView : MonoBehaviour
	{
		[SerializeField, Required] private DOTweenAnimation _doTweenAnimation;
		[SerializeField, Required] private TextMeshProUGUI LevelText;
		[SerializeField, Required] private TextMeshProUGUI QuantityText;
		[SerializeField, Required] private TextMeshProUGUI ItemText;
		[SerializeField, Required] private Image IconImage;
		[SerializeField, Required] private Image _rarityImage;
		[SerializeField, Required] private Image _autoFireIcon;
		[SerializeField, Required] private Image _manualFireIcon;
		
		private IGameServices _services;
		
		/// <summary>
		/// Set Loot information here; Rarity, Level, Quantity, etc.
		/// </summary>
		public async void SetInfo(EquipmentDataInfo info)
		{
			_services ??= MainInstaller.Resolve<IGameServices>();
			
			ItemText.text = info.GameId.GetTranslation();
			LevelText.text = $"LV {info.Data.Level.ToString()}";
			QuantityText.enabled = false;
			IconImage.enabled = false;
			_rarityImage.sprite = await _services.AssetResolverService.RequestAsset<ItemRarity, Sprite>(info.Data.Rarity);
			IconImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(info.GameId);
			IconImage.enabled = true;
		}

		/// <summary>
		/// Plays a tween to show the item has been collected. Used in the summary sequence with a delay for a "mexican wave" style effect.
		/// </summary>
		public void PlayCollectedTween(float delay, UnityAction onFinishedCallback)
		{
			_doTweenAnimation.hasOnComplete = true;
			_doTweenAnimation.delay = delay;
			_doTweenAnimation.onComplete.RemoveAllListeners();
			_doTweenAnimation.onComplete.AddListener(onFinishedCallback);
			_doTweenAnimation.CreateTween();
			_doTweenAnimation.tween.Play();
		}
	}
}


