using I2.Loc;
using UnityEngine.Playables;

namespace FirstLight.Game.Timeline
{
	/// <inheritdoc />
	/// <remarks>
	/// The <see cref="PlayableBehaviour"/> for the Talking head UI
	/// </remarks>
	[System.Serializable]
	public class TalkingHeadBehaviour : PlayableBehaviourBase
	{
		public LocalizedString Title;

		/// <inheritdoc />
		protected override void OnEnter(Playable playable)
		{
			Services.GenericDialogService.OpenTalkingHeadDialog(Title.ToString(), Resume);
		}
	}
}