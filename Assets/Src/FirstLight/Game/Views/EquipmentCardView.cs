using FirstLight.Game.Data;
using FirstLight.Game.MonoComponent;
using Quantum;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This Mono component handles visual setup for a equipment card item
	/// </summary>
	public class EquipmentCardView : MonoBehaviour, IErcRenderable
	{
		[System.Serializable]
		public class SpriteDimensional
		{
			public Sprite[] Array;
		}
		
		public Sprite[] _factionSprites;
		public Sprite[] _frameSprites;
		public Sprite[] _frameShapeMasks;
		public Sprite[] _NameTagSprites;
		public Sprite[] _adjectivePatternSprites;

		
		[SerializeField] private TextMeshProUGUI _nameText;
		[SerializeField] private TextMeshProUGUI _gradeText;
		[SerializeField] private SpriteRenderer _factionSpriteRenderer;
		[SerializeField] private SpriteRenderer _factionShadowSpriteRenderer;
		[SerializeField] private Renderer _renderer;

		private MaterialPropertyBlock _propBlock;
		private readonly string[] _gradeRomanNumerals = new[] { "I", "II", "III", "IV", "V" };
		private readonly int _frameId = Shader.PropertyToID("_Frame");
		private readonly int _frameShapeMaskId = Shader.PropertyToID("_FrameShapeMask");
		private readonly int _nameTagId = Shader.PropertyToID("_NameTag");
		private readonly int _adjectivePatternId = Shader.PropertyToID("_AdjectivePattern");
		private readonly int _plusIndicatorId = Shader.PropertyToID("_Plus_Indicator");
		
		private void Awake()
		{
			_propBlock = new MaterialPropertyBlock();
		}

		public string Name
		{
			set => _nameText.text = value;
		}

		/// <summary>
		/// Initialise material and visual elements based on metadata object 
		/// </summary>
		public void Initialise(Equipment metadata)
		{
			_propBlock ??= new MaterialPropertyBlock();
			
			_gradeText.text = _gradeRomanNumerals[(int)metadata.Grade];

			var factionId = (int)metadata.Faction;
			var rarityId = (int)metadata.Rarity;
			var adjectiveId = (int)metadata.Adjective;
			var materialId = (int)metadata.Material;
			
			_factionSpriteRenderer.sprite = _factionSprites[factionId];
			_factionShadowSpriteRenderer.sprite = _factionSprites[factionId];
			
			_propBlock.Clear();
			_renderer.GetPropertyBlock(_propBlock);
			
			_propBlock.SetFloat(_plusIndicatorId, ((rarityId + 1) % 2) == 0 ? 1f : 0f);
			_propBlock.SetTexture(_frameId, _frameSprites[rarityId].texture);
			_propBlock.SetTexture(_frameShapeMaskId, _frameShapeMasks[rarityId].texture);
			_propBlock.SetTexture(_nameTagId, _NameTagSprites[materialId].texture);
			_propBlock.SetTexture(_adjectivePatternId, _adjectivePatternSprites[adjectiveId].texture);
			
			_renderer.SetPropertyBlock(_propBlock, 0);
		}
	}
}


