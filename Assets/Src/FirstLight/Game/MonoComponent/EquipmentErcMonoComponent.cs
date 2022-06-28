using FirstLight.Game.Data;
using UnityEngine;


namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage any erc renderable object 
	/// </summary>
	public interface IErcRenderable
	{
		/// <summary>
		/// Initialises a erc object given erc metadata <see cref="metadata"/>
		/// </summary>
		/// <param name="metadata"></param>
		public void Initialise(Erc721MetaData metadata);
	}
	
	/// <summary>
	/// This Mono component handles visual setup for a equipment item
	/// </summary>
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
		
		/// <summary>
		/// Initialise material and game objects based on metadata object 
		/// </summary>
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