using System.Collections.Generic;
using FirstLight.Game.Ids;
using UnityEngine;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This MonoComponent acts as a container of all <see cref="RenderersContainerMonoComponent"/> inside of this GameObject, also exposes inclusive the
	/// <see cref="Renderer"/> that this game object contains.
	/// </summary>
	[RequireComponent(typeof(RenderersContainerMonoComponent))]
	public class RenderersContainerProxyMonoComponent : MonoBehaviour, IRendersContainer
	{
		private readonly List<RenderersContainerMonoComponent> _renderersContainers = new();

		private void Awake()
		{
			AddRenderersContainer(GetComponent<RenderersContainerMonoComponent>());
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

		public void SetMaterial(Material material, bool keepTexture)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetMaterial(material, keepTexture);
			}
		}

		public void SetLayer(int layer)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetLayer(layer);
			}
		}

		public void SetEnabled(bool rEnabled)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetEnabled(rEnabled);
			}
		}

		public void SetColor(Color color)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetColor(color);
			}
		}
		
		public void SetAdditiveColor(Color color)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetAdditiveColor(color);
			}
		}

		public void SetMaterial(MaterialVfxId materialId, bool keepTexture)
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.SetMaterial(materialId, keepTexture);
			}
		}

		public void ResetMaterials()
		{
			foreach (var renderersContainer in _renderersContainers)
			{
				renderersContainer.ResetMaterials();
			}
		}
	}
}