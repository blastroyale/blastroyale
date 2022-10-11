using System.Threading.Tasks;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This Mono component handles visual setup for a equipment card item
	/// </summary>
	public class EquipmentCardView : MonoBehaviour, IErcRenderable
	{
		public Sprite[] _factionSprites;
		public Sprite[] _frameSprites;
		public Sprite[] _frameShapeMasks;
		public Sprite[] _NameTagSprites;
		public Sprite[] _adjectivePatternSprites;

		[SerializeField] private TextMeshProUGUI _nameText;
		[SerializeField] private TextMeshProUGUI _gradeText;
		[SerializeField] private Image _factionSpriteRenderer;
		[SerializeField] private Image _factionShadowSpriteRenderer;
		[SerializeField] private Image _itemIcon;
		[SerializeField] private RawImage _card;

		private readonly string[] _gradeRomanNumerals = {"I", "II", "III", "IV", "V"};
		private readonly int _frameId = Shader.PropertyToID("_Frame");
		private readonly int _frameShapeMaskId = Shader.PropertyToID("_FrameShapeMask");
		private readonly int _nameTagId = Shader.PropertyToID("_NameTag");
		private readonly int _adjectivePatternId = Shader.PropertyToID("_AdjectivePattern");
		private readonly int _plusIndicatorId = Shader.PropertyToID("_Plus_Indicator");

		private Material _cardMat;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_cardMat = _card.material = Instantiate(_card.material);
		}

		/// <summary>
		/// Initialise material and visual elements based on metadata object 
		/// </summary>
		public async Task Initialise(Equipment metadata)
		{
			_nameText.text = LocalizationManager.GetTranslation($"GameIds/{metadata.GameId.ToString()}");

			_gradeText.text = _gradeRomanNumerals[(int) metadata.Grade];

			var factionId = (int) metadata.Faction;
			var rarityId = (int) metadata.Rarity;
			var adjectiveId = (int) metadata.Adjective;
			var materialId = (int) metadata.Material;

			_factionSpriteRenderer.sprite = _factionSprites[factionId];
			_factionShadowSpriteRenderer.sprite = _factionSprites[factionId];

			_cardMat.SetFloat(_plusIndicatorId, ((rarityId + 1) % 2) == 0 ? 1f : 0f);
			_cardMat.SetTexture(_frameId, _frameSprites[rarityId].texture);
			_cardMat.SetTexture(_frameShapeMaskId, _frameShapeMasks[rarityId].texture);
			_cardMat.SetTexture(_nameTagId, _NameTagSprites[materialId].texture);
			_cardMat.SetTexture(_adjectivePatternId, _adjectivePatternSprites[adjectiveId].texture);
			
			// Refreshes the material
			_card.enabled = false;
			_card.enabled = true;

			_itemIcon.sprite =
				await _services.AssetResolverService.RequestAsset<GameId, Sprite>(metadata.GameId, true, false);
		}
	}
}