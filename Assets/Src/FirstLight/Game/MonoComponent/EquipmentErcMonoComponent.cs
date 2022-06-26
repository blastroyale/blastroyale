using FirstLight.Game.Data;
using UnityEngine;


namespace FirstLight.Game.MonoComponent
{
	public interface IErcRenderable
	{
		public void Initialise(Erc721MetaData metadata);
	}
	
	public class EquipmentErcMonoComponent : MonoBehaviour, IErcRenderable
	{
		[SerializeField] private GameObject[] _equipmentRarityGameObjects;
		[SerializeField] private EquipmentErcRenderableData _equipmentErcSpriteData;
		[SerializeField] private Renderer[] _renderers;
		
		private MaterialPropertyBlock _propBlock;
		
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
		
		public void Initialise(Erc721MetaData metadata)
		{
			var rarityId = metadata.attibutesDictionary["rarity"];
			var materialId = metadata.attibutesDictionary["material"];
			var adjectiveId = metadata.attibutesDictionary["adjective"];;
			
						
			_propBlock ??= new MaterialPropertyBlock();
						
			_propBlock.Clear();
			
			foreach (var r in _renderers)
			{
				r.GetPropertyBlock(_propBlock);
				
				_propBlock.SetTexture(_surfaceTextureId, _equipmentErcSpriteData.SurfaceTexture[materialId].texture);
				_propBlock.SetTexture(_adjectiveTextureId, _equipmentErcSpriteData.AdjectiveTexture[adjectiveId].texture);
				r.SetPropertyBlock(_propBlock, 0);	
			}

			if (_equipmentRarityGameObjects == null || _equipmentRarityGameObjects.Length <= 0)
			{
				return;
			}
			
			for (var i = 0; i <= rarityId; i++)
			{
				_equipmentRarityGameObjects[i].SetActive(true);
			}
		}
	}
}