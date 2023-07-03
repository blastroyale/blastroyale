using System;
using System.Collections.Generic;
using System.Linq;
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
		/// Sets the layer of the renderers.
		/// </summary>
		void SetRenderersLayer(int layer);

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
		
		private readonly List<List<Material>> _originalMaterialsPerRenderer = new();

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
			_originalMaterialsPerRenderer.Clear();

			foreach (var render in renderers)
			{
				if (render is ParticleSystemRenderer || render.TryGetComponent(typeof(VisualEffect), out _))
				{
					_particleRenderers.Add(render);
					continue;
				}

				_renderers.Add(render);

				for (var i = 0; i < render.sharedMaterials.Length; i++)
				{
					if (i == 0)
					{
						_originalMaterials.Add(render.sharedMaterials[i]);
					}
				}
			}
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_propBlock = new MaterialPropertyBlock();

			foreach (var r in _renderers)
			{
				_originalMaterialsPerRenderer.Add(r.sharedMaterials.ToList());
			}
		}

		public void SetMaterialPropertyValue(int propertyId, float value)
		{
			foreach (var rendererItem in _renderers)
			{
				_propBlock.Clear();
				rendererItem.GetPropertyBlock(_propBlock);
				_propBlock.SetFloat(propertyId, value);

				var materialCount = Math.Min(rendererItem.materials.Length, 1);

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

		public void SetMaterialPropertyValue(int propertyId, float startValue, float endValue, float duration)
		{
			foreach (var rendererItem in _renderers)
			{
				var materialCount = Math.Min(rendererItem.materials.Length, 1);

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

		// TODO: Avoid duplicating the material
		// https://tree.taiga.io/project/firstlightgames-blast-royale-reloaded/task/334
		public void SetRendererColor(Color c)
		{
			foreach (var render in _renderers)
			{
				render.material.color = c;
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
				var sharedMaterialCount = _renderers[i].sharedMaterials.Length;
				var materialCount = Math.Min(_renderers[i].materials.Length, 1);

				var newMaterials = new List<Material>(3);

				for (var j = 0; j < materialCount; j++, count++)
				{
					var material = materialResolver(count);

					if (keepTexture && _originalMaterials[count].HasProperty(_mainText))
					{
						material.SetTexture(_mainText, _originalMaterials[count].GetTexture(_mainText));
					}

					newMaterials.Add(material);
				}

				if (sharedMaterialCount > 1)
				{
					newMaterials.Add(_renderers[i].sharedMaterials[^1]);
				}

				_renderers[i].materials = newMaterials.ToArray();
				_renderers[i].shadowCastingMode = mode;
			}
		}

		public void SetRenderersLayer(int layer)
		{
			foreach (var r in _renderers)
			{
				r.gameObject.layer = layer;
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
			if (_originalMaterialsPerRenderer is {Count: > 0})
			{
				for (var i = 0; i < _renderers.Count; i++)
				{
					_renderers[i].sharedMaterials = _originalMaterialsPerRenderer[i].ToArray();
				}
			}
			else
			{
				SetMaterial(i => _originalMaterials[i], ShadowCastingMode.On, false);
			}
		}
	}
}