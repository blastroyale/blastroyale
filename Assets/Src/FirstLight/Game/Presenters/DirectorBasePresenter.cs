using FirstLight.UiService;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter provides methods for starting the appear and
	/// unload directors
	/// </summary>
	public abstract class DirectorBasePresenter : UiPresenter
	{
		[SerializeField] private PlayableDirector _appearDirector;
		[SerializeField] private PlayableDirector _unloadDirector;
		
		/// <summary>
		/// Play the appear director timeline
		/// </summary>
		public void TriggerAppearAnimations()
		{
			_appearDirector.Play();
		}
		
		/// <summary>
		/// Play the unload director timeline
		/// </summary>
		public void TriggerUnloadAnimations()
		{
			_unloadDirector.Play();
		}
	}
}