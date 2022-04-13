using System.Linq;
using FirstLight.Game.Data;
using Src.FirstLight.Modules.Services.Runtime.Tools;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.MonoComponent
{
	public class EquipmentCardMonoComponent : MonoBehaviour, IErcRenderable
	{
		[SerializeField] private TextMeshProUGUI _nameText;
		[SerializeField] private TextMeshProUGUI _gradeText;
		[SerializeField] private SpriteRenderer _factionSpriteRenderer;
		[SerializeField] private SpriteRenderer _factionShadowSpriteRenderer;
	
		[SerializeField] private EquipmentCardErcRenderableData _itemCardSpriteData;
		[SerializeField] private Renderer _renderer;

		private MaterialPropertyBlock _propBlock;
		private readonly string[] _gradeRomanNumerals = new[] { "I", "II", "III", "IV", "V" };
		private readonly int _backId = Shader.PropertyToID("_Back");
		private readonly int _frameId = Shader.PropertyToID("_Frame");
		private readonly int _frameShapeMaskId = Shader.PropertyToID("_FrameShapeMask");
		private readonly int _nameTagId = Shader.PropertyToID("_NameTag");
		private readonly int _adjectivePatternId = Shader.PropertyToID("_AdjectivePattern");
		private readonly int _plusAmountGradePatternId = Shader.PropertyToID("_PlusAmountGradePattern");
		
		private void Awake()
		{
			_propBlock = new MaterialPropertyBlock();
		}
		
		public void Initialise(Erc721Metadata metadata)
		{
			_propBlock ??= new MaterialPropertyBlock();
			
			_nameText.text = metadata.name;
			_gradeText.text = _gradeRomanNumerals[metadata.attibutesDictionary["grade"]];
			
			var factionId = metadata.attibutesDictionary["faction"];
			var rarityId = metadata.attibutesDictionary["rarity"];
			var adjectiveId = metadata.attibutesDictionary["adjective"];
			var materialId = metadata.attibutesDictionary["material"];
			
			_factionSpriteRenderer.sprite = _itemCardSpriteData.FactionSprites[factionId];
			_factionShadowSpriteRenderer.sprite = _itemCardSpriteData.FactionSprites[factionId];
			
			_propBlock.Clear();
			_renderer.GetPropertyBlock(_propBlock);
			
			_propBlock.SetTexture(_backId, _itemCardSpriteData.Back[materialId].Array[rarityId].texture);
			_propBlock.SetTexture(_frameId, _itemCardSpriteData.Frame[materialId].Array[rarityId].texture);
			_propBlock.SetTexture(_frameShapeMaskId, _itemCardSpriteData.FrameShapeMask[rarityId].texture);
			_propBlock.SetTexture(_nameTagId, _itemCardSpriteData.NameTag[materialId].texture);
			_propBlock.SetTexture(_adjectivePatternId, _itemCardSpriteData.AdjectivePattern[adjectiveId].texture);
			_propBlock.SetTexture(_plusAmountGradePatternId, _itemCardSpriteData.PlusAmountGradePattern[rarityId].texture);
			
			_renderer.SetPropertyBlock(_propBlock, 0);
		}
	}
}


