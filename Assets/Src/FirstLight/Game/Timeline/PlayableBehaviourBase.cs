using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.Timeline
{
	/// <summary>
	/// A behaviour on the timeline to be executed when reached
	/// </summary>
	public abstract class PlayableBehaviourBase : PlayableBehaviour
	{
		protected IGameServices Services;
		protected IGameDataProvider DataProvider;
		protected Playable Playable;
		
		public bool IsPlaying { get; private set; }
		public TimelineClip CustomClipReference { get; set; }
		public double StartTime => CustomClipReference.start;
		public double EndTime => CustomClipReference.end;

		/// <inheritdoc />
		public override void OnPlayableCreate(Playable playable)
		{
			if (!Application.isPlaying)
			{
				return;
			}

			Playable = playable;
			Services = MainInstaller.Resolve<IGameServices>();
			DataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			OnCreated(playable);
		}

		/// <inheritdoc />
		public override void OnBehaviourPlay(Playable playable, FrameData info)
		{
			if (!Application.isPlaying || IsPlaying)
			{
				return;
			}

			IsPlaying = true;
			Playable = playable;
			
			OnEnter(playable);
		}

		/// <inheritdoc />
		public override void OnBehaviourPause(Playable playable, FrameData info)
		{
			if (!IsPlaying || playable.GetTime() + 0.001f < playable.GetDuration())
			{
				return;
			}
			
			Playable = playable;
			IsPlaying = false;
			
			OnExit(playable);
		}
		
		protected virtual void OnCreated(Playable playable) { }
		protected virtual void OnEnter(Playable playable) { }
		protected virtual void OnExit(Playable playable) { }

		protected void Pause()
		{
			Playable.GetGraph().StopTimeline();
		}

		protected void Resume()
		{
			Playable.GetGraph().PlayTimeline();
		}
	}
}