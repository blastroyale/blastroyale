using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.VFX;
using ReadOnlyOdin = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// Interface for a renderers container MonoComponent for renderer visibility and customizing material behaviour for a given game object.
	/// </summary>
	public interface IRendersContainer
	{
		/// <summary>
		/// Set enabled flag of all renderers in the container
		/// </summary>
		void SetEnabled(bool enabled);

		/// <summary>
		/// Set's the given <paramref name="materialId"/> to all <see cref="Renderer"/> in this game object and his children.
		/// If the given <paramref name="keepTexture"/> is true, then will keep original texture from the rendered.
		/// </summary>
		void SetMaterial(MaterialVfxId materialId, bool keepTexture);

		/// <summary>
		/// Set's the material using the given <paramref name="material"/> to all <see cref="Renderer"/> in this
		/// game object and his children.
		/// If the given <paramref name="keepTexture"/> is true, then will keep original texture from the rendered.
		/// </summary>
		void SetMaterial(Material material, bool keepTexture);

		/// <summary>
		/// Sets the color of the renderers.
		/// </summary>
		void SetColor(Color color);
		
		/// <summary>
		/// Sets the additive color of the renderers.
		/// </summary>
		void SetAdditiveColor(Color color);

		/// <summary>
		/// Sets the layer of the renderers.
		/// </summary>
		void SetLayer(int layer);

		/// <summary>
		/// Resets all this game object and his children <see cref="Renderer"/> to their original materials
		/// </summary>
		void ResetMaterials();
	}

	/// <summary>
	/// This MonoComponent acts as a container of all <see cref="Renderer"/> inside of this GameObject, inclusive the
	/// <see cref="Renderer"/> that this game object might contain.
	/// It keeps track of the original <see cref="Material"/> state of the objects and allows to reset it via <seealso cref="ResetMaterials"/>.
	/// </summary>
	public class RenderersContainerMonoComponent : MonoBehaviour, IRendersContainer
	{
		private static readonly int _mainTex = Shader.PropertyToID("_MainTex");
		private static readonly int _additiveColor = Shader.PropertyToID("_AdditiveColor");
		private static readonly int _color = Shader.PropertyToID("_Color");
		
		[SerializeField, ReadOnlyOdin] private List<Renderer> _renderers = new();
		[SerializeField, ReadOnlyOdin] private List<Renderer> _particleRenderers = new();
		[SerializeField, ReadOnlyOdin] private List<Material> _originalMaterials = new();
		[SerializeField, ReadOnlyOdin] private List<Color> _rendererColors = new();
		[SerializeField, ReadOnlyOdin] private List<Color> _rendererAdditiveColors = new();
		
		private IGameServices _services;

		private void OnValidate()
		{
			UpdateRenderers();
		}

		public void UpdateRenderers()
		{
			var renderers = GetComponentsInChildren<Renderer>(true);

			_particleRenderers.Clear();
			_renderers.Clear();
			_originalMaterials.Clear();
			_rendererColors.Clear();
			_rendererAdditiveColors.Clear();

			foreach (var r in renderers)
			{
				if (r is ParticleSystemRenderer || r.TryGetComponent(typeof(VisualEffect), out _))
				{
					_particleRenderers.Add(r);
					continue;
				}

				_renderers.Add(r);

				_rendererColors.Add(r.sharedMaterial != null && r.sharedMaterial.HasProperty(_color) ? r.sharedMaterial.color : default);
				_rendererAdditiveColors.Add(r.sharedMaterial != null && r.sharedMaterial.HasProperty(_additiveColor) ? r.sharedMaterial.GetColor(_additiveColor) : default);
				_originalMaterials.Add(r.sharedMaterial);
			}
		}

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		public void SetEnabled(bool rEnabled)
		{
			foreach (var render in _renderers)
			{
				render.enabled = rEnabled;
			}

			foreach (var render in _particleRenderers)
			{
				render.enabled = rEnabled;
			}
		}

		public bool Enabled
		{
			get
			{
				foreach (var render in _renderers)
				{
					return render.enabled;
				}

				foreach (var render in _particleRenderers)
				{
					return render.enabled;
				}

				return false;
			}
		}

		public void SetColor(Color c)
		{
			// TODO: Avoid duplicating the material
			// https://tree.taiga.io/project/firstlightgames-blast-royale-reloaded/task/334
			foreach (var render in _renderers)
			{
				if (render.material.HasProperty(_color))
				{
					render.material.color = c;
				}
			}

			for (var i=0; i<_rendererColors.Count; i++)
			{
				_rendererColors[i] = c;
			}
		}

		public void ResetColor()
		{
			for(var i=0; i<_rendererColors.Count; i++)
			{
				if (_originalMaterials[i].HasProperty(_color))
				{
					_renderers[i].material.color = _originalMaterials[i].color;
					
					_rendererColors[i] = _originalMaterials[i].color;
				}
			}
		}

		public bool GetFirstRendererColor(ref Color color)
		{
			foreach (var render in _renderers)
			{
				if (render.material.HasProperty(_color))
				{
					color = render.material.color;

					return true;
				}
			}
			
			return false;
		}

		public void SetAdditiveColor(Color c)
		{
			// TODO: Avoid duplicating the material
			// https://tree.taiga.io/project/firstlightgames-blast-royale-reloaded/task/334
			foreach (var render in _renderers)
			{
				if (render.material.HasProperty(_additiveColor))
				{
					render.material.SetColor(_additiveColor, c);
				}
			}
		}
		
		public void ResetAdditiveColor()
		{
			for (var i = 0; i < _renderers.Count; i++)
			{
				var render = _renderers[i];

				if (render.material.HasProperty(_additiveColor))
				{
					render.material.SetColor(_additiveColor, _rendererAdditiveColors[i]);
				}
			}
		}

		public async void SetMaterial(MaterialVfxId materialId, bool keepTexture)
		{
			var mat = await _services.AssetResolverService.RequestAsset<MaterialVfxId, Material>(materialId);

			// In case the loading takes more time then expected
			if (this.IsDestroyed())
			{
				Destroy(mat);
				return;
			}

			SetMaterial(mat, keepTexture);
		}

		public void SetMaterial(Material material, bool keepTexture)
		{
			foreach (var r in _renderers)
			{
				var sm = r.sharedMaterial;

				r.sharedMaterial = material;

				if (keepTexture)
				{
					r.material.SetTexture(_mainTex, sm.GetTexture(_mainTex));

					// TODO: Check performance
					// var mp = new MaterialPropertyBlock();
					// mp.SetTexture(_mainTex, sm.GetTexture(_mainTex));
					// r.SetPropertyBlock(mp);
				}
			}
		}

		public void SetLayer(int layer)
		{
			foreach (var r in _renderers)
			{
				r.gameObject.layer = layer;
			}
		}

		public void ResetMaterials()
		{
			for (var i = 0; i < _renderers.Count; i++)
			{
				var r = _renderers[i];
				r.sharedMaterial = _originalMaterials[i];

				if (r.sharedMaterial.HasProperty(_color))
				{
					r.material.color = _rendererColors[i];
				}
			}
		}
	}
}