using FirstLight.Game.Services;
using I2.Loc;
using Quantum;
using UnityEngine.Playables;

namespace FirstLight.Game.Timeline
{
	/// <inheritdoc />
	/// <remarks>
	/// The <see cref="PlayableBehaviour"/> for showing videos on the generic dialog UI
	/// </remarks>
	[System.Serializable]
	public class GenericVideoBehaviour : PlayableBehaviourBase
	{
		public LocalizedString Title;
		public LocalizedString Description;
		public GameId VideoId;
		public LocalizedString ButtonText;

		/// <inheritdoc />
		protected override void OnEnter(Playable playable)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ButtonText.ToString(),
				ButtonOnClick = Resume
			};
			
			Services.GenericDialogService.OpenVideoDialog(Title.ToString(), Description.ToString(), VideoId, false, confirmButton);
		}
	}
}