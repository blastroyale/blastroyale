using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	///  This Mono component handles playing a vfx particle system at the world hit position when pointer clicked event is received
	/// </summary>
	public class MainMenuVfxMonoComponent : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField] private ParticleSystem _particleSystem;
		
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.dragging)
			{
				return;
			}

			_particleSystem.transform.position = eventData.pointerCurrentRaycast.worldPosition;
			_particleSystem.Play();
		}
	}
}