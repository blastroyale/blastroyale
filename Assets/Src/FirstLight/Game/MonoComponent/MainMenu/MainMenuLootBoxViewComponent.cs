using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// Attached to the Loot Box in the main menu scene. Tapping on it takes the player to the Crates Screen.
	/// </summary>
	public class MainMenuLootBoxViewComponent : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField] private GameObject _vfxGlow;
		[SerializeField] private GameObject _glow;
		
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		/// <inheritdoc />
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.dragging)
			{
				return;
			}
			
			_services.MessageBrokerService.Publish(new MenuWorldLootBoxClickedMessage());
		}

		/// <summary>
		/// Set's the VFX to the given <paramref name="show"/> state
		/// </summary>
		public void ShowVFX(bool show)
		{
			_vfxGlow.SetActive(show);
			_glow.SetActive(show);
		}
	}
}