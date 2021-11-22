using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.Rendering;


namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This MonoComponent acts as a container of all <see cref="RenderersContainerMonoComponent"/> inside of this GameObject, also exposes inclusive the
	/// <see cref="Renderer"/> that this game object contains.
	/// </summary>
	[RequireComponent(typeof(RenderersContainerMonoComponent))]
	public class RenderersContainerProxyMonoComponent : MonoBehaviour, IRendersContainer
	{
		[SerializeField] private RenderersContainerMonoComponent _mainRenderersContainer;
		[SerializeField] private List<RenderersContainerMonoComponent> _renderersContainers = new List<RenderersContainerMonoComponent>();
		
		
		private void OnValidate()
		{
			_mainRenderersContainer = _mainRenderersContainer ? _mainRenderersContainer : GetComponent<RenderersContainerMonoComponent>();
			
			_renderersContainers.Clear();	
			
			_renderersContainers.Add(_mainRenderersContainer);
		}

		/// <summary>
		/// Add a given renders container to the list of containers
		/// </summary>
		public void AddRenderersContainer(RenderersContainerMonoComponent renderersContainer)
		{
			_renderersContainers.Add(renderersContainer);
		}

		/// <summary>
		/// Remove a given renders container to the list of containers
		/// </summary>
		public void RemoveRenderersContainer(RenderersContainerMonoComponent renderersContainer)
		{
			_renderersContainers.Remove(renderersContainer);
		}

		/// <inheritdoc />
		public void SetMaterial(Func<int, Material> materialResolver, ShadowCastingMode mode, bool keepTexture)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetMaterial(materialResolver, mode, keepTexture);
			}
		}

		/// <inheritdoc />
		public void SetMaterialPropertyValue(int propertyId, float value)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetMaterialPropertyValue(propertyId, value);
			}
		}
		
		/// <inheritdoc />
		public void SetRendererState(bool visible)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetRendererState(visible);
			}
		}

		/// <inheritdoc />
		public void SetMaterial(MaterialVfxId materialId, ShadowCastingMode mode, bool keepTexture)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetMaterial(materialId, mode, keepTexture);
			}
		}

		/// <inheritdoc />
		public void SetMaterialPropertyValue(int propertyId, float startValue, float endValue, float duration)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetMaterialPropertyValue(propertyId, startValue, endValue, duration);
			}
		}

		/// <inheritdoc />
		public void ResetToOriginalMaterials()
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.ResetToOriginalMaterials();
			}
		}
	}
}