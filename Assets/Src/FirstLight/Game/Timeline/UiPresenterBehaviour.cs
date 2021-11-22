using System;
using FirstLight.Game.Services;
using UnityEngine.Playables;

namespace FirstLight.Game.Timeline
{
	/// <inheritdoc />
	/// <remarks>
	/// The <see cref="PlayableBehaviour"/> for showing videos on the generic dialog UI
	/// </remarks>
	[System.Serializable]
	public class UiPresenterBehaviour : PlayableBehaviourBase
	{
		public bool OpenUiOnEnter = true;
		public bool OpenUiOnExit;
		public bool CloseUiOnEnter;
		public bool CloseUiOnExit;
		
		public Type UiPresenter { get; set; }
		public IGameUiService UiService { get; set; }

		/// <inheritdoc />
		protected override void OnEnter(Playable playable)
		{
			if (OpenUiOnEnter)
			{
				UiService.OpenUi(UiPresenter);
			}
			else if (CloseUiOnEnter)
			{ 
				UiService.CloseUi(UiPresenter);
			}
		}

		/// <inheritdoc />
		protected override void OnExit(Playable playable)
		{
			if (CloseUiOnExit)
			{ 
				UiService.CloseUi(UiPresenter);
			}
			else if (OpenUiOnExit)
			{
				UiService.OpenUi(UiPresenter);
			}
		}
	}
}