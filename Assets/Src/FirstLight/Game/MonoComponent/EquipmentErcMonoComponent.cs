using System.Linq;
using FirstLight.Game.Data;
using Src.FirstLight.Modules.Services.Runtime.Tools;
using UnityEngine;


namespace FirstLight.Game.MonoComponent
{
	public class EquipmentErcMonoComponent : MonoBehaviour, IErcRenderable
	{
		[SerializeField] private GameObject[] _equipmentRarityGameObjects;
		[SerializeField] private EquipmentErcRenderableData _equipmentErcSpriteData;
		[SerializeField] private Renderer[] _renderers;
		
		private MaterialPropertyBlock _propBlock;
		
		private readonly int _surfaceEnableId = Shader.PropertyToID("_SURFACEENABLE");
		private readonly int _surfaceTextureId = Shader.PropertyToID("_SurfaceTexture");
		private readonly int _adjectiveTextureId = Shader.PropertyToID("_AdjectiveTexture");
		
		private void Awake()
		{
			_propBlock = new MaterialPropertyBlock();
		}
		
		private void OnValidate()
		{
			_renderers = GetComponentsInChildren<Renderer>(true);
		}
		
		public void Initialise(Erc721Metadata metadata)
		{
			var rarityId = metadata.attributes.FirstOrDefault(a => a.trait_type == "rarity").value;
			var materialId = metadata.attributes.FirstOrDefault(a => a.trait_type == "material").value;
			var adjectiveId = metadata.attributes.FirstOrDefault(a => a.trait_type == "adjective").value;
			
						
			_propBlock ??= new MaterialPropertyBlock();
						
			_propBlock.Clear();

			foreach (var r in _renderers)
			{
				r.GetPropertyBlock(_propBlock);
				
				_propBlock.SetFloat(_surfaceEnableId, 1);
				_propBlock.SetTexture(_surfaceTextureId, _equipmentErcSpriteData.SurfaceTexture[materialId].texture);
				_propBlock.SetTexture(_adjectiveTextureId, _equipmentErcSpriteData.AdjectiveTexture[adjectiveId].texture);

				r.SetPropertyBlock(_propBlock, 0);	
			}
			
			for (var i = 0; i <= rarityId; i++)
			{
				_equipmentRarityGameObjects[i].SetActive(true);
			}

		}
	}
}