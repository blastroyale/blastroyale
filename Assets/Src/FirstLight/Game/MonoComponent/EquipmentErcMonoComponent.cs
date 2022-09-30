using System.Threading.Tasks;
using Quantum;
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
		public Task Initialise(Equipment metadata);
	}
	
	/// <summary>
	/// This Mono component handles visual setup for a equipment item
	/// </summary>
	public class EquipmentErcMonoComponent : MonoBehaviour, IErcRenderable
	{
		[SerializeField] private GameObject[] _equipmentRarityGameObjects;
		[SerializeField] private Sprite[] _surfaceTextures;
		[SerializeField] private Sprite[] _adjectiveTextures;
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
		public Task Initialise(Equipment metadata)
		{
			_propBlock ??= new MaterialPropertyBlock();
						
			_propBlock.Clear();
			
			foreach (var r in _renderers)
			{
				r.GetPropertyBlock(_propBlock);
				
				_propBlock.SetTexture(_surfaceTextureId, _surfaceTextures[(int)metadata.Material].texture);
				_propBlock.SetTexture(_adjectiveTextureId, _adjectiveTextures[(int)metadata.Adjective].texture);
				r.SetPropertyBlock(_propBlock, 0);	
			}

			if (_equipmentRarityGameObjects == null || _equipmentRarityGameObjects.Length <= 0)
			{
				return null;
			}
			
			for (var i = 0; i <= (int)metadata.Rarity; i++)
			{
				_equipmentRarityGameObjects[i].SetActive(true);
			}

			return null;
		}
	}
}