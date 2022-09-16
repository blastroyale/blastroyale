using System;
using System.Collections.Generic;
using DG.Tweening;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// Interface for a renderers container MonoComponent for renderer visibility and customizing material behaviour for a given game object.
	/// </summary>
	public interface IRendersContainer
	{
		/// <summary>
		/// Set a material property float value using property id 
		/// </summary>
		void SetMaterialPropertyValue(int propertyId, float value);
		/// <summary>
		/// Set a material property float value over duration time to an end value and back to start value
		/// </summary>
		void SetMaterialPropertyValue(int propertyId, float startValue, float endValue, float duration);
		/// <summary>
		/// Set enabled flag of all renderers in the container
		/// </summary>
		void SetRendererState(bool visible);
		/// <summary>
		/// Set's the given <paramref name="materialId"/> to all <see cref="Renderer"/> in this game object and his children.
		/// If the given <paramref name="keepTexture"/> is true, then will keep original texture from the rendered.
		/// </summary>
		void SetMaterial(MaterialVfxId materialId, ShadowCastingMode mode, bool keepTexture);
		/// <summary>
		/// Set's the material using the given <paramref name="materialResolver"/> to all <see cref="Renderer"/> in this
		/// game object and his children.
		/// If the given <paramref name="keepTexture"/> is true, then will keep original texture from the rendered.
		/// </summary>
		void SetMaterial(Func<int, Material> materialResolver, ShadowCastingMode mode, bool keepTexture);
		/// <summary>
		/// Disables all particle systems
		/// </summary>
		public void DisableParticles();
		/// <summary>
		/// Resets all this game object and his children <see cref="Renderer"/> to their original materials
		/// </summary>
		void ResetToOriginalMaterials();
	}
	
	/// <summary>
	/// This MonoComponent acts as a container of all <see cref="Renderer"/> inside of this GameObject, inclusive the
	/// <see cref="Renderer"/> that this game object might contain.
	/// It keeps track of the original <see cref="Material"/> state of the objects and allows to reset it via <seealso cref="ResetToOriginalMaterials"/>.
	/// </summary>
	public class RenderersContainerMonoComponent : MonoBehaviour, IRendersContainer
	{
		private static readonly int _mainText = Shader.PropertyToID("_MainTex");
		
		[SerializeField] private List<Material> _originalMaterials = new List<Material>();
		[SerializeField] private List<Renderer> _renderers = new List<Renderer>();
		[SerializeField] private List<Renderer> _particleRenderers = new List<Renderer>();
		[SerializeField] private List<GameObject> _rendererRoots = new List<GameObject>();
		[SerializeField] private Renderer _mainRenderer;
		
		private IGameServices _services;
		private MaterialPropertyBlock _propBlock;
		
		/// <summary>
		/// A readonly list of all the original <see cref="Material"/> when this object was created
		/// </summary>
		public IReadOnlyList<Material> OriginalMaterials => _originalMaterials;
		/// <summary>
		/// A readonly list of all the <see cref="Renderer"/> inside of this game object (this game object and all it's children)
		/// </summary>
		public IReadOnlyList<Renderer> Renderers => _renderers;
		/// <summary>
		/// A readonly list of all the <see cref="Renderer"/> of particles inside of this game object (this game object and all it's children)
		/// </summary>
		public IReadOnlyList<Renderer> ParticleRenderers => _particleRenderers;
		/// <summary>
		/// The <see cref="Renderer"/> that the game object containing this script might contain.
		/// If this game object does not contain a <see cref="Renderer"/> then it will return null.
		/// </summary>
		public Renderer MainRenderer => _mainRenderer;

		private void OnValidate()
		{
			_mainRenderer = _mainRenderer ? _mainRenderer : GetComponent<Renderer>();

			var renderers = GetComponentsInChildren<Renderer>(true);
			
			_particleRenderers.Clear();
			_renderers.Clear();
			_originalMaterials.Clear();
			
			foreach (var render in renderers)
			{
				if (render is ParticleSystemRenderer || render.TryGetComponent(typeof(VisualEffect), out _))
				{
					_particleRenderers.Add(render);
					continue;
				}
				
				_renderers.Add(render);
				
				foreach (var material in render.sharedMaterials)
				{
					_originalMaterials.Add(material);
				}
			}
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_propBlock = new MaterialPropertyBlock();
		}
		
		/// <inheritdoc />
		public void SetMaterialPropertyValue(int propertyId, float value)
		{
			foreach (var rendererItem in _renderers)
			{
				_propBlock.Clear();
				rendererItem.GetPropertyBlock(_propBlock);
				_propBlock.SetFloat(propertyId, value);
				
				var materialCount = rendererItem.materials.Length;

				for (var j = 0; j < materialCount; j++)
				{
					var material = rendererItem.materials[j];

					if (!material.HasProperty(propertyId))
					{
						continue;
					}
					
					rendererItem.SetPropertyBlock(_propBlock, j);
				}
			}
		}
		
		/// <inheritdoc />
		public void SetMaterialPropertyValue(int propertyId, float startValue, float endValue, float duration)
		{
			foreach (var rendererItem in _renderers)
			{
				var materialCount = rendererItem.materials.Length;

				for (var j = 0; j < materialCount; j++)
				{
					var material = rendererItem.materials[j];
					
					if (!material.HasProperty(propertyId))
					{
						continue;
					}
					
					material.DOKill();
					material.DOFloat(endValue, propertyId, duration).OnComplete(() =>
					{
						material.DOFloat(startValue, propertyId, duration).SetAutoKill(true);
					}).SetAutoKill(true);
				}
			}
		}
		
		/// <inheritdoc />
		public void SetRendererState(bool visible)
		{
			foreach (var render in _renderers)
			{
				render.enabled = visible;
			}
			foreach (var render in _particleRenderers)
			{
				render.enabled = visible;
			}
			foreach (var render in _rendererRoots)
			{
				render.SetActive(visible);
			}
		}
		
		/// <inheritdoc />
		public async void SetMaterial(MaterialVfxId materialId, ShadowCastingMode mode, bool keepTexture)
		{
			var mat = await _services.AssetResolverService.RequestAsset<MaterialVfxId, Material>(materialId);
			
			// In case the loading takes more time then expected
			if (this.IsDestroyed())
			{
				Destroy(mat);
				return;
			}
			
			SetMaterial(i => mat, mode, keepTexture);
		}
		
		/// <inheritdoc />
		public void SetMaterial(Func<int, Material> materialResolver, ShadowCastingMode mode, bool keepTexture)
		{
			// Original Materials have all the materials in order of the renderers
			for (int i = 0, count = 0; i < _renderers.Count; i++)
			{
				var materialCount = _renderers[i].materials.Length;
				var newMaterials = new Material[materialCount];

				for (var j = 0; j < materialCount; j++, count++)
				{
					var material = materialResolver(count);

					if (keepTexture && _originalMaterials[count].HasProperty(_mainText))
					{
						material.SetTexture(_mainText, _originalMaterials[count].GetTexture(_mainText));
					}

					newMaterials[j] = material;
				}

				_renderers[i].materials = newMaterials;
				_renderers[i].shadowCastingMode = mode;
			}
		}

		/// <inheritdoc />
		public void DisableParticles()
		{
			foreach (var render in _particleRenderers)
			{
				render.enabled = false;
			}
		}
		
		/// <inheritdoc />
		public void ResetToOriginalMaterials()
		{
			SetMaterial(i => _originalMaterials[i], ShadowCastingMode.On, false);
		}
	}
}