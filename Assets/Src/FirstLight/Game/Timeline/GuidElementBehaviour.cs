using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent;
using UnityEngine.Playables;

namespace FirstLight.Game.Timeline
{
	/// <inheritdoc />
	/// <remarks>
	/// The <see cref="PlayableBehaviour"/> for the <see cref="GuidElementMonoComponent"/>
	/// </remarks>
	[System.Serializable]
	public class GuidElementBehaviour : PlayableBehaviourBase
	{
		public EnumSelector<GuidId> Id;
		public bool IsElementActiveOnEnter = true;
		public bool IsElementActiveOnExit;

		/// <inheritdoc />
		protected override void OnEnter(Playable playable)
		{
			Services.GuidService.GetElement(Id).SetState(IsElementActiveOnEnter);
		}

		/// <inheritdoc />
		protected override void OnExit(Playable playable)
		{
			Services.GuidService.GetElement(Id).SetState(IsElementActiveOnExit);
		}
	}
}