using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Infos;
using Quantum;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <summary>
	/// Component for Gliders in the Main Menu.
	/// Used to turn Particle Effects on or off.
	/// </summary>
	public class MainMenuGliderViewComponent :MonoBehaviour
	{
		/// <summary>
		/// Array of Trail Emitter Game Objects that display VFX Trails.
		/// </summary>
		[SerializeField] private GameObject[] _trails;


		/// <summary>
		/// Turn particle systems on or off.
		/// </summary>
		/// <param name="activate"></param>
		public void ActivateParticleEffects(bool activate)
		{
			foreach (var trail in _trails)
			{
				trail.SetActive(activate);
			}
		}

	}
}