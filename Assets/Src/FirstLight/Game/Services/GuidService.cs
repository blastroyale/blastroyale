using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service allows to match a list of elements from the game to be accessed at any moment from any system in the game.
	/// This allows to manipulate elements at free will by having the correct referenced <see cref="GuidId"/>
	/// </summary>
	public interface IGuidService
	{
		/// <summary>
		/// Adds the given <paramref name="element"/> to the list of GUIDs reference by this service
		/// </summary>
		void AddElement(IGuidElement element);
		
		/// <summary>
		/// Removes the element with the given <paramref name="id"/> of the list of GUIDs reference by this service
		/// </summary>
		void RemoveElement(GuidId id);
		
		/// <summary>
		/// Removes the given <paramref name="element"/> from the list of GUIDs reference by this service
		/// </summary>
		void RemoveElement(IGuidElement element);
		
		/// <summary>
		/// Requests the <see cref="IGuidElement"/> matching the given <paramref name="id"/> from the list of GUIDs
		/// reference by this service
		/// </summary>
		IGuidElement GetElement(GuidId id);
	}
	
	/// <inheritdoc />
	public class GuidService : IGuidService
	{
		private readonly Dictionary<GuidId, IList<IGuidElement>> _elements = new Dictionary<GuidId, IList<IGuidElement>>(new GuidIdComparer());
		
		/// <inheritdoc />
		public void AddElement(IGuidElement element)
		{
			if (!_elements.TryGetValue(element.Id, out var elements))
			{
				elements = new List<IGuidElement>();
				
				_elements.Add(element.Id, elements);
			}
			
			elements.Add(element);
		}

		/// <inheritdoc />
		public void RemoveElement(GuidId id)
		{
			_elements.Remove(id);
		}

		/// <inheritdoc />
		public void RemoveElement(IGuidElement id)
		{
			_elements.Remove(id.Id);
		}

		/// <inheritdoc />
		public IGuidElement GetElement(GuidId id)
		{
			if (!_elements.ContainsKey(id))
			{
				var elements = Object.FindObjectsOfType<GuidElementMonoComponent>(true);

				foreach (var element in elements)
				{
					if (!_elements.ContainsKey(element.Id))
					{
						AddElement(element);
					}
				}
			}

			return _elements[id][0];
		}
	}
}