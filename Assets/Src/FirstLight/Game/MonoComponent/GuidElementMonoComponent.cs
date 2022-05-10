using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// 
	/// </summary>
	public interface IGuidElement
	{
		/// <summary>
		/// Request the GuidId for this element
		/// </summary>
		GuidId Id { get; }
		
		/// <summary>
		/// The references GameObjects this GUID Id element is setting up to
		/// </summary>
		IReadOnlyList<GameObject> Elements { get; }

		/// <summary>
		/// Set's all the elements referenced by this GUID to the given <paramref name="active"/> state
		/// </summary>
		void SetState(bool active);
	}
	
	/// <inheritdoc cref="IGuidElement" />
	public class GuidElementMonoComponent : MonoBehaviour, IGuidElement
	{
		[SerializeField] private EnumSelector<GuidId> _id;
		[SerializeField] private List<GameObject> _elements;

		private IGameServices _services;

		/// <inheritdoc />
		public GuidId Id => _id;

		/// <inheritdoc />
		public IReadOnlyList<GameObject> Elements => _elements;

		private void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_services.GuidService.AddElement(this);
		}

		private void OnDestroy()
		{
			_services?.GuidService?.RemoveElement(Id);
		}

		/// <inheritdoc />
		public void SetState(bool active)
		{
			foreach (var element in _elements)
			{
				element.SetActive(active);
			}
		}
	}
}